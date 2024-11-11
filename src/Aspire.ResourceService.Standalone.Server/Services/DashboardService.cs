using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.ResourceProviders;

using Grpc.Core;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal sealed class DashboardService : Proto.V1.DashboardService.DashboardServiceBase
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<DashboardService> _logger;
    private readonly IResourceProvider _resourceProvider;
    private readonly IServiceInformationProvider _serviceInformationProvider;

    public DashboardService(IServiceInformationProvider serviceInformationProvider,
        IResourceProvider resourceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DashboardService> logger)
    {
        _serviceInformationProvider = serviceInformationProvider;
        _resourceProvider = resourceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public override Task<ApplicationInformationResponse> GetApplicationInformation(
        ApplicationInformationRequest request, ServerCallContext context)
    {
        _logger.ReturningApplicationInformation();

        return Task.FromResult(new ApplicationInformationResponse
        {
            ApplicationName = _serviceInformationProvider.GetServiceInformation().Name
        });
    }

    public override async Task WatchResources(WatchResourcesRequest request,
        IServerStreamWriter<WatchResourcesUpdate> responseStream, ServerCallContext context)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping,
            context.CancellationToken);

        try
        {
            var resources = await _resourceProvider.GetResourcesAsync().ConfigureAwait(false);

            var data = new InitialResourceData();
            data.Resources.Add(resources);
            // foreach (var r in resources)
            // {
            //     var resource = new Resource
            //     {
            //         DisplayName = r.DisplayName,
            //         Name = r.Name,
            //         CreatedAt = r.CreatedAt,
            //         State = r.State,
            //         ResourceType = KnownResourceTypes.Container,
            //         Uid = r.Uid,
            //         Urls = r.Urls
            //     };
            //     resource.Urls.Add(r.Urls.Select(u => new Url
            //     {
            //         Name = u.Name, FullUrl = u.FullUrl, IsInternal = u.IsInternal
            //     }));
            //     data.Resources.Add(resource);
            // }

            await responseStream
                .WriteAsync(new WatchResourcesUpdate { InitialData = data }, cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return.
        }
        catch (IOException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Cancelled writes throw IOException.
        }
        catch (Exception ex)
        {
            _logger.LogErrorWatchingResources(context.Method, ex);
            throw;
        }
    }
}

// ReSharper disable once UnusedType.Global
internal static partial class Log
{
    [LoggerMessage(LogLevel.Trace, "Returning application information")]
    public static partial void ReturningApplicationInformation(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error executing service method {Method}")]
    public static partial void LogErrorWatchingResources(this ILogger logger, string method, Exception ex);
}
