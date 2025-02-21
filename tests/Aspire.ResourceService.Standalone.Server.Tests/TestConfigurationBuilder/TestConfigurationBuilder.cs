
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Microsoft.Extensions.Configuration;

namespace Aspire.ResourceService.Standalone.Server.Tests.TestConfigurationBuilder;
public static class TestConfigurationBuilder
{
    public static IConfiguration GetTestConfiguration(string resourceProvider)
    {
        var config = new ConfigurationBuilder();

        if (resourceProvider == "docker")
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ResourceProvider", "docker" }
            });
        }

        if (resourceProvider == "k8s")
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ResourceProvider", "k8s" },
                { "KubernetesResourceProviderConfiguration:Namespace", "test" },
                { "KubernetesResourceProviderConfiguration:Servicenames", "redis;rabbitmq;mongo" }
            });
        }

        return config.Build();
    }

    public static string GetK8sNamespaceValue()
    {
        var config = GetTestConfiguration("k8s")
            .GetSection("KubernetesResourceProviderConfiguration")
            .Get<KubernetesResourceProviderConfiguration>();
        string nameSpace = config?.Namespace ?? throw new InvalidOperationException();

        return nameSpace;
    }

    public static string[] GetK8sServiceNamesValue()
    {
        var config = GetTestConfiguration("k8s")
            .GetSection("KubernetesResourceProviderConfiguration")
            .Get<KubernetesResourceProviderConfiguration>();
        string serviceNames = config?.Servicenames ?? throw new InvalidOperationException();

        return serviceNames.Split(';');
    }
}
