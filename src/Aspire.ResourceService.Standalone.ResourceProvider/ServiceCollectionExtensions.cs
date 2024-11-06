using Docker.DotNet;

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceService.Standalone.ResourceProvider;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }
}
