namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceReporter(this IServiceCollection services)
    {

        services.AddSingleton<ResourceReporter>();

        return services;
    }
}
