using Aspire.Dashboard.Model;
using Docker.DotNet.Models;
using Google.Protobuf.WellKnownTypes;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

// ReSharper disable CheckNamespace

namespace Aspire.ResourceService.Proto.V1;

public sealed partial class Resource
{
    internal static Resource FromDockerContainer(ContainerListResponse container)
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
    internal static Resource FromK8sContainer(KubernetesContainer container)
    {
        var resource = new Resource
        {
            CreatedAt = container.StartedAt,
            State = container.State,
            DisplayName = container.Name,
            ResourceType = KnownResourceTypes.Container,
            Name = container.Name,
            Uid = container.ContainerID
        };
        resource.Urls.Add(new Url()
        {
            IsInternal = false,
            Name = $"http://{container.Name}:{container.Port}",
            FullUrl = $"http://{container.Name}:{container.Port}"
        });
        return resource;
    }
}
