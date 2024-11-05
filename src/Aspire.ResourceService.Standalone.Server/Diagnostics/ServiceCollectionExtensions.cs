using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.ResourceService.Standalone.Server.Diagnostics;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceInformationProvider(this IServiceCollection services)
    {
        return services.AddServiceInformationProvider<AssemblyServiceInformationProvider>();
    }

    public static IServiceCollection AddServiceInformationProvider<T>(this IServiceCollection services)
        where T : IServiceInformationProvider, new()
    {
        services.TryAddSingleton<IServiceInformationProvider>(_ => new T());

        return services;
    }
}
