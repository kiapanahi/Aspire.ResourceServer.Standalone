using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Docker.DotNet;
using FluentAssertions;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.ResourceService.Standalone.Server.Tests.TestConfigurationBuilder.TestConfigurationBuilder;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResourceProviderShouldRegisterDockerClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestConfiguration("docker");

        // Act
        services.AddResourceProvider(config);
        var serviceProvider = services.BuildServiceProvider();
        var dockerClient = serviceProvider.GetService<IDockerClient>();

        // Assert
        dockerClient.Should().NotBeNull();
    }

    [Fact]
    public void AddResourceProviderShouldRegisterKubernetesClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestConfiguration("k8s");

        // Act
        services.AddResourceProvider(config);
        var serviceProvider = services.BuildServiceProvider();
        var dockerClient = serviceProvider.GetService<IKubernetes>();

        // Assert
        dockerClient.Should().NotBeNull();
    }

    [Fact]
    public void AddResourceProviderShouldRegisterDockerResourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestConfiguration("docker");

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var resourceProvider = serviceProvider.GetService<IResourceProvider>();

        // Assert
        resourceProvider.Should().NotBeNull();
        resourceProvider.Should().BeOfType<DockerResourceProvider>();
    }

    [Fact]
    public void AddResourceProviderShouldRegisterKubernetesResourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestConfiguration("k8s");

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var resourceProvider = serviceProvider.GetService<IResourceProvider>();

        // Assert
        resourceProvider.Should().NotBeNull();
        resourceProvider.Should().BeOfType<KubernetesResourceProvider>();
    }
}
