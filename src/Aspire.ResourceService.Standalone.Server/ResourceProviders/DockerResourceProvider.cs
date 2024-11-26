using System.Runtime.CompilerServices;

using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;

using Docker.DotNet;
using Docker.DotNet.Models;

using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal sealed partial class DockerResourceProvider : IResourceProvider
{
    private readonly IDockerClient _dockerClient;

    public DockerResourceProvider(IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task<List<Resource>> GetResourcesAsync()
    {
        var containers = await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters())
            .ConfigureAwait(false);

        List<Resource> resources = [];
        foreach (var container in containers)
        {
            var ar = new Resource
            {
                CreatedAt = Timestamp.FromDateTime(container.Created),
                State = container.State,
                DisplayName = container.Names.First(),
                ResourceType = KnownResourceTypes.Container,
                Name = string.Join('|', container.Names),
                Uid = container.ID
            };

            ar.Urls.Add(container.Ports.Where(p => !string.IsNullOrEmpty(p.IP))
                .Select(s => new Url
                {
                    IsInternal = false,
                    Name = $"https://{s.IP}:{s.PublicPort}",
                    FullUrl = $"https://{s.IP}:{s.PublicPort}"
                }));

            resources.Add(ar);
        }

        return resources;
    }

    public async IAsyncEnumerable<string> GerResourceLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters(), cancellationToken);

        var container = containers.Single(c => c.Names.Contains(resourceName));

        using var stream = await _dockerClient.Containers
            .GetContainerLogsAsync(container.ID, false, new ContainerLogsParameters()
            {
                ShowStdout = true,
                ShowStderr = true
            }, cancellationToken)
            .ConfigureAwait(false);

        var (output, error) = await stream.ReadOutputToEndAsync(cancellationToken).ConfigureAwait(false);

        var lines = output.Split(Environment.NewLine).Union(error.Split(Environment.NewLine));

        foreach (var line in lines)
        {
            yield return line;
        }

    }
}
