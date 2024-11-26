using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal interface IResourceProvider
{
    IAsyncEnumerable<string> GerResourceLogs(string resourceName, CancellationToken cancellationToken);
    Task<List<Resource>> GetResourcesAsync();
}
