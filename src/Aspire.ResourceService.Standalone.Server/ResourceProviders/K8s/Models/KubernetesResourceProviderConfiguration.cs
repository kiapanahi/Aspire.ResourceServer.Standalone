namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

public class KubernetesResourceProviderConfiguration
{
    public required string Namespace { get; set; }
    public required string[] ServiceNames { get; set; }
}
