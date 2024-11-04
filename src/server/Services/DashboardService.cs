using Aspire.ResourceServer.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Proto.V1;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

namespace Server.Services;

internal sealed class DashboardService : Aspire.ResourceService.Proto.V1.DashboardService.DashboardServiceBase
{
    private readonly ILogger<DashboardService> _logger;
    private readonly IServiceInformationProvider _serviceInformationProvider;

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

    public override async Task WatchResources(WatchResourcesRequest request,
        IServerStreamWriter<WatchResourcesUpdate> responseStream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(
                new WatchResourcesUpdate
                {
                    InitialData = new InitialResourceData
                    {
                        Resources =
                        {
                            new Resource
                            {
                                Name = "my-resource",
                                State = "foobar",
                                DisplayName = "MY FREAKING RESOURCE",
                                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.Now),
                                ResourceType = "EXTERNAL RESOURCE",
                                Uid = Guid.NewGuid().ToString("D")
                            }
                        }
                    }
                }, context.CancellationToken);
        }
    }
}

// ReSharper disable once UnusedType.Global
internal static partial class Log
{
    [LoggerMessage(LogLevel.Trace, "Returning application information")]
    public static partial void ReturningApplicationInformation(this ILogger logger);
}
