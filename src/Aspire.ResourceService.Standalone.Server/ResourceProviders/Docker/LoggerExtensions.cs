namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;

internal static partial class DockerResourceProviderLogs
{
    [LoggerMessage(LogLevel.Debug, "Monitoring Docker events started")]
    public static partial void MonitoringDockerEventsStarted(this ILogger<DockerResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Waiting for Docker events")]
    public static partial void WaitingForDockerEvents(this ILogger<DockerResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Captured change: {Change}")]
    public static partial void CapturedDockerChange(this ILogger<DockerResourceProvider> logger, string change);

    [LoggerMessage(LogLevel.Debug, "Skipping change of type: {Change}")]
    public static partial void SkippingChange(this ILogger<DockerResourceProvider> logger, string change);
}
