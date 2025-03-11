using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.Reporting;
using Grpc.Core;

using DashboardGrpcService = Aspire.ResourceService.Proto.V1.DashboardService.DashboardServiceBase;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal sealed class DashboardService : DashboardGrpcService
{
    private readonly DashboardData _dashboardData;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(DashboardData dashboardData, IHostApplicationLifetime hostApplicationLifetime, ILogger<DashboardService> logger)
    {
        _dashboardData = dashboardData;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public override async Task<ApplicationInformationResponse> GetApplicationInformation(ApplicationInformationRequest request, ServerCallContext context)
    {
        _logger.ReturningApplicationInformation();
        var serviceInformation = await _dashboardData.GetResourceServerServiceInformation().ConfigureAwait(false);
        return new ApplicationInformationResponse { ApplicationName = serviceInformation.Name };
    }

    public override async Task WatchResources(WatchResourcesRequest request, IServerStreamWriter<WatchResourcesUpdate> responseStream, ServerCallContext context)
    {
        await ExecuteAsync(WatchResourcesInternal, context).ConfigureAwait(false);

        async Task WatchResourcesInternal(CancellationToken cancellationToken)
        {
            var (initialData, updates) = _dashboardData.SubscribeResources();

            _logger.LogCompilingInitialResources();
            var data = new InitialResourceData();
            foreach (var snapshot in initialData)
            {
                data.Resources.Add(Resource.FromSnapshot(snapshot));
            }
            _logger.LogInitialResourcesCompiled();

            _logger.WritingInitialResourcesToStream();
            await responseStream.WriteAsync(new WatchResourcesUpdate { InitialData = data }, cancellationToken).ConfigureAwait(false);
            _logger.InitialResourcesWroteToStreamSuccessfully();

            await foreach (var batch in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var changes = new WatchResourcesChanges();

                foreach (var update in batch)
                {
                    var change = new WatchResourcesChange();

                    if (update.ChangeType is ResourceSnapshotChangeType.Upsert)
                    {
                        change.Upsert = Resource.FromSnapshot(update.Resource);
                    }
                    else if (update.ChangeType is ResourceSnapshotChangeType.Delete)
                    {
                        change.Delete = new() { ResourceName = update.Resource.Name, ResourceType = update.Resource.ResourceType };
                    }
                    else
                    {
                        throw new FormatException($"Unexpected {nameof(ResourceSnapshotChange)} type: {update.ChangeType}");
                    }

                    _logger.LogGotResourceUpdate(change);
                    changes.Value.Add(change);
                }

                await responseStream.WriteAsync(new() { Changes = changes }, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override Task WatchResourceConsoleLogs(WatchResourceConsoleLogsRequest request, IServerStreamWriter<WatchResourceConsoleLogsUpdate> responseStream, ServerCallContext context)
    {
        _logger.StartedWatchingResourceConsoleLogs(request.ResourceName);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task> execute, ServerCallContext serverCallContext)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping, serverCallContext.CancellationToken);

        try
        {
            await execute(cts.Token).ConfigureAwait(false);
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
            _logger.LogErrorWatchingResources(serverCallContext.Method, ex);
            throw;
        }
    }
}
