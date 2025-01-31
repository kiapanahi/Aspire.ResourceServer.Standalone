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

public class ApplicationInformationTests
{
    private readonly Mock<IServiceInformationProvider> _mockServiceInformationProvider;
    private readonly Mock<IResourceProvider> _mockResourceProvider;
    private readonly Mock<IHostApplicationLifetime> _mockHostApplicationLifetime;
    private readonly DashboardServiceImpl _dashboardService;

    public ApplicationInformationTests()
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
}
