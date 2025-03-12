namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddResourceNotificationService(this IServiceCollection services)
    {
        services.AddSingleton<ResourceNotificationService>();
        return services;
    }
}
