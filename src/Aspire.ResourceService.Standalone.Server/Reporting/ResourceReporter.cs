using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.Dashboard;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal sealed class ResourceReporter
{
    private readonly object _syncLock = new();

    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, ResourceSnapshot> _resourceSnapshot = [];
    private readonly Channel<ResourceSnapshotChange> _resourceSnapshotChangeChannel;

    public ResourceReporter(CancellationToken cancellationToken)
    {
        _resourceSnapshotChangeChannel = Channel.CreateUnbounded<ResourceSnapshotChange>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
        _cancellationToken = cancellationToken;
    }

    public ResourceSnapshotSubscription SubscribeResources()
    {
        return new ResourceSnapshotSubscription(
            InitialState: [.. _resourceSnapshot.Select(s => s.Value)],
            Subscription: StreamUpdates());

        async IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> StreamUpdates([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(enumeratorCancellationToken, _cancellationToken);

            await foreach (var batch in _resourceSnapshotChangeChannel.GetBatchesAsync(cancellationToken: linked.Token).ConfigureAwait(false))
            {
                yield return batch;
            }
        }
    }

    public async ValueTask UpdateAsync(ResourceSnapshot snapshot, ResourceSnapshotChangeType changeType)
    {
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

        await _resourceSnapshotChangeChannel.Writer.WriteAsync(new ResourceSnapshotChange(changeType, snapshot), _cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _resourceSnapshotChangeChannel.Writer.Complete();
    }
}
