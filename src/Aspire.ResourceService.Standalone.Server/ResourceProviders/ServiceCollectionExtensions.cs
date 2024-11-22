using Docker.DotNet;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services)
    {

        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }
}
