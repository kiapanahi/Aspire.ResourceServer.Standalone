namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

public class KubernetesResourceProviderConfiguration
{
    public string? Namespace { get; set; }
    public string? Servicenames { get; set; }
}
