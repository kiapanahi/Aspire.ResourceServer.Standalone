using System.Diagnostics;
using System.Reflection;

namespace Aspire.ResourceServer.Standalone.Server.Diagnostics;

internal static class ServerDiagnostics
{
    private static readonly Assembly Assembly = typeof(AssemblyServiceInformationProvider).Assembly;

    public static readonly string ServiceVersion;

    static ServerDiagnostics()
    {
        ServiceVersion = Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    public static ActivitySource ServerActivitySource = new(Constants.ServiceName, version: ServiceVersion);
}
