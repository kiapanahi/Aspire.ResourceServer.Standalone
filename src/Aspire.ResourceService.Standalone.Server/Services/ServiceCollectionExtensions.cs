namespace Aspire.ResourceService.Standalone.Server.Services;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddDashboardData(this IServiceCollection services)
    {
        services.AddSingleton<DashboardData>();
        return services;
    }
}
