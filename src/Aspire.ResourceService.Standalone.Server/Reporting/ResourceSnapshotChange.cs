using Aspire.Hosting.Dashboard;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal sealed record ResourceSnapshotChange(
    ResourceSnapshotChangeType ChangeType,
    ResourceSnapshot Resource);
