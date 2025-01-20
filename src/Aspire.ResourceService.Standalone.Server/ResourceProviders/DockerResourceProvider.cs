using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Docker.DotNet;
using Docker.DotNet.Models;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal sealed partial class DockerResourceProvider(IDockerClient dockerClient, ILogger<DockerResourceProvider> logger)
    : IResourceProvider
{
    private readonly SemaphoreSlim _syncRoot = new(1);
    private readonly List<ContainerListResponse> _dockerContainers = [];

    public async Task<ResourceSubscription> GetResources(CancellationToken cancellationToken)
    {
        var containers = await GetContainers().ConfigureAwait(false);

        List<Resource> resources = [];
        foreach (var container in containers)
        {
            var containerName = container.Names.First().Replace("/", "");
            var ar = new Resource
            {
                CreatedAt = Timestamp.FromDateTime(container.Created),
                State = container.State,
                DisplayName = containerName,
                ResourceType = KnownResourceTypes.Container,
                Name = containerName,
                Uid = container.ID
            };

            ar.Urls.Add(container.Ports.Where(p => !string.IsNullOrEmpty(p.IP))
                .Select(s => new Url
                {
                    IsInternal = false,
                    Name = $"http://{s.IP}:{s.PublicPort}",
                    FullUrl = $"http://{s.IP}:{s.PublicPort}"
                }));

            resources.Add(ar);
        }

        return new ResourceSubscription(resources, UpdateStream(cancellationToken));

        [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
        async IAsyncEnumerable<WatchResourcesChange> UpdateStream(
            [EnumeratorCancellation] CancellationToken cancellation)
        {
            logger.LogInformation("Starting to monitor Docker events");
            var channel = Channel.CreateUnbounded<Message>();
            var progress = new Progress<Message>(message => channel.Writer.TryWrite(message));

            _ = dockerClient.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, cancellation);
            logger.LogInformation("Monitoring Docker events started");

            logger.LogInformation("Waiting for Docker events");
            await foreach (var msg in channel.Reader.ReadAllAsync(cancellation).ConfigureAwait(false))
            {
                logger.LogInformation("Captured change: {Message}", JsonSerializer.Serialize(msg));

                if (!string.Equals(msg.Type, "container", StringComparison.Ordinal))
                {
                    logger.LogInformation("Change type not container, skipping");
                    continue;
                }

                var createdAt = Timestamp.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(msg.Time).UtcDateTime);
                var displayName = msg.Actor.Attributes["name"];
                var containerId = msg.Actor.ID;
                yield return msg.Action switch
                {
                    "start" => new WatchResourcesChange
                    {
                        Upsert = new Resource
                        {
                            CreatedAt = createdAt,
                            State = "running",
                            DisplayName = displayName,
                            ResourceType = KnownResourceTypes.Container,
                            Name = displayName,
                            Uid = containerId
                        }
                    },
                    "stop" or "die" => new WatchResourcesChange
                    {
                        Delete = new ResourceDeletion
                        {
                            ResourceType = KnownResourceTypes.Container, ResourceName = displayName
                        }
                    },
                    _ => new WatchResourcesChange()
                };
            }
        }
    }

    public async IAsyncEnumerable<string> GerResourceLogs(string resourceName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var containers = await GetContainers().ConfigureAwait(false);

        var container = containers.Single(c => c.Names.Contains(resourceName));

        var notificationChannel = Channel.CreateUnbounded<string>();

        void WriteToChannel(string log)
        {
            notificationChannel.Writer.TryWrite(log);
        }

        IProgress<string> p = new Progress<string>(WriteToChannel);

        _ = dockerClient.Containers
            .GetContainerLogsAsync(container.ID,
                new ContainerLogsParameters() { ShowStdout = true, ShowStderr = true, Follow = true },
                cancellationToken, p);

        await foreach (var logItem in notificationChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return logItem;
        }
    }

    private async ValueTask<List<ContainerListResponse>> GetContainers()
    {
        if (_dockerContainers.Count != 0)
        {
            return _dockerContainers;
        }

        try
        {
            await _syncRoot.WaitAsync().ConfigureAwait(false);
            var c = await dockerClient.Containers
                .ListContainersAsync(new ContainersListParameters(), CancellationToken.None)
                .ConfigureAwait(false);

            _dockerContainers.AddRange(c);

            return _dockerContainers;
        }

        finally
        {
            _syncRoot.Release();
        }
    }
}
