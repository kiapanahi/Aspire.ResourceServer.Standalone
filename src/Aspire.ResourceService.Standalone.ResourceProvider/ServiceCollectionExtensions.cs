using Aspire.ResourceServer.Standalone.ResourceLocator;

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceService.Standalone.ResourceProvider;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceProvider(this IServiceCollection services)
    {
        services.AddSingleton<IResourceProvider, DockerResourceProvider>();

        return services;
    }
}
