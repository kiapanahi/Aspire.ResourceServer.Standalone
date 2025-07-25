using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public class ResourceNotificationServiceTests
{
    [Fact]
    public void AddResourceProvider_ShouldRegisterMultipleProviders_WhenConfigurationHasDockerOnly()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestMultipleDockerProviderConfiguration();

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var dockerProvider = serviceProvider.GetService<DockerResourceProvider>();
        var notificationService = serviceProvider.GetService<IResourceNotificationService>();
        var primaryProvider = serviceProvider.GetService<IResourceProvider>();

        dockerProvider.Should().NotBeNull();
        notificationService.Should().NotBeNull();
        primaryProvider.Should().NotBeNull();
        primaryProvider.Should().BeOfType<ResourceNotificationService>();
    }

    [Fact]
    public void AddResourceProvider_ShouldRegisterSingleProvider_WhenConfigurationHasSingleProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = TestConfigurationBuilder.TestConfigurationBuilder.GetTestConfiguration("docker");

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var dockerProvider = serviceProvider.GetService<DockerResourceProvider>();
        var notificationService = serviceProvider.GetService<IResourceNotificationService>();
        var primaryProvider = serviceProvider.GetService<IResourceProvider>();

        dockerProvider.Should().NotBeNull();
        notificationService.Should().NotBeNull();
        primaryProvider.Should().NotBeNull();
        primaryProvider.Should().BeOfType<ResourceNotificationService>();
    }

    [Fact]
    public void AddResourceProvider_ShouldThrowException_WhenUnknownProviderSpecified()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestConfiguration("unknown");

        // Act & Assert
        var action = () => services.AddResourceProvider(config);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown resource provider: unknown");
    }

    [Fact]
    public void AddResourceProvider_ShouldNotRegisterProviders_WhenNoProvidersSpecified()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = GetTestEmptyConfiguration();

        // Act
        services.AddResourceProvider(config);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var dockerProvider = serviceProvider.GetService<DockerResourceProvider>();
        var k8sProvider = serviceProvider.GetService<KubernetesResourceProvider>();
        var notificationService = serviceProvider.GetService<IResourceNotificationService>();
        var primaryProvider = serviceProvider.GetService<IResourceProvider>();

        dockerProvider.Should().BeNull();
        k8sProvider.Should().BeNull();
        notificationService.Should().BeNull();
        primaryProvider.Should().BeNull();
    }

    private static IConfiguration GetTestMultipleDockerProviderConfiguration()
    {
        var config = new ConfigurationBuilder();
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ResourceProviders:0", "docker" },
            { "ResourceProvider", "docker" } // This tests both single and multiple config paths
        });
        return config.Build();
    }

    private static IConfiguration GetTestConfiguration(string resourceProvider)
    {
        if (resourceProvider == "unknown")
        {
            var config = new ConfigurationBuilder();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ResourceProvider", "unknown" }
            });
            return config.Build();
        }

        return TestConfigurationBuilder.TestConfigurationBuilder.GetTestConfiguration(resourceProvider);
    }

    private static IConfiguration GetTestEmptyConfiguration()
    {
        var config = new ConfigurationBuilder();
        config.AddInMemoryCollection(new Dictionary<string, string?>());
        return config.Build();
    }
}