using Aspire.Hosting.Dashboard;
using Aspire.ResourceService.Standalone.Server.Reporting;
using Aspire.ResourceService.Standalone.Server.Tests.Helpers;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void DisposableReporter()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(NullLogger<ResourceReporter>.Instance, cts.Token);
        using (reporter)
        {
        }
        Action disposed = reporter.Dispose;
        disposed.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DisposableReporterWithCancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var reporter = new ResourceReporter(NullLogger<ResourceReporter>.Instance, cts.Token);
        using (reporter)
        {
        }
        Action disposed = reporter.Dispose;
        disposed.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task EmptyInitialStateWhenReporterIsCold()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(NullLogger<ResourceReporter>.Instance, cts.Token);
        var subscription = reporter.SubscribeResources();

        subscription.InitialState.Should().BeEmpty();
        await cts.CancelAsync().DefaultTimeout();
    }

    [Fact]
    public async Task ExpectedInitialResourcesWhenSubscribing()
    {
        var cts = new CancellationTokenSource();
        var reporter = new ResourceReporter(NullLogger<ResourceReporter>.Instance, cts.Token);

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
        var reporter = new ResourceReporter(NullLogger<ResourceReporter>.Instance, cts.Token);

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
            StopTimeStamp = null
        };
    }

    //[Fact]
    //public async Task UpdateAsyncUpsertAddsResourceAndSendsChange()
    //{
    //    // Arrange
    //    var snapshot = new FakeResourceSnapshot
    //    {
    //        Name = "TestResource",
    //        DisplayName = "Fake TestResource",
    //        Uid = Guid.NewGuid().ToString(),
    //        State = "Active",
    //        StateStyle = null,
    //        ExitCode = 0,
    //        CreationTimeStamp = DateTime.UtcNow,
    //        StartTimeStamp = DateTime.UtcNow,
    //        StopTimeStamp = null
    //    };
    //    var subscription = _reporter.SubscribeResources();
    //    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    //    // Act
    //    await _reporter.UpdateAsync(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(true);



    //    //// Read one batch from the subscription stream.
    //    //IAsyncEnumerator<IReadOnlyList<ResourceSnapshotChange>> enumerator = subscription.Subscription.GetAsyncEnumerator(cts.Token);
    //    //enumerator.MoveNextAsync().AsTask().Wait();
    //    //var batch = enumerator.Current;

    //    //// Assert
    //    //batch.Should().HaveCount(1);
    //    //var change = batch.Single();
    //    //change.ChangeType.Should().Be(ResourceSnapshotChangeType.Upsert);
    //    //change.Snapshot.Name.Should().Be(snapshot.Name);
    //}

    //[Fact]
    //public async Task UpdateAsync_Delete_RemovesResourceAndSendsChange()
    //{
    //    // Arrange
    //    var snapshot = new FakeResourceSnapshot
    //    {
    //        Name = "TestResource",
    //        DisplayName = "Fake TestResource",
    //        Uid = Guid.NewGuid().ToString(),
    //        State = "Active",
    //        StateStyle = null,
    //        ExitCode = 0,
    //        CreationTimeStamp = DateTime.UtcNow,
    //        StartTimeStamp = DateTime.UtcNow,
    //        StopTimeStamp = null
    //    };
    //    // First, add the resource.
    //    await _reporter.UpdateAsync(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(true);

    //    // Confirm it's added via subscription (consume the upsert batch)
    //    var subscription = _reporter.SubscribeResources();
    //    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
    //    IAsyncEnumerator<IReadOnlyList<ResourceSnapshotChange>> enumerator = subscription.Subscription.GetAsyncEnumerator(cts.Token);
    //    await enumerator.MoveNextAsync();
    //    // Now delete the resource.
    //    await _reporter.UpdateAsync(snapshot, ResourceSnapshotChangeType.Delete).ConfigureAwait(true);

    //    // Act: Read the delete batch.
    //    await enumerator.MoveNextAsync().ConfigureAwait(true);
    //    var batch = enumerator.Current;

    //    // Assert
    //    batch.Should().HaveCount(1);
    //    var change = batch.Single();
    //    change.ChangeType.Should().Be(ResourceSnapshotChangeType.Delete);
    //    change.Snapshot.Name.Should().Be(snapshot.Name);
    //}
}
