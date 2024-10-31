using Aspire.ResourceServer.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Proto.V1;

using Grpc.Core;

namespace Server.Services;

internal sealed class DashboardService : Aspire.ResourceService.Proto.V1.DashboardService.DashboardServiceBase
{
    private readonly IServiceInformationProvider _serviceInformationProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IServiceInformationProvider serviceInformationProvider, ILogger<DashboardService> logger)
    {
        _serviceInformationProvider = serviceInformationProvider;
        _logger = logger;
    }

    public override Task<ApplicationInformationResponse> GetApplicationInformation(
        ApplicationInformationRequest request, ServerCallContext context)
    {
        _logger.ReturningApplicationInformation();

        return Task.FromResult(new ApplicationInformationResponse
        {
            ApplicationName = _serviceInformationProvider.GetServiceInformation().ToString()
        });
    }

    public override Task WatchResources(WatchResourcesRequest request,
        IServerStreamWriter<WatchResourcesUpdate> responseStream, ServerCallContext context)
    {
        return base.WatchResources(request, responseStream, context);
    }
}

// ReSharper disable once UnusedType.Global
internal static partial class Log
{
    [LoggerMessage(LogLevel.Trace, "Returning application information")]
    public static partial void ReturningApplicationInformation(this ILogger logger);
}
