using AspireResouce = Aspire.ResourceService.Proto.V1.Resource;

namespace Aspire.ResourceService.Standalone.ResourceProvider;
public interface IResourceProvider : IDisposable
{
    Task<IEnumerable<AspireResouce>> GetResourcesAsync();
}
