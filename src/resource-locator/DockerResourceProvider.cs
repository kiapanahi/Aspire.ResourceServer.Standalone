using Docker.DotNet;
using Docker.DotNet.Models;

using Microsoft.Extensions.Logging;

using AspireResouce = Aspire.ResourceService.Proto.V1.Resource;

namespace Aspire.ResourceServer.Standalone.ResourceLocator;

internal sealed partial class DockerResourceProvider : IResourceProvider
{
    private readonly ILogger<DockerResourceProvider> _logger;
    private readonly DockerClient _dockerClient;

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
            Properties = {
                new ResourceService.Proto.V1.ResourceProperty {
                    Name = "image",
                    DisplayName = "Image Name",
                    Value = new Google.Protobuf.WellKnownTypes.Value {
                        StringValue = container.Image
                    }
                },
                new ResourceService.Proto.V1.ResourceProperty {
                    Name = "container_id",
                    DisplayName = "Container ID",
                    Value = new Google.Protobuf.WellKnownTypes.Value {
                        StringValue = container.ID
                    }
                },
                new ResourceService.Proto.V1.ResourceProperty {
                    Name = "ports",
                    DisplayName = "Ports",
                    Value = new Google.Protobuf.WellKnownTypes.Value {
                        StringValue = string.Join(',', container.Ports.Select(s => $"{s.PrivatePort}:{s.PublicPort}"))
                    }
                }
            },
            Name = container.Names.FirstOrDefault(),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(container.Created),
            State = container.State,
            ResourceType = "container",
            Uid = container.ID
        });
    }

}
