using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.Reporting;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal sealed class DashboardData : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly ResourceReporter _resourceReporter;
    private readonly IServiceInformationProvider _serviceInformationProvider;

    public DashboardData(IServiceInformationProvider serviceInformationProvider)
    {
        _resourceReporter = new ResourceReporter(_cts.Token);
        _serviceInformationProvider = serviceInformationProvider;
    }
    internal ValueTask<ServiceInformation> GetResourceServerServiceInformation() => ValueTask.FromResult(_serviceInformationProvider.GetServiceInformation());
    internal ResourceSnapshotSubscription SubscribeResources() => _resourceReporter.SubscribeResources();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
