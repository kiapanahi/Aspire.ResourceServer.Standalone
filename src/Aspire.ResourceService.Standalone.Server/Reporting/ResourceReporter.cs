namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal sealed class ResourceReporter : IDisposable
{
    private readonly ILogger<ResourceReporter> _logger;
    private readonly CancellationTokenSource _cts = new();

    public ResourceReporter(ILogger<ResourceReporter> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    internal ResourceSnapshotSubscription SubscribeResources()
    {
        _logger.LogSubscribingToResourceSnapshots();
        throw new NotImplementedException();
    }
}

internal static partial class ResourceReporterLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Subscribing to resource snapshots.")]
    public static partial void LogSubscribingToResourceSnapshots(this ILogger<ResourceReporter> logger);
}
