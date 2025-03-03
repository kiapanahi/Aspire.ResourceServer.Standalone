using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.Dashboard;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal sealed class ResourceReporter : IDisposable
{
    private readonly object _syncLock = new();

    private readonly ILogger<ResourceReporter> _logger;
    private readonly CancellationTokenSource _cts;

    private readonly Dictionary<string, ResourceSnapshot> _resourceSnapshot = [];
    private readonly Channel<ResourceSnapshotChange> _resourceSnapshotChangeChannel;
    public ResourceReporter(ILogger<ResourceReporter> logger, CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _resourceSnapshotChangeChannel = Channel.CreateUnbounded<ResourceSnapshotChange>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
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
        return new ResourceSnapshotSubscription(
            InitialState: [.. _resourceSnapshot.Select(s => s.Value)],
            Subscription: StreamUpdates());

        async IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> StreamUpdates([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(enumeratorCancellationToken, _cts.Token);

            await foreach (var batch in _resourceSnapshotChangeChannel.GetBatchesAsync(cancellationToken: linked.Token).ConfigureAwait(false))
            {
                yield return batch;
            }
        }
    }

    internal async ValueTask UpdateAsync(ResourceSnapshot snapshot, ResourceSnapshotChangeType changeType)
    {
        _logger.LogReceivedResourceUpdate(snapshot.Name, changeType);
        lock (_syncLock)
        {
            switch (changeType)
            {
                case ResourceSnapshotChangeType.Upsert:
                    _resourceSnapshot[snapshot.Name] = snapshot;
                    break;

                case ResourceSnapshotChangeType.Delete:
                    _resourceSnapshot.Remove(snapshot.Name);
                    break;
            }
        }

        await _resourceSnapshotChangeChannel.Writer.WriteAsync(new ResourceSnapshotChange(changeType, snapshot), _cts.Token).ConfigureAwait(false);
    }
}

internal static partial class ResourceReporterLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "Subscribing to resource snapshots.")]
    public static partial void LogSubscribingToResourceSnapshots(this ILogger<ResourceReporter> logger);

    [LoggerMessage(LogLevel.Debug, "Updating resource snapshot {Snapshot} with change type {ChangeType}.")]
    public static partial void LogReceivedResourceUpdate(this ILogger<ResourceReporter> logger, string snapshot, ResourceSnapshotChangeType changeType);
}
