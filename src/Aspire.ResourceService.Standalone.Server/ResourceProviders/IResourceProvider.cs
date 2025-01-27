using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

public interface IResourceProvider
{
    IAsyncEnumerable<string> GerResourceLogs(string resourceName, CancellationToken cancellationToken);
    Task<ResourceSubscription> GetResources(CancellationToken cancellationToken);
}

public sealed record class ResourceSubscription(
    IReadOnlyList<Resource> InitialData,
    IAsyncEnumerable<WatchResourcesChange?> ChangeStream);
