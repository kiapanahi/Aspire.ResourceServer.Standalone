using Aspire.ResourceService.Proto.V1;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal static partial class WatchResourcesLogs
{
    [LoggerMessage(LogLevel.Trace, "Returning application information")]
    public static partial void ReturningApplicationInformation(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Preparing to compile initial resources")]
    public static partial void LogCompilingInitialResources(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Initial resources compiled")]
    public static partial void LogInitialResourcesCompiled(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Preparing to send initial resources")]
    public static partial void WritingInitialResourcesToStream(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Initial resources sent")]
    public static partial void InitialResourcesWroteToStreamSuccessfully(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error executing service method {Method}")]

    public static partial void LogErrorWatchingResources(this ILogger logger, string method, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Got resource update: {Update}")]
    public static partial void LogGotResourceUpdate(this ILogger logger, WatchResourcesChange update);

    [LoggerMessage(LogLevel.Trace, "Started watching console logs for resource: {Resource}")]
    public static partial void StartedWatchingResourceConsoleLogs(this ILogger<DashboardService> logger, string resource);

    [LoggerMessage(LogLevel.Trace, "Awaiting log stream for resource: {Resource}")]
    public static partial void AwaitingLogStream(this ILogger<DashboardService> logger, string resource);

    [LoggerMessage(LogLevel.Trace, "Got log entry from stream")]
    public static partial void GotLogEntry(this ILogger<DashboardService> logger);

    [LoggerMessage(LogLevel.Trace, "Writing log item to stream: {Update}")]
    public static partial void WritingLogToOutputStream(this ILogger<DashboardService> logger, WatchResourceConsoleLogsUpdate update);
}
