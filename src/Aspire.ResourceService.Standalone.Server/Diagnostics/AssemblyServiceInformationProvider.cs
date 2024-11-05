namespace Aspire.ResourceService.Standalone.Server.Diagnostics;

internal sealed class AssemblyServiceInformationProvider : IServiceInformationProvider
{
    private ServiceInformation ServiceInformation { get; } = new(
        Constants.ServiceName, ServerDiagnostics.ServiceVersion);

    public ServiceInformation GetServiceInformation()
    {
        return ServiceInformation;
    }
}
