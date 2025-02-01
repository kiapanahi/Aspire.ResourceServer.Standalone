using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Docker.DotNet;
using k8s;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services, IConfiguration configuration)
    {

        if (configuration["UseDocker"] == "True")
        {
            services.AddDockerResourceProvider();
        }

        if (configuration["UseK8s"] == "True")
        {
            services.Configure<KubernetesResourceProviderConfiguration>(
                configuration.GetSection("KubernetesResourceProviderConfiguration"));

            services.AddKubernetesResourceProvider();
        }

        return services;
    }

    public static IServiceCollection AddDockerResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }

    public static IServiceCollection AddKubernetesResourceProvider(this IServiceCollection services)
    {
        if (KubernetesClientConfiguration.IsInCluster())
        {
            services.AddSingleton<Kubernetes>(new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
        }
        else
        {
            services.AddSingleton<Kubernetes>(new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile()));
        }

        services.AddSingleton<IResourceProvider, KubernetesResourceProvider>();

        return services;
    }
}
