using System.Text.Json;
using Aspire.Dashboard.Model;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;

internal sealed partial class DockerResourceProvider : IResourceProvider, IDisposable
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerResourceProvider> _logger;

    public DockerResourceProvider(IDockerClient dockerClient, ILogger<DockerResourceProvider> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
    }

    public async ValueTask GetResources(CancellationToken cancellation = default)
    {
        var initialContainers = await GetContainers(cancellation).ConfigureAwait(false);

        foreach (var container in initialContainers)
        {
            var dockerResource = DockerResourceSnapshot.FromContainer(container);
        }

        var progress = new Progress<Message>(ReportChange);

        _ = _dockerClient.System.MonitorEventsAsync(new ContainerEventsParameters(), progress, cancellation);
        _logger.MonitoringDockerEventsStarted();
        _logger.WaitingForDockerEvents();

        async void ReportChange(Message msg)
        {
            _logger.CapturedDockerChange(JsonSerializer.Serialize(msg));

            if (!string.Equals(msg.Type, KnownResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
            {
                _logger.SkippingChange(msg.Type);
                return;
            }
            var container = await FindContainer(msg.Actor.ID, cancellation).ConfigureAwait(false);
        }
    }

    public async Task<IList<ContainerListResponse>> GetContainers(CancellationToken cancellationToken = default)
    {
        var c = await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters() { All = true }, cancellationToken)
            .ConfigureAwait(false);
        return c;
    }

    public async Task<ContainerListResponse> FindContainer(string ContainerId, CancellationToken cancellationToken = default)
    {
        var containers = await GetContainers(cancellationToken).ConfigureAwait(false);

        var container = containers.Single(c => c.ID.Equals(ContainerId, StringComparison.OrdinalIgnoreCase));

        return container;
    }

    public void Dispose()
    {
        _dockerClient?.Dispose();
    }
}
