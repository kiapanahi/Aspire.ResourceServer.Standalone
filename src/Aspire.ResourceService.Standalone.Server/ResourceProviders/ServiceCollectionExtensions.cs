using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Docker.DotNet;
using k8s;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var providersToRegister = new List<string>();

        // Support both single provider and multiple providers configuration
        var singleProvider = configuration["ResourceProvider"];
        if (!string.IsNullOrEmpty(singleProvider))
        {
            providersToRegister.Add(singleProvider);
        }

        // Support multiple providers via ResourceProviders array configuration
        var multipleProviders = configuration.GetSection("ResourceProviders").GetChildren()
            .Select(p => p.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .Cast<string>() // Explicitly cast non-null values to string
            .ToList();
        
        providersToRegister.AddRange(multipleProviders);

        // Remove duplicates
        providersToRegister = providersToRegister.Distinct().ToList();

        // Register individual providers with specific keys to avoid conflicts
        foreach (var provider in providersToRegister)
        {
            switch (provider.ToLowerInvariant())
            {
                case "docker":
                    services.AddDockerResourceProvider();
                    break;
                case "k8s":
                    services.AddKubernetesResourceProvider(configuration);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown resource provider: {provider}");
            }
        }

        // Register the aggregating notification service
        if (providersToRegister.Count > 0)
        {
            services.AddSingleton<IResourceNotificationService>(serviceProvider =>
            {
                // Get concrete providers directly to avoid circular dependency
                var resourceProviders = new List<IResourceProvider>();
                
                if (providersToRegister.Contains("docker"))
                {
                    resourceProviders.Add(serviceProvider.GetRequiredService<DockerResourceProvider>());
                }
                
                if (providersToRegister.Contains("k8s"))
                {
                    resourceProviders.Add(serviceProvider.GetRequiredService<KubernetesResourceProvider>());
                }
                
                var logger = serviceProvider.GetRequiredService<ILogger<ResourceNotificationService>>();
                return new ResourceNotificationService(resourceProviders, logger);
            });
            
            // For backward compatibility with DashboardService expecting IResourceProvider
            // Register the notification service as the primary IResourceProvider
            services.AddSingleton<IResourceProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<IResourceNotificationService>() as IResourceProvider
                ?? throw new InvalidOperationException("ResourceNotificationService should implement IResourceProvider"));
        }

        return services;
    }

    public static IServiceCollection AddDockerResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<DockerResourceProvider>();

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

        services.AddSingleton<KubernetesResourceProvider>();

        return services;
    }
}
