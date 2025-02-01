using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Google.Protobuf.WellKnownTypes;
using k8s;
using Microsoft.Extensions.Options;
using k8s.Models;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;

internal sealed partial class KubernetesResourceProvider(Kubernetes kubernetes, IOptions<KubernetesResourceProviderConfiguration> configuration, ILogger<KubernetesResourceProvider> logger) : IResourceProvider, IDisposable
{
    public async Task<ResourceSubscription> GetResources(CancellationToken cancellationToken)
    {
        var containers = await GetKubernetesContainers().ConfigureAwait(false);
        var resources = containers.Select(Resource.FromK8sContainer).ToList().AsReadOnly();

        return new ResourceSubscription(resources, UpdateStream(cancellationToken));

        async IAsyncEnumerable<WatchResourcesChange?> UpdateStream(
            [EnumeratorCancellation] CancellationToken cancellation)
        {
            var channel = Channel.CreateUnbounded<K8sMessage>();

            async Task WatchEvents(CancellationToken cancellationToken)
            {
                var watch = await kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
                    configuration.Value.Namespace,
                    cancellationToken: cancellationToken,
                    watch: true)
                    .ConfigureAwait(false);

                watch.Watch<V1Pod, V1PodList>((type, item) =>
                {
                    if (item.Status is not null)
                    {
                        if (item.Status.ContainerStatuses is not null)
                        {
                            foreach (var container in item.Status.ContainerStatuses)
                            {
                                var containerState = "";

                                var startedAt = DateTime.Now;

                                if (container.State.Running is not null)
                                {
                                    containerState = "Running";
                                    if (container.State.Running.StartedAt is not null)
                                    {
                                        startedAt = (DateTime)container.State.Running.StartedAt;
                                    }
                                }

                                if (container.State.Terminated is not null)
                                {
                                    containerState = "Terminated";
                                }

                                if (container.State.Waiting is not null)
                                {
                                    containerState = "Waiting";
                                }
                                //channel.Writer.TryWrite($"[{type}] - Pod: {item.Metadata.Name} - Pod Phase: {item.Status.Phase} - Container: {container.Name} - ContainerState: {containerState}");

                                var message = new K8sMessage
                                {
                                    ContainerState = containerState,
                                    ContainerId = container.ContainerID,
                                    PodName = item.Metadata.Name,
                                    Type = type.ToString()
                                };

                                channel.Writer.TryWrite(message);
                            }
                        }
                    }
                    
                });
            }

            _ = Task.Run(() => WatchEvents(cancellationToken), cancellationToken).ConfigureAwait(false);
            
            await foreach (var msg in channel.Reader.ReadAllAsync(cancellation).ConfigureAwait(false))
            {
                logger.CapturedKubernetesChange(System.Text.Json.JsonSerializer.Serialize(msg));

                //if (!string.Equals(msg.Type, KnownResourceTypes.Container, StringComparison.OrdinalIgnoreCase))
                //{
                //    logger.SkippingChange(msg.Type);
                //    continue;
                //}

                yield return await GetChangeForStartedContainer(msg.ContainerId).ConfigureAwait(false);
            }
        }
    }

    public async IAsyncEnumerable<ResourceLogEntry> GerResourceLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var pods = await kubernetes.CoreV1.ListNamespacedPodAsync(
                    namespaceParameter: configuration.Value.Namespace,
                    cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);

        string podName = pods.Items.Where(p => p.Metadata.Labels["app"] == resourceName &&
                p.Status.Phase == "Running" &&
                p.Status.Conditions.Any(c => c.Type == "Ready" && c.Status == "True")).FirstOrDefault()?.Metadata.Name ?? "";

        if (string.IsNullOrWhiteSpace(podName))
        {
            throw new InvalidOperationException("Could not get name of pod");
        }

        var logStream = await kubernetes.CoreV1.ReadNamespacedPodLogAsync(
                    name: podName,
                    namespaceParameter: configuration.Value.Namespace,
                    container: resourceName,
                    cancellationToken: cancellationToken,
                    follow: true
                ).ConfigureAwait(false);

        using (var reader = new StreamReader(logStream))
        {
            while (!reader.EndOfStream)
            {
                
                string? logline = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (logline is not null)
                {
                    yield return new ResourceLogEntry(resourceName, logline);
                }
            }
        }
    }

    private async Task<WatchResourcesChange?> GetChangeForStartedContainer(string containerId)
    {
        try
        {
            var containers = await GetKubernetesContainers().ConfigureAwait(false);
            var container =
                containers.Single(c => string.Equals(c.ContainerID, containerId, StringComparison.OrdinalIgnoreCase));
            var resource = Resource.FromK8sContainer(container);

            return new() { Upsert = resource };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async ValueTask<List<KubernetesContainer>> GetKubernetesContainers()
    {
        var containers = new List<KubernetesContainer>();
        if (configuration.Value.ServiceNames.Length == 0)
        {
            throw new InvalidOperationException("No service names provided!");
        }

        var pods = await kubernetes.CoreV1.ListNamespacedPodAsync(
                namespaceParameter: configuration.Value.Namespace
                )
                .ConfigureAwait(false);

        foreach (var serviceName in configuration.Value.ServiceNames)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                continue;
            }

            if (pods.Items.Count == 0)
            {

            }

            var pod = pods.Items.Where(p => p.Metadata.Labels["app"] == serviceName &&
            p.Status.Phase == "Running" &&
            p.Status.Conditions.Any(c => c.Type == "Ready" && c.Status == "True")).FirstOrDefault();

            if (pod is not null)
            {
                var containerStatusDetails = pod.Status.ContainerStatuses.FirstOrDefault(c => c.Name == serviceName);
                var containerSpecDetails = pod.Spec.Containers.FirstOrDefault(c => c.Name == serviceName);

                if (containerStatusDetails is null || containerSpecDetails is null)
                {
                    throw new InvalidOperationException("Could not get container details!");
                }

                var port = 0;
                var containerPorts = containerSpecDetails.Ports.FirstOrDefault();

                if (containerPorts is null)
                {
                    port = 0000;
                }
                else
                {
                    port = containerPorts.ContainerPort;
                }

                var containerState = "";

                var startedAt = DateTime.Now;

                if (containerStatusDetails.State.Running is not null)
                {
                    containerState = "Running";
                    if (containerStatusDetails.State.Running.StartedAt is not null)
                    {
                        startedAt = (DateTime)containerStatusDetails.State.Running.StartedAt;
                    }
                }

                if (containerStatusDetails.State.Terminated is not null)
                {
                    containerState = "Terminated";
                }

                if (containerStatusDetails.State.Waiting is not null)
                {
                    containerState = "Waiting";
                }

                var container = new KubernetesContainer()
                {
                    Name = containerStatusDetails.Name,
                    ContainerID = containerStatusDetails.ContainerID,
                    Port = port,
                    Image = containerStatusDetails.Image,
                    Ready = containerStatusDetails.Ready,
                    RestartCount = containerStatusDetails.RestartCount,
                    State = containerState,
                    StartedAt = Timestamp.FromDateTime(startedAt)
                };

                containers.Add(container);
            }
        }
        return containers;
    }
}

internal static partial class KubernetesResourceProviderLogs
{
    [LoggerMessage(LogLevel.Debug, "Monitoring Kubernetes events started")]
    public static partial void MonitoringKubernetesEventsStarted(this ILogger<KubernetesResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Waiting for Kubernetes events")]
    public static partial void WaitingForKubernetesEvents(this ILogger<KubernetesResourceProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Captured change: {Change}")]
    public static partial void CapturedKubernetesChange(this ILogger<KubernetesResourceProvider> logger, string change);

    [LoggerMessage(LogLevel.Debug, "Skipping change of type: {Change}")]
    public static partial void SkippingChange(this ILogger<KubernetesResourceProvider> logger, string change);
    [LoggerMessage(LogLevel.Information, "LOGGING SOMETHING: {Value}")]
    public static partial void Test(this ILogger<KubernetesResourceProvider> logger, string value);
}

internal sealed partial class KubernetesResourceProvider : IDisposable
{
    public bool _disposedValue;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                kubernetes?.Dispose();
            }

            _disposedValue = true;
        }
    }
}
