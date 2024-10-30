using System.Reflection;

namespace Server.Diagnostics;

public readonly record struct ServiceInformation(string Name, string Version);

public interface IServiceInformationProvider
{
    ServiceInformation GetServiceInformation();
}

internal sealed class AssemblyServiceInformationProvider : IServiceInformationProvider
{
    private const string ServiceName = "ASARS";
    private static readonly string ServiceVersion;

    private static readonly Assembly Assembly = typeof(AssemblyServiceInformationProvider).Assembly;

    static AssemblyServiceInformationProvider()
    {
        ServiceVersion = Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private ServiceInformation ServiceInformation { get; } = new ServiceInformation(ServiceName, ServiceVersion);

    public ServiceInformation GetServiceInformation()
    {
        return ServiceInformation;
    }
}