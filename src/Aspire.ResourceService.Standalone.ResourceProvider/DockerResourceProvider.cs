using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.ResourceProvider;

using Docker.DotNet;
using Docker.DotNet.Models;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Logging;

using AspireResouce = Aspire.ResourceService.Proto.V1.Resource;

namespace Aspire.ResourceServer.Standalone.ResourceLocator;

internal sealed partial class DockerResourceProvider : IResourceProvider
{
    private readonly DockerClient _dockerClient;
    private readonly ILogger<DockerResourceProvider> _logger;

    public DockerResourceProvider(ILogger<DockerResourceProvider> logger)
    {
        _logger = logger;
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    public async Task<IEnumerable<AspireResouce>> GetResourcesAsync()
    {
        var containers = await _dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters())
            .ConfigureAwait(false);

        return containers.Select(container => new AspireResouce
        {
            DisplayName = container.Names.FirstOrDefault(),
            Properties =
            {
                new ResourceProperty
                {
                    Name = "image",
                    DisplayName = "Image Name",
                    Value = new Value { StringValue = container.Image }
                },
                new ResourceProperty
                {
                    Name = "container_id",
                    DisplayName = "Container ID",
                    Value = new Value { StringValue = container.ID }
                },
                new ResourceProperty
                {
                    Name = "ports",
                    DisplayName = "Ports",
                    Value =
                        new Value
                        {
                            StringValue = string.Join(',',
                                container.Ports.Select(s => $"{s.PrivatePort}:{s.PublicPort}"))
                        }
                }
            },
            Name = container.Names.FirstOrDefault(),
            CreatedAt = Timestamp.FromDateTime(container.Created),
            State = container.State,
            ResourceType = "container",
            Uid = container.ID
        });
    }
}
