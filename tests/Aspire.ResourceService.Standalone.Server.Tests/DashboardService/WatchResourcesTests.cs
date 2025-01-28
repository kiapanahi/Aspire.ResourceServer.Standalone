using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.Tests.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using DashboardServiceImpl = Aspire.ResourceService.Standalone.Server.Services.DashboardService;

namespace Aspire.ResourceService.Standalone.Server.Tests.DashboardService;

public class WatchResourcesTests
{
    private readonly Mock<IServiceInformationProvider> _mockServiceInformationProvider;
    private readonly Mock<IResourceProvider> _mockResourceProvider;
    private readonly Mock<IHostApplicationLifetime> _mockHostApplicationLifetime;
    private readonly DashboardServiceImpl _dashboardService;

    public WatchResourcesTests()
    {
        _mockServiceInformationProvider = new Mock<IServiceInformationProvider>();
        _mockResourceProvider = new Mock<IResourceProvider>();
        _mockHostApplicationLifetime = new Mock<IHostApplicationLifetime>();
        _dashboardService = new DashboardServiceImpl(
            _mockServiceInformationProvider.Object,
            _mockResourceProvider.Object,
            _mockHostApplicationLifetime.Object,
            NullLogger<DashboardServiceImpl>.Instance);
    }

    [Fact]
    public async Task InitialSourceDataWithEmptyUpdateStream()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<WatchResourcesUpdate>(callContext);

        _mockResourceProvider
            .Setup(x => x.GetResources(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockResourceSubscription());

        // Act
        using var call = _dashboardService.WatchResources(new WatchResourcesRequest { IsReconnect = true }, responseStream, callContext);

        // Assert
        call.IsCompleted.Should().BeTrue();
        await call.ConfigureAwait(true);
        responseStream.Complete();

        var allMessages = new List<WatchResourcesUpdate>();
        await foreach (var message in responseStream.ReadAllAsync().ConfigureAwait(false))
        {
            allMessages.Add(message);
        }

        allMessages.Should().ContainSingle();

        static ResourceSubscription MockResourceSubscription()
        {
            return new ResourceSubscription([new()], Enumerable.Empty<WatchResourcesChange>().ToAsyncEnumerable());
        }
    }

    [Fact]
    public async Task WatchResourcesSkipsNullResourceUpdates()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<WatchResourcesUpdate>(callContext);

        var initialData = new List<Resource>();
        var updates = new List<WatchResourcesChange?>
        {
            null, // This should be skipped
            null
        }.ToAsyncEnumerable();

        _mockResourceProvider
            .Setup(x => x.GetResources(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceSubscription(initialData, updates));

        // Act
        using var call = _dashboardService.WatchResources(new WatchResourcesRequest(), responseStream, callContext);

        // Assert
        call.IsCompleted.Should().BeTrue();
        await call.ConfigureAwait(true);
        responseStream.Complete();

        var allMessages = new List<WatchResourcesUpdate>();
        await foreach (var message in responseStream.ReadAllAsync().ConfigureAwait(false))
        {
            allMessages.Add(message);
        }

        allMessages.Should().ContainSingle("only the initial resources should be returned");
        allMessages[0].InitialData.Should().NotBeNull();
        allMessages[0].Changes.Should().BeNull();
    }

    [Fact]
    public async Task WatchResourcesThrowsIfResourceUpdateIsEmpty()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<WatchResourcesUpdate>(callContext);

        var initialData = new List<Resource>();
        var emptyUpdate = new WatchResourcesChange(); // Neither Upsert nor Delete

        var updates = new List<WatchResourcesChange?> { emptyUpdate }.ToAsyncEnumerable();

        _mockResourceProvider
            .Setup(x => x.GetResources(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceSubscription(initialData, updates));

        // Act
        Func<Task> act = async () =>
        {
            using var call = _dashboardService.WatchResources(new WatchResourcesRequest(), responseStream, callContext);
            await call.ConfigureAwait(false);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>("Empty resource updates are not allowed");
    }

}

internal static class Extensions
{
    internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
        {
            yield return item;
        }
        await Task.CompletedTask.ConfigureAwait(true);
    }
}
