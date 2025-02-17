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

public class WatchResourceLogsTests
{
    private readonly Mock<IServiceInformationProvider> _mockServiceInformationProvider;
    private readonly Mock<IResourceProvider> _mockResourceProvider;
    private readonly Mock<IHostApplicationLifetime> _mockHostApplicationLifetime;
    private readonly DashboardServiceImpl _dashboardService;

    public WatchResourceLogsTests()
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
    public async Task WatchResourcesLogs()
    {
        // Arrange
        var logs = new List<ResourceLogEntry> { new("resource", "log-1"), new("resource", "log-2") };
        _mockResourceProvider
            .Setup(x => x.GetResourceLogs(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        update.LogLines[0].Text.Should().Be(logs[0].Line);
        update.LogLines[1].Text.Should().Be(logs[1].Line);
    }

}

