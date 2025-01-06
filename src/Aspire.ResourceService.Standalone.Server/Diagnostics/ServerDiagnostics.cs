using System.Diagnostics;
using System.Reflection;

namespace Aspire.ResourceService.Standalone.Server.Diagnostics;

internal static class ServerDiagnostics
{
    private static readonly Assembly s_assembly = typeof(AssemblyServiceInformationProvider).Assembly;

    public static readonly string ServiceVersion = s_assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static ActivitySource ServerActivitySource = new(Constants.ServiceName, ServiceVersion);
}
