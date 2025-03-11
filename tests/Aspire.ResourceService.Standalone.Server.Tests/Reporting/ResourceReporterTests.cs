using Aspire.Hosting.Dashboard;
using Aspire.ResourceService.Standalone.Server.Reporting;
using Aspire.ResourceService.Standalone.Server.Tests.Helpers;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Standalone.Server.Tests.Reporting;

// A fake implementation of ResourceSnapshot for testing purposes.
internal sealed class FakeResourceSnapshot : ResourceSnapshot
{
    public override string ResourceType => "Fake";

    protected override IEnumerable<(string Key, Value Value, bool IsSensitive)> GetProperties()
    {
        return [];
    }
}

public sealed class ResourceReporterTests
{
    [Fact]
    public async Task EmptyInitialStateWhenReporterIsCold()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(cts.Token);
        var subscription = reporter.SubscribeResources();

        subscription.InitialState.Should().BeEmpty();
        await cts.CancelAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ExpectedInitialResourcesWhenSubscribing()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(cts.Token);

        var a = CreateResourceSnapshot("A");
        var b = CreateResourceSnapshot("B");
        var deleted = CreateResourceSnapshot("deleted");
        var c = CreateResourceSnapshot("C");

        await reporter.UpdateAsync(a, ResourceSnapshotChangeType.Upsert).ConfigureAwait(true);
        await reporter.UpdateAsync(b, ResourceSnapshotChangeType.Upsert).ConfigureAwait(true);
        await reporter.UpdateAsync(c, ResourceSnapshotChangeType.Upsert).ConfigureAwait(true);
        await reporter.UpdateAsync(deleted, ResourceSnapshotChangeType.Delete).ConfigureAwait(true);

        var (initial, _) = reporter.SubscribeResources();

        initial.Should().HaveCount(3);
        initial.Should().Contain(a);
        initial.Should().Contain(b);
        initial.Should().Contain(c);
        initial.Should().NotContain(deleted);

        await cts.CancelAsync().DefaultTimeout();
    }

    [Fact]
    public async Task UpdatesAreStreamed()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(cts.Token);

        var (_, subscription) = reporter.SubscribeResources();

        var tcs = new TaskCompletionSource<IReadOnlyList<ResourceSnapshotChange>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = Task.Run(async () =>
        {
            await foreach (var change in subscription.ConfigureAwait(true))
            {
                tcs.SetResult(change);
            }
        });

        var a = CreateResourceSnapshot("A");

        await reporter.UpdateAsync(a, ResourceSnapshotChangeType.Upsert).DefaultTimeout().ConfigureAwait(true);

        var change = await tcs.Task.DefaultTimeout();

        change.Should().ContainSingle();

        await cts.CancelAsync().DefaultTimeout();

        try
        {
            await task.DefaultTimeout().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // swallow
        }

    }

    private static ResourceSnapshot CreateResourceSnapshot(string resourceName)
    {
        return new FakeResourceSnapshot
        {
            Name = resourceName,
            DisplayName = $"{resourceName} Display Name",
            Uid = Guid.NewGuid().ToString(),
            State = "Active",
            StateStyle = null,
            ExitCode = 0,
            CreationTimeStamp = DateTime.UtcNow,
            StartTimeStamp = DateTime.UtcNow,
            StopTimeStamp = null,
            Commands = [],
            Environment = [],
            HealthReports = [],
            Relationships = [],
            Urls = [],
            Volumes = []

        };
    }

}
