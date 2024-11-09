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

    public async Task<IEnumerable<Resource>> GetResourcesAsync()
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
                ResourceType = "Container",
                Name = string.Join('|', container.Names),
                Uid = container.ID
            };

            ar.Urls.Add(container.Ports.Where(p => !string.IsNullOrEmpty(p.IP))
                .Select(s => new Url { FullUrl = $"{s.IP}:{s.PublicPort}" }));

            resources.Add(ar);
        }

        return resources;
    }
}
