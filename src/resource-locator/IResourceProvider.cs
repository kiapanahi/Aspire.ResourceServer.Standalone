using AspireResouce = Aspire.ResourceService.Proto.V1.Resource;

namespace Aspire.ResourceServer.Standalone.ResourceLocator;
public interface IResourceProvider : IDisposable
{
    Task<IEnumerable<AspireResouce>> GetResourcesAsync();
}
