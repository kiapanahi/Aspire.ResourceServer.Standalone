using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

public interface IResourceProvider
{
    IAsyncEnumerable<string> GerResourceLogs(string resourceName, CancellationToken cancellationToken);
    Task<List<Resource>> GetResourcesAsync();
}
