namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

public interface IResourceProvider
{
    ValueTask GetResources(CancellationToken cancellationToken);
}
