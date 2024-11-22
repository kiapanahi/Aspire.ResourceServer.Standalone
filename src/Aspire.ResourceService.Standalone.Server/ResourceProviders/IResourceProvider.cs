using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

internal interface IResourceProvider
{
    Task<List<Resource>> GetResourcesAsync();
}
