using Aspire.ResourceService.Standalone.Server.ResourceProviders;

using Docker.DotNet;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResourceProviderShouldRegisterDockerClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResourceProvider();
        var serviceProvider = services.BuildServiceProvider();
        var dockerClient = serviceProvider.GetService<IDockerClient>();

        // Assert
        dockerClient.Should().NotBeNull();
    }

    [Fact]
    public void AddResourceProviderShouldRegisterResourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResourceProvider();
        var serviceProvider = services.BuildServiceProvider();
        var resourceProvider = serviceProvider.GetService<IResourceProvider>();

        // Assert
        resourceProvider.Should().NotBeNull();
        resourceProvider.Should().BeOfType<DockerResourceProvider>();
    }
}
