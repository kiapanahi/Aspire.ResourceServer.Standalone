using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public class ResourceNotificationServiceIntegrationTests
{
    [Fact]
    public async Task GetResources_ShouldAggregateFromMultipleProviders()
    {
        // Arrange
        var logger = new Mock<ILogger<ResourceNotificationService>>();
        
        var provider1 = CreateMockProvider("provider1-resource", "running");
        var provider2 = CreateMockProvider("provider2-resource", "exited");
        
        var notificationService = new ResourceNotificationService(new[] { provider1.Object, provider2.Object }, logger.Object);

        // Act
        var result = await notificationService.GetResources(CancellationToken.None);

        // Assert
        result.InitialData.Should().HaveCount(2);
        result.InitialData.Should().Contain(r => r.Name == "provider1-resource");
        result.InitialData.Should().Contain(r => r.Name == "provider2-resource");
    }

    [Fact]
    public async Task GetResourceLogs_ShouldRouteToCorrectProvider()
    {
        // Arrange
        var logger = new Mock<ILogger<ResourceNotificationService>>();
        
        var provider1 = CreateMockProvider("provider1-resource", "running");
        var provider2 = CreateMockProvider("provider2-resource", "exited");
        
        // Set up log entries
        provider1.Setup(p => p.GetResourceLogs("provider1-resource", It.IsAny<CancellationToken>()))
            .Returns(CreateLogStream("provider1-resource", "Log from provider 1"));
        
        provider2.Setup(p => p.GetResourceLogs("provider2-resource", It.IsAny<CancellationToken>()))
            .Returns(CreateLogStream("provider2-resource", "Log from provider 2"));

        var notificationService = new ResourceNotificationService(new[] { provider1.Object, provider2.Object }, logger.Object);
        
        // First call GetResources to populate the resource-provider mapping
        await notificationService.GetResources(CancellationToken.None);

        // Act
        var logs = new List<ResourceLogEntry>();
        await foreach (var log in notificationService.GetResourceLogs("provider1-resource", CancellationToken.None))
        {
            logs.Add(log);
        }

        // Assert
        logs.Should().HaveCount(1);
        logs[0].ResourceName.Should().Be("provider1-resource");
        logs[0].Text.Should().Be("Log from provider 1");
    }

    [Fact]
    public async Task GetResourceLogs_ShouldReturnEmpty_WhenResourceNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<ResourceNotificationService>>();
        var provider1 = CreateMockProvider("existing-resource", "running");
        var notificationService = new ResourceNotificationService(new[] { provider1.Object }, logger.Object);

        // Act
        var logs = new List<ResourceLogEntry>();
        await foreach (var log in notificationService.GetResourceLogs("non-existing-resource", CancellationToken.None))
        {
            logs.Add(log);
        }

        // Assert
        logs.Should().BeEmpty();
    }

    private static Mock<IResourceProvider> CreateMockProvider(string resourceName, string state)
    {
        var provider = new Mock<IResourceProvider>();
        
        var resource = new Resource
        {
            Name = resourceName,
            ResourceType = "Container",
            DisplayName = resourceName,
            Uid = Guid.NewGuid().ToString(),
            State = state,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var subscription = new ResourceSubscription(
            new[] { resource },
            CreateEmptyChangeStream()
        );

        provider.Setup(p => p.GetResources(It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        return provider;
    }

    private static async IAsyncEnumerable<WatchResourcesChange?> CreateEmptyChangeStream()
    {
        await Task.Delay(1).ConfigureAwait(false); // Make it async
        yield break; // Empty stream
    }

    private static async IAsyncEnumerable<ResourceLogEntry> CreateLogStream(string resourceName, string logText)
    {
        await Task.Delay(1).ConfigureAwait(false); // Make it async
        yield return new ResourceLogEntry(resourceName, logText);
    }
}