using Aspire.ResourceServer.Standalone.Server.Diagnostics;

using FluentAssertions;

namespace Aspire.ResourceServer.Standalone.Server.Tests.Diagnostics;

public class ConstantsTests
{
    [Fact]
    public void TestServiceName()
    {
        Constants.ServiceName.Should().Be("Aspire.ResourceServer.Standalone.Server");
    }
}
