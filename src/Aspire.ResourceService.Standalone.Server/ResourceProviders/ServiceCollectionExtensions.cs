using Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Docker.DotNet;
using k8s;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services, IConfiguration configuration)
    {

        if (configuration["ResourceProvider"] == "docker")
        {
            services.AddDockerResourceProvider();
        }

        if (configuration["ResourceProvider"] == "k8s")
        {
            services.AddKubernetesResourceProvider(configuration);
        }

        return services;
    }

    public static IServiceCollection AddDockerResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }

    public static IServiceCollection AddKubernetesResourceProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KubernetesResourceProviderConfiguration>(
                configuration.GetSection("KubernetesResourceProviderConfiguration"));

        if (KubernetesClientConfiguration.IsInCluster())
        {
            services.AddSingleton<IKubernetes>(new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
        }
        else
        {
            services.AddSingleton<IKubernetes>(new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile()));
        }

        services.AddSingleton<IResourceProvider, KubernetesResourceProvider>();

        return services;
    }
}
