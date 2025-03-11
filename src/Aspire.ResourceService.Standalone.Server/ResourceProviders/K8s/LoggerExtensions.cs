namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;

internal static partial class KubernetesResourceProviderLogs
{
    [LoggerMessage(LogLevel.Debug, "Captured change: {Change}")]
    public static partial void CapturedKubernetesChange(this ILogger<KubernetesResourceProvider> logger, string change);
}
