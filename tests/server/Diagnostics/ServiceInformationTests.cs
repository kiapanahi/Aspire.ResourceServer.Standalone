using Aspire.ResourceServer.Standalone.Server.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.ResourceServer.Standalone.Server.Tests.Diagnostics;

public sealed class ServiceInformationTests
{

    [Fact]
    public void VersionGuard()
    {
        IServiceInformationProvider sut = new AssemblyServiceInformationProvider();


        sut.GetServiceInformation().Version.Should().Be("0.0.1");
        sut.GetServiceInformation().Name.Should().Be("Aspire.ResourceServer.Standalone.Server");
    }

    [Fact]
    public void DefaultServiceInformationProviderIsAssemblyServiceInformationProvider()
    {
        var sp = new ServiceCollection()
            .AddServiceInformationProvider()
            .BuildServiceProvider();

        var sut = sp.GetRequiredService<IServiceInformationProvider>();
        sut.Should().BeOfType<AssemblyServiceInformationProvider>();
    }

    [Fact]
    public void MultipleServiceInformationProviderResolutionsResultInSameInstance()
    {
        var sp = new ServiceCollection()
            .AddServiceInformationProvider()
            .BuildServiceProvider();

        var @base = sp.GetRequiredService<IServiceInformationProvider>();

        foreach (var instance in Enumerable.Range(1, 10).Select(_ => sp.GetRequiredService<IServiceInformationProvider>()))
        {
            instance.Should().BeSameAs(@base);
            instance.Should().BeEquivalentTo(@base);
        }
    }

    [Fact]
    public void MockServiceInformationProviderIsResolved()
    {
        var sp = new ServiceCollection()
            .AddServiceInformationProvider<MockServiceInformationProvider>()
            .BuildServiceProvider();

        var sut = sp.GetRequiredService<IServiceInformationProvider>();
        sut.Should().BeOfType<MockServiceInformationProvider>();

        var si = sut.GetServiceInformation();

        si.Name.Should().Be("mock-name");
        si.Version.Should().Be("mock-version");
    }


    private sealed class MockServiceInformationProvider : IServiceInformationProvider
    {
        public ServiceInformation GetServiceInformation()
        {
            return new("mock-name", "mock-version");
        }
    }
}
