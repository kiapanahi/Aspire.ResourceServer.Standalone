using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Docker.DotNet;
using FluentAssertions;
using k8s;
using Microsoft.Extensions.Configuration;
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
        resourceProvider.Should().BeOfType<ResourceNotificationService>();
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
        resourceProvider.Should().BeOfType<ResourceNotificationService>();
    }

    [Fact]
    public void AddResourceProviderShouldRegisterMultipleProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestMultipleProvidersConfiguration();

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var dockerProvider = serviceProvider.GetService<DockerResourceProvider>();
        var k8sProvider = serviceProvider.GetService<KubernetesResourceProvider>();
        var resourceProvider = serviceProvider.GetService<IResourceProvider>();
        var notificationService = serviceProvider.GetService<IResourceNotificationService>();

        dockerProvider.Should().NotBeNull();
        k8sProvider.Should().NotBeNull();
        resourceProvider.Should().NotBeNull();
        resourceProvider.Should().BeOfType<ResourceNotificationService>();
        notificationService.Should().NotBeNull();
    }

    private static IConfiguration GetTestMultipleProvidersConfiguration()
    {
        var config = new ConfigurationBuilder();
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ResourceProviders:0", "docker" },
            { "ResourceProviders:1", "k8s" },
            { "KubernetesResourceProviderConfiguration:Namespace", "test" },
            { "KubernetesResourceProviderConfiguration:Servicenames", "redis;rabbitmq;mongo" }
        });
        return config.Build();
    }
}
