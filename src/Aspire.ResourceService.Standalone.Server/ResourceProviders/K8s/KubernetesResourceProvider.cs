using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Google.Protobuf.WellKnownTypes;
using k8s;
using Microsoft.Extensions.Options;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;

internal sealed class KubernetesResourceProvider : IResourceProvider
{
    private readonly IKubernetes _kubernetes;
    private readonly IOptions<KubernetesResourceProviderConfiguration> _configuration;
    private readonly ILogger<KubernetesResourceProvider> _logger;

    public KubernetesResourceProvider(IKubernetes kubernetes,
        IOptions<KubernetesResourceProviderConfiguration> configuration,
        ILogger<KubernetesResourceProvider> logger)
    {
        _kubernetes = kubernetes;
        _configuration = configuration;
        _logger = logger;
    }

    public async ValueTask GetResources(CancellationToken cancellationToken)
    {
        var containers = await GetKubernetesContainers().ConfigureAwait(false);
        var resources = containers.Select(Resource.FromK8sContainer).ToList().AsReadOnly();
    }

    public async ValueTask<List<KubernetesContainer>> GetKubernetesContainers()
    {
        string[] srvNames = _configuration.Value.Servicenames?.Split(';') ?? throw new InvalidOperationException();

        var containers = new List<KubernetesContainer>();

        if (srvNames.Length == 0)
        {
            throw new InvalidOperationException("No service names provided!");
        }

        var pods = await _kubernetes.CoreV1.ListNamespacedPodAsync(namespaceParameter: _configuration.Value.Namespace)
            .ConfigureAwait(false);

        if (pods.Items.Count == 0)
        {
            return containers;
        }

        foreach (var serviceName in srvNames)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                continue;
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

    public void Dispose()
    {
        _kubernetes?.Dispose();
    }
}
