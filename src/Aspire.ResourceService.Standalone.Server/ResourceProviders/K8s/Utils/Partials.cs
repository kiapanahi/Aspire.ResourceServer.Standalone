using Aspire.Dashboard.Model;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

// ReSharper disable CheckNamespace

namespace Aspire.ResourceService.Proto.V1;

public sealed partial class Resource
{
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
