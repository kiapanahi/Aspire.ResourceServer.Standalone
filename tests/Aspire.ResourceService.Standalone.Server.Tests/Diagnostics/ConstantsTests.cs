using Aspire.ResourceService.Standalone.Server.Diagnostics;

using FluentAssertions;

namespace Aspire.ResourceService.Standalone.Server.Tests.Diagnostics;

public class ConstantsTests
{
    [Fact]
    public void TestServiceName()
    {
        Constants.ServiceName.Should().Be("Aspire.ResourceService.Standalone.Server");
    }
}
