using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.Reporting;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;

internal sealed partial class DockerResourceProvider : IResourceProvider, IDisposable
{
    private bool _disposedValue;
    private readonly IResourceReporter _resourceReporter;
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerResourceProvider> _logger;

    public DockerResourceProvider(IResourceReporter resourceReporter, IDockerClient dockerClient, ILogger<DockerResourceProvider> logger)
    {
        _resourceReporter = resourceReporter;
        _dockerClient = dockerClient;
        _logger = logger;
        _ = Task.Run(async () =>
        {
            await WatchResources().ConfigureAwait(false);
        });
    }
    public async Task<ResourceSubscription> GetResources(CancellationToken cancellationToken = default)
    {
        var containers = await GetContainers(cancellationToken).ConfigureAwait(false);

        var resources = containers.Select(Resource.FromDockerContainer).ToList().AsReadOnly();

        return new ResourceSubscription(resources, UpdateStream(cancellationToken));

        async IAsyncEnumerable<WatchResourcesChange?> UpdateStream([EnumeratorCancellation] CancellationToken cancellation)
        {
            var channel = Channel.CreateUnbounded<Message>();
            var progress = new Progress<Message>(message => channel.Writer.TryWrite(message));

            try
            {
                _ = _dockerClient.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, cancellation);
                _logger.MonitoringDockerEventsStarted();
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                // Task is cancelled, swallow the exception.
            }

            _logger.WaitingForDockerEvents();
            await foreach (var msg in channel.Reader.ReadAllAsync(cancellation).ConfigureAwait(false))
            {
                _logger.CapturedDockerChange(JsonSerializer.Serialize(msg));

                if (!string.Equals(msg.Type, KnownResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.SkippingChange(msg.Type);
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
            var resource = Resource.FromDockerContainer(container);

            return new() { Upsert = resource };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async ValueTask WatchResources(CancellationToken cancellation = default)
    {
        var initialContainers = await GetContainers(cancellation).ConfigureAwait(false);

        foreach (var container in initialContainers)
        {
            var dockerResource = DockerResourceSnapshot.FromContainer(container);
            await _resourceReporter.UpdateAsync(dockerResource, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        }

    }

    public async IAsyncEnumerable<ResourceLogEntry> GetResourceLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var containers = await GetContainers(cancellationToken).ConfigureAwait(false);

        var container = containers.Single(c => c.Names.First().Replace("/", "").Equals(resourceName, StringComparison.Ordinal));

        var notificationChannel = Channel.CreateUnbounded<ResourceLogEntry>();

        void WriteToChannel(string log)
        {
            notificationChannel.Writer.TryWrite(new ResourceLogEntry(resourceName, log));
        }

        IProgress<string> p = new Progress<string>(WriteToChannel);

        try
        {
            _ = _dockerClient.Containers.GetContainerLogsAsync(container.ID,
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

    public async Task<IList<ContainerListResponse>> GetContainers(CancellationToken cancellationToken = default)
    {
        var c = await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters() { All = true }, cancellationToken)
            .ConfigureAwait(false);
        return c;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dockerClient?.Dispose();
            }

            _disposedValue = true;
        }
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
