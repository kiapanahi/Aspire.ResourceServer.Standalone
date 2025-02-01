using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal sealed partial class DockerResourceProvider(IDockerClient dockerClient, ILogger<DockerResourceProvider> logger)
    : IResourceProvider
{
    public async Task<ResourceSubscription> GetResources(CancellationToken cancellationToken)
    {
        var containers = await GetContainers().ConfigureAwait(false);

        var resources = containers.Select(Resource.FromContainer).ToList().AsReadOnly();

        return new ResourceSubscription(resources, UpdateStream(cancellationToken));

        async IAsyncEnumerable<WatchResourcesChange?> UpdateStream(
            [EnumeratorCancellation] CancellationToken cancellation)
        {
            var channel = Channel.CreateUnbounded<Message>();
            var progress = new Progress<Message>(message => channel.Writer.TryWrite(message));

            try
            {
                _ = dockerClient.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, cancellation);
                logger.MonitoringDockerEventsStarted();
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                // Task is cancelled, swallow the exception.
            }

            logger.WaitingForDockerEvents();
            await foreach (var msg in channel.Reader.ReadAllAsync(cancellation).ConfigureAwait(false))
            {
                logger.CapturedDockerChange(JsonSerializer.Serialize(msg));

                if (!string.Equals(msg.Type, KnownResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
                {
                    logger.SkippingChange(msg.Type);
                    continue;
                }

                yield return await GetChangeForStartedContainer(msg.Actor.ID).ConfigureAwait(false);
            }
        }
    }

    private async Task<WatchResourcesChange?> GetChangeForStartedContainer(string containerId)
    {
        try
        {
            var containers = await GetContainers().ConfigureAwait(false);
            var container =
                containers.Single(c => string.Equals(c.ID, containerId, StringComparison.OrdinalIgnoreCase));
            var resource = Resource.FromContainer(container);

            return new() { Upsert = resource };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<ResourceLogEntry> GerResourceLogs(string resourceName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var containers = await GetContainers().ConfigureAwait(false);

        var container = containers.Single(c => c.Names.First().Replace("/", "").Equals(resourceName, StringComparison.Ordinal));

        var notificationChannel = Channel.CreateUnbounded<ResourceLogEntry>();

        void WriteToChannel(string log)
        {
            notificationChannel.Writer.TryWrite(new ResourceLogEntry(resourceName, log));
        }

        IProgress<string> p = new Progress<string>(WriteToChannel);

        try
        {
            _ = dockerClient.Containers.GetContainerLogsAsync(container.ID,
                new() { ShowStdout = true, ShowStderr = true, Follow = true },
                cancellationToken,
                p);
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
        {
            notificationChannel.Writer.Complete();
        }

        await foreach (var logItem in notificationChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return logItem;
        }
    }

    public async Task<IList<ContainerListResponse>> GetContainers()
    {
        var c = await dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters() { All = true }, CancellationToken.None)
            .ConfigureAwait(false);
        return c;
    }
}

internal static partial class DockerResourceProviderLogs
{
    [LoggerMessage(LogLevel.Debug, "Monitoring Docker events started")]
    public static partial void MonitoringDockerEventsStarted(this ILogger<DockerResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Waiting for Docker events")]
    public static partial void WaitingForDockerEvents(this ILogger<DockerResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Captured change: {Change}")]
    public static partial void CapturedDockerChange(this ILogger<DockerResourceProvider> logger, string change);

    [LoggerMessage(LogLevel.Debug, "Skipping change of type: {Change}")]
    public static partial void SkippingChange(this ILogger<DockerResourceProvider> logger, string change);
}
