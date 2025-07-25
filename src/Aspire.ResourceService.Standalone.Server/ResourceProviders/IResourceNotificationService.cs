using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

/// <summary>
/// Service that aggregates resources and logs from multiple resource providers.
/// </summary>
public interface IResourceNotificationService
{
    /// <summary>
    /// Gets aggregated resources from all registered providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A subscription containing initial resources and aggregated change stream</returns>
    Task<ResourceSubscription> GetResources(CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets logs for a specific resource by routing to the correct provider.
    /// </summary>
    /// <param name="resourceName">Name of the resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of log entries for the resource</returns>
    IAsyncEnumerable<ResourceLogEntry> GetResourceLogs(string resourceName, CancellationToken cancellationToken);
}