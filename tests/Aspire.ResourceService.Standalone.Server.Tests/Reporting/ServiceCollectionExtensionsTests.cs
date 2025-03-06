using Aspire.ResourceService.Standalone.Server.Reporting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceService.Standalone.Server.Tests.Reporting;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterResourceReporter()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddResourceReporter();
        var serviceProvider = services.BuildServiceProvider();
        var reporter = serviceProvider.GetRequiredService<IResourceReporter>();

        // Assert
        reporter.Should().NotBeNull();
        reporter.Should().BeOfType<ResourceReporter>();
    }

    [Fact]
    public void ResourceReporterIsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddResourceReporter();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var firstInstance = serviceProvider.GetService<IResourceReporter>();
        var secondInstance = serviceProvider.GetService<IResourceReporter>();

        // Assert
        firstInstance.Should().BeSameAs(secondInstance);
    }
}
