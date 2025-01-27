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

public class DashboardServiceTests
{
    private readonly Mock<IServiceInformationProvider> _mockServiceInformationProvider;
    private readonly Mock<IResourceProvider> _mockResourceProvider;
    private readonly Mock<IHostApplicationLifetime> _mockHostApplicationLifetime;
    private readonly DashboardServiceImpl _dashboardService;

    public DashboardServiceTests()
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
    public async Task GetApplicationInformationTest()
    {
        // Arrange
        var expectedName = Constants.ServiceName;
        _mockServiceInformationProvider
            .Setup(x => x.GetServiceInformation())
            .Returns(new ServiceInformation { Name = expectedName });

        var request = new ApplicationInformationRequest();
        var context = TestServerCallContext.Create();

        // Act
        var response = await _dashboardService.GetApplicationInformation(request, context).ConfigureAwait(true);

        // Assert
        response.ApplicationName.Should().Be(expectedName);
    }

    [Fact]
    public async Task WatchResourcesStreamsData()
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
    public async Task WatchResourcesLogs()
    {
        // Arrange
        var logs = new List<string> { "Log1", "Log2" };
        _mockResourceProvider
            .Setup(x => x.GerResourceLogs(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(logs.ToAsyncEnumerable());

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<WatchResourceConsoleLogsUpdate>(callContext);

        var request = new WatchResourceConsoleLogsRequest { ResourceName = "resource" };

        // Act
        var call = _dashboardService.WatchResourceConsoleLogs(request, responseStream, callContext);

        call.IsCompleted.Should().BeTrue();
        await call.ConfigureAwait(true);

        responseStream.Complete();

        // Assert
        var update = await responseStream.ReadNextAsync().ConfigureAwait(true);
        update.Should().NotBeNull();
        update!.LogLines.Should().HaveCount(2);
        update.LogLines[0].Text.Should().Be(logs[0]);
        update.LogLines[1].Text.Should().Be(logs[1]);
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
