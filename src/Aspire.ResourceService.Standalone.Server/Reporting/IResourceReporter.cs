using Aspire.Hosting.Dashboard;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal interface IResourceReporter : IDisposable
{
    ResourceSnapshotSubscription SubscribeResources();
    ValueTask UpdateAsync(ResourceSnapshot snapshot, ResourceSnapshotChangeType changeType);
}
