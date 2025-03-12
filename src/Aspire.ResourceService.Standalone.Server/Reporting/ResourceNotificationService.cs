
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.ResourceService.Standalone.Server.AspireModels;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.ResourceService.Standalone.Server.Reporting;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public sealed class ResourceNotificationService : IDisposable
{
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger;
    private readonly CancellationTokenSource _disposing = new();
    private readonly object _onResourceUpdatedLock = new();

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resourceLoggerService">The resource logger service.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Notification updates will be cancelled when the service is disposed.
    }

    /// <summary>
    /// Watch for changes to the state for all resources.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<ResourceEvent> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ResourceEvent>();

        void WriteToChannel(ResourceEvent resourceEvent) =>
            channel.Writer.TryWrite(resourceEvent);

        lock (_onResourceUpdatedLock)
        {
            OnResourceUpdated += WriteToChannel;
        }

        // Return the last snapshot for each resource.
        // We do this after subscribing to the event to avoid missing any updates.

        // Keep track of the versions we have seen so far to avoid duplicates.
        var versionsSeen = new Dictionary<(IResource, string), long>();

        foreach (var state in _resourceNotificationStates)
        {
            var (resource, resourceId) = state.Key;

            if (state.Value.LastSnapshot is { } snapshot)
            {
                versionsSeen[state.Key] = snapshot.Version;

                yield return new ResourceEvent(resource, resourceId, snapshot);
            }
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // Skip events that are older than the max version we have seen so far. This avoids duplicates.
                if (versionsSeen.TryGetValue((item.Resource, item.ResourceId), out var maxVersionSeen) && item.Snapshot.Version <= maxVersionSeen)
                {
                    // We can remove the version from the seen list since we have seen it already.
                    // We only care about events we have returned to the caller
                    versionsSeen.Remove((item.Resource, item.ResourceId));
                    continue;
                }

                yield return item;
            }
        }
        finally
        {
            lock (_onResourceUpdatedLock)
            {
                OnResourceUpdated -= WriteToChannel;
            }

            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="resourceId"> The id of the resource.</param>
    /// <param name="stateFactory">A factory that creates the new state based on the previous state.</param>
    public Task PublishUpdateAsync(IResource resource, string resourceId, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        var notificationState = GetResourceNotificationState(resource, resourceId);

        lock (notificationState)
        {
            var previousState = GetCurrentSnapshot(resource, notificationState);

            var newState = stateFactory(previousState);

            // Increment the snapshot version, this is a per resource version.
            newState = newState with { Version = notificationState.GetNextVersion() };

            notificationState.LastSnapshot = newState;

            OnResourceUpdated?.Invoke(new ResourceEvent(resource, resourceId, newState));

            if (_logger.IsEnabled(LogLevel.Debug) && newState.State?.Text is { Length: > 0 } newStateText && !string.IsNullOrWhiteSpace(newStateText))
            {
                var previousStateText = previousState?.State?.Text;
                if (!string.IsNullOrWhiteSpace(previousStateText) && !string.Equals(previousStateText, newStateText, StringComparison.OrdinalIgnoreCase))
                {
                    // The state text has changed from the previous state
                    _logger.LogStateChanged(resource.Name, resourceId, previousStateText, newStateText);
                }
                else if (string.IsNullOrWhiteSpace(previousStateText))
                {
                    // There was no previous state text so just log the new state
                    _logger.LogNewResourceState(resource.Name, resourceId, newStateText);
                }
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogResource(
                    newState.Version,
                    resource.Name,
                    resourceId,
                    newState.ResourceType,
                    newState.CreationTimeStamp,
                    newState.State?.Text,
                    newState.State?.Style,
                    newState.HealthStatus,
                    newState.ResourceReadyEvent is not null,
                    newState.ExitCode,
                    string.Join(", ", newState.Urls.Select(u => $"{u.Name} = {u.Url}")),
                    JoinIndentLines(newState.EnvironmentVariables.Where(e => e.IsFromSpec).Select(e => $"{e.Name} = {e.Value}")),
                    JoinIndentLines(newState.Properties.Select(p => $"{p.Name} = {Stringify(p.Value)}")),
                    JoinIndentLines(newState.HealthReports.Select(p => $"{p.Name} = {Stringify(p.Status)}")),
                    JoinIndentLines(newState.Commands.Select(c => $"{c.DisplayName} ({c.Name}) = {Stringify(c.State)}")));

                static string Stringify(object? o) => o switch
                {
                    IEnumerable<int> ints => string.Join(", ", ints.Select(i => i.ToString(CultureInfo.InvariantCulture))),
                    IEnumerable<string> strings => string.Join(", ", strings.Select(s => s)),
                    null => "(null)",
                    _ => o.ToString()!
                };

                static string JoinIndentLines(IEnumerable<string> values)
                {
                    const int spaces = 2;
                    var indent = new string(' ', spaces);
                    var separator = Environment.NewLine + indent;

                    var result = string.Join(separator, values);
                    if (string.IsNullOrEmpty(result))
                    {
                        return result;
                    }

                    // Indent first line.
                    return indent + result;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static CustomResourceSnapshot GetCurrentSnapshot(IResource resource, ResourceNotificationState notificationState)
    {
        var previousState = notificationState.LastSnapshot;

        if (previousState is null)
        {
            // If there is no initial snapshot, create an empty one.
            previousState ??= new CustomResourceSnapshot()
            {
                ResourceType = resource.GetType().Name,
                Properties = [],
                Relationships = []
            };
        }

        return previousState;
    }

    private ResourceNotificationState GetResourceNotificationState(IResource resource, string resourceId) =>
        _resourceNotificationStates.GetOrAdd((resource, resourceId), _ => new ResourceNotificationState());

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposing.Cancel();
    }

    /// <summary>
    /// The annotation that allows publishing and subscribing to changes in the state of a resource.
    /// </summary>
    private sealed class ResourceNotificationState
    {
        private long _lastVersion = 1;
        public long GetNextVersion() => Interlocked.Increment(ref _lastVersion);
        public CustomResourceSnapshot? LastSnapshot { get; set; }
    }
}

/// <summary>
/// Represents a change in the state of a resource.
/// </summary>
/// <param name="resource">The resource associated with the event.</param>
/// <param name="resourceId">The unique id of the resource.</param>
/// <param name="snapshot">The snapshot of the resource state.</param>
public class ResourceEvent(IResource resource, string resourceId, CustomResourceSnapshot snapshot)
{
    /// <summary>
    /// The resource associated with the event.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The unique id of the resource.
    /// </summary>
    public string ResourceId { get; } = resourceId;

    /// <summary>
    /// The snapshot of the resource state.
    /// </summary>
    public CustomResourceSnapshot Snapshot { get; } = snapshot;
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Resource {Resource}/{ResourceId} changed state: {PreviousState} -> {NewState}")]
    public static partial void LogStateChanged(this ILogger<ResourceNotificationService> logger, string resource, string resourceId, string previousState, string newState);

    [LoggerMessage(LogLevel.Debug, "Resource {Resource}/{ResourceId} changed state: {NewState}")]
    public static partial void LogNewResourceState(this ILogger<ResourceNotificationService> logger, string resource, string resourceId, string newState);

    [LoggerMessage(LogLevel.Trace, """
                    Version: {Version}
                    Resource {Resource}/{ResourceId} update published:
                    ResourceType = {ResourceType},
                    CreationTimeStamp = {CreationTimeStamp:s},
                    State = {{ Text = {StateText}, Style = {StateStyle} }},
                    HeathStatus = {HealthStatus},
                    ResourceReady = {ResourceReady},
                    ExitCode = {ExitCode},
                    Urls = {{ {Urls} }},
                    EnvironmentVariables = {{
                    {EnvironmentVariables}
                    }},
                    Properties = {{
                    {Properties}
                    }},
                    HealthReports = {{
                    {HealthReports}
                    }},
                    Commands = {{
                    {Commands}
                    }}
                    """)]
    public static partial void LogResource(this ILogger<ResourceNotificationService> logger,
        long version,
        string resource,
        string resourceId,
        string resourceType,
        DateTime? creationTimeStamp,
        string? stateText,
        string? stateStyle,
        HealthStatus? healthStatus,
        bool resourceReady,
        int? exitCode,
        string Urls,
        string environmentVariables,
        string properties,
        string healthReports,
        string commands);
}
