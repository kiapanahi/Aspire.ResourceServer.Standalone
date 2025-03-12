using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.ResourceService.Standalone.Server.AspireModels;
using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.Reporting;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal sealed class DashboardData : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly ResourceReporter _resourceReporter;
    private readonly IServiceInformationProvider _serviceInformationProvider;
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly ILogger<DashboardData> _logger;

    public DashboardData(IServiceInformationProvider serviceInformationProvider,
        ResourceNotificationService resourceNotificationService,
        ILogger<DashboardData> logger)
    {
        _resourceReporter = new ResourceReporter(_cts.Token);
        _serviceInformationProvider = serviceInformationProvider;
        _resourceNotificationService = resourceNotificationService;
        _logger = logger;
        var cancellationToken = _cts.Token;

        Task.Run(async () =>
        {
            static GenericResourceSnapshot CreateResourceSnapshot(IResource resource, string resourceId, DateTime creationTimestamp, CustomResourceSnapshot snapshot)
            {
                return new GenericResourceSnapshot(snapshot)
                {
                    Uid = resourceId,
                    CreationTimeStamp = snapshot.CreationTimeStamp ?? creationTimestamp,
                    StartTimeStamp = snapshot.StartTimeStamp,
                    StopTimeStamp = snapshot.StopTimeStamp,
                    Name = resourceId,
                    DisplayName = resource.Name,
                    Urls = snapshot.Urls,
                    Volumes = snapshot.Volumes,
                    Environment = snapshot.EnvironmentVariables,
                    Relationships = snapshot.Relationships,
                    ExitCode = snapshot.ExitCode,
                    State = snapshot.State?.Text,
                    StateStyle = snapshot.State?.Style,
                    HealthReports = snapshot.HealthReports,
                    Commands = snapshot.Commands
                };
            }

            var timestamp = DateTime.UtcNow;

            await foreach (var @event in resourceNotificationService.WatchAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var snapshot = CreateResourceSnapshot(@event.Resource, @event.ResourceId, timestamp, @event.Snapshot);

                    _logger.UpdatingResourceSnapshot(snapshot.Name, snapshot.DisplayName, snapshot.State);

                    await _resourceReporter.UpdateAsync(snapshot, ResourceSnapshotChangeType.Upsert)
                            .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.ErrorUpdatingResourceSnapshot(@event.Resource.Name, ex);
                }
            }
        },
        cancellationToken);
    }
    internal ValueTask<ServiceInformation> GetResourceServerServiceInformation() => ValueTask.FromResult(_serviceInformationProvider.GetServiceInformation());
    internal ResourceSnapshotSubscription SubscribeResources() => _resourceReporter.SubscribeResources();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Updating resource snapshot for {Name}/{DisplayName}: {State}")]
    public static partial void UpdatingResourceSnapshot(this ILogger<DashboardData> logger, string name, string displayName, string? state);

    [LoggerMessage(LogLevel.Error, "Error updating resource snapshot for {Name}")]
    public static partial void ErrorUpdatingResourceSnapshot(this ILogger<DashboardData> logger, string name, Exception ex);
}
