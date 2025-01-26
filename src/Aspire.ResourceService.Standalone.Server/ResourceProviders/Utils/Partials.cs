using Aspire.Dashboard.Model;
using Docker.DotNet.Models;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Proto.V1;

public sealed partial class Resource
{
    internal static Resource FromContainer(ContainerListResponse container)
    {
        var containerName = container.Names.First().Replace("/", "");
        var resource = new Resource
        {
            CreatedAt = Timestamp.FromDateTime(container.Created),
            State = container.State,
            DisplayName = containerName,
            ResourceType = KnownResourceTypes.Container,
            Name = containerName,
            Uid = container.ID
        };
        resource.Urls.Add(container.Ports.Where(p => !string.IsNullOrEmpty(p.IP))
            .Select(s => new Url
            {
                IsInternal = false,
                Name = $"http://{s.IP}:{s.PublicPort}",
                FullUrl = $"http://{s.IP}:{s.PublicPort}"
            }));
        return resource;
    }
}
