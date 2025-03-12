
using Aspire.ResourceService.Standalone.Server.Reporting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceService.Standalone.Server.Tests.Reporting;
public sealed class ResourceNotificationServiceTests
{
    [Fact]
    public void ResourceNotificationServiceIsRegistered()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddResourceNotificationService();
        var serviceProvider = services.BuildServiceProvider();
        var resourceNotificationService = serviceProvider.GetRequiredService<ResourceNotificationService>();
        resourceNotificationService.Should().NotBeNull();
    }

    [Fact]
    public void ResourceNotificationServiceIsSingleton()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddResourceNotificationService();
        var serviceProvider = services.BuildServiceProvider();
        var resourceNotificationService1 = serviceProvider.GetRequiredService<ResourceNotificationService>();
        var resourceNotificationService2 = serviceProvider.GetRequiredService<ResourceNotificationService>();
        resourceNotificationService1.Should().BeSameAs(resourceNotificationService2);
    }
}
