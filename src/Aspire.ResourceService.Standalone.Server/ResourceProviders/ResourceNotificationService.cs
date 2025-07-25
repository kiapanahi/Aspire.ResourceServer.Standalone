using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders;

/// <summary>
/// Service that aggregates resources and logs from multiple resource providers.
/// </summary>
internal sealed partial class ResourceNotificationService : IResourceNotificationService, IResourceProvider
{
    private readonly IEnumerable<IResourceProvider> _resourceProviders;
    private readonly ILogger<ResourceNotificationService> _logger;
    private readonly Dictionary<string, IResourceProvider> _resourceProviderMap = new();

    public ResourceNotificationService(
        IEnumerable<IResourceProvider> resourceProviders,
        ILogger<ResourceNotificationService> logger)
    {
        _resourceProviders = resourceProviders;
        _logger = logger;
    }

    public async Task<ResourceSubscription> GetResources(CancellationToken cancellationToken)
    {
        _logger.LogGettingResourcesFromProviders(_resourceProviders.Count());

        var providerSubscriptionMap = new Dictionary<IResourceProvider, ResourceSubscription>();
        var allInitialResources = new List<Resource>();

        // Get initial resources from all providers
        foreach (var provider in _resourceProviders)
        {
            try
            {
                var subscription = await provider.GetResources(cancellationToken).ConfigureAwait(false);
                providerSubscriptionMap[provider] = subscription;
                
                // Track which provider owns which resources for log routing
                foreach (var resource in subscription.InitialData)
                {
                    _resourceProviderMap[resource.Name] = provider;
                }
                
                allInitialResources.AddRange(subscription.InitialData);
                _logger.LogProviderReturnedResources(provider.GetType().Name, subscription.InitialData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToGetResourcesFromProvider(ex, provider.GetType().Name);
                // Continue with other providers even if one fails
            }
        }

        var aggregatedChangeStream = AggregateChangeStreams(providerSubscriptionMap, cancellationToken);
        
        _logger.LogAggregatedResources(allInitialResources.Count, _resourceProviders.Count());

        return new ResourceSubscription(allInitialResources.AsReadOnly(), aggregatedChangeStream);
    }

    public async IAsyncEnumerable<ResourceLogEntry> GetResourceLogs(
        string resourceName, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_resourceProviderMap.TryGetValue(resourceName, out var provider))
        {
            _logger.LogNoProviderFoundForResource(resourceName);
            yield break;
        }

        _logger.LogRoutingLogsToProvider(resourceName, provider.GetType().Name);

        await foreach (var logEntry in provider.GetResourceLogs(resourceName, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return logEntry;
        }
    }

    private async IAsyncEnumerable<WatchResourcesChange?> AggregateChangeStreams(
        IReadOnlyDictionary<IResourceProvider, ResourceSubscription> providerSubscriptionMap,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (providerSubscriptionMap.Count == 0)
        {
            yield break;
        }

        // Create a channel to merge all change streams
        var channel = Channel.CreateUnbounded<WatchResourcesChange?>();
        var writer = channel.Writer;

        // Start tasks to read from each provider's change stream
        var readerTasks = new List<Task>();
        
        foreach (var (provider, subscription) in providerSubscriptionMap)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    await foreach (var change in subscription.ChangeStream.WithCancellation(cancellationToken)
                                       .ConfigureAwait(false))
                    {
                        // Update the resource provider map when resources are added/updated
                        if (change?.Upsert != null)
                        {
                            _resourceProviderMap[change.Upsert.Name] = provider;
                        }
                        
                        // Remove from map when resources are deleted
                        if (change?.Delete != null)
                        {
                            _resourceProviderMap.Remove(change.Delete.ResourceName);
                        }

                        await writer.WriteAsync(change, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    _logger.LogErrorReadingFromProviderChangeStream(ex, provider.GetType().Name);
                }
            }, cancellationToken);
            
            readerTasks.Add(task);
        }

        // Start a task to close the writer when all readers complete
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(readerTasks).ConfigureAwait(false);
            }
            finally
            {
                writer.Complete();
            }
        }, cancellationToken);

        // Yield changes as they come from the aggregated channel
        await foreach (var change in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return change;
        }
    }
}

internal static partial class ResourceNotificationServiceLogs
{
    [LoggerMessage(LogLevel.Debug, "Getting resources from {ProviderCount} providers")]
    public static partial void LogGettingResourcesFromProviders(this ILogger<ResourceNotificationService> logger, int providerCount);

    [LoggerMessage(LogLevel.Debug, "Provider {ProviderType} returned {ResourceCount} resources")]
    public static partial void LogProviderReturnedResources(this ILogger<ResourceNotificationService> logger, string providerType, int resourceCount);

    [LoggerMessage(LogLevel.Warning, "Failed to get resources from provider {ProviderType}")]
    public static partial void LogFailedToGetResourcesFromProvider(this ILogger<ResourceNotificationService> logger, Exception ex, string providerType);

    [LoggerMessage(LogLevel.Information, "Aggregated {TotalResources} resources from {ProviderCount} providers")]
    public static partial void LogAggregatedResources(this ILogger<ResourceNotificationService> logger, int totalResources, int providerCount);

    [LoggerMessage(LogLevel.Warning, "No provider found for resource {ResourceName}")]
    public static partial void LogNoProviderFoundForResource(this ILogger<ResourceNotificationService> logger, string resourceName);

    [LoggerMessage(LogLevel.Debug, "Routing logs for resource {ResourceName} to provider {ProviderType}")]
    public static partial void LogRoutingLogsToProvider(this ILogger<ResourceNotificationService> logger, string resourceName, string providerType);

    [LoggerMessage(LogLevel.Error, "Error reading from provider {ProviderType} change stream")]
    public static partial void LogErrorReadingFromProviderChangeStream(this ILogger<ResourceNotificationService> logger, Exception ex, string providerType);
}