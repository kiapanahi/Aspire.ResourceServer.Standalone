using System.Collections.Immutable;
using Aspire.Hosting.Dashboard;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

internal sealed record ResourceSnapshotSubscription(
    ImmutableArray<ResourceSnapshot> InitialState,
    IAsyncEnumerable<IReadOnlyList<ResourceSnapshotChange>> Subscription);

