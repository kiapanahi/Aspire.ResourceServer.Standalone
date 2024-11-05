using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceServer.Standalone.ResourceLocator;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }
}
