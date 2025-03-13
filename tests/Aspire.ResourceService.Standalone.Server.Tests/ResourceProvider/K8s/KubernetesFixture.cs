using Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider.K8s.Deployment;
using k8s;
using Testcontainers.K3s;
using static Aspire.ResourceService.Standalone.Server.Tests.TestConfigurationBuilder.TestConfigurationBuilder;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider.K8s;

[CollectionDefinition("Kubernetes Test Collection")]
public class KubernetesTest : ICollectionFixture<KubernetesFixture>
{
}
public class KubernetesFixture : IAsyncLifetime
{
    private static readonly string s_k3sKubeConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "k3s-kubeconfig.yaml");

    public string? TestNamespace { get; private set; }
    public string[]? Servicenames { get; private set; }

    public IKubernetes? Kubernetes { get; private set; }

    public int ContainerCount { get; private set; }

    public KubernetesFixture()
    {
        TestNamespace = GetK8sNamespaceValue() ?? throw new InvalidOperationException();
        Servicenames = GetK8sServiceNamesValue() ?? throw new InvalidOperationException();
        ContainerCount = Servicenames.Length;
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(TestNamespace) || Servicenames is null || ContainerCount == 0)
        {
            throw new ArgumentNullException(nameof(KubernetesFixture));
        }

        var k3sContainer = await StartAndGetContainer().ConfigureAwait(false);

        Kubernetes = GetKubernetesClient(s_k3sKubeConfigPath, k3sContainer.GetKubeconfigAsync().Result);

        await DeployTestContainers(Kubernetes, TestNamespace, Servicenames).ConfigureAwait(false);

    }

    public async Task DeployTestContainers(IKubernetes kubernetes, string nameSpace, string[] serviceNames)
    {
        await kubernetes.CoreV1.CreateNamespaceAsync(DeploymentObjects.GetTestNamespace(nameSpace))
            .ConfigureAwait(false);

        var port = 3000;

        foreach (var serviceName in serviceNames)
        {
            Task createDeployment = kubernetes.AppsV1.CreateNamespacedDeploymentAsync(DeploymentObjects.GetTestDeployment(serviceName, port, nameSpace), nameSpace);

            Task createService = kubernetes.CoreV1.CreateNamespacedServiceAsync(DeploymentObjects.GetTestService(serviceName, port, nameSpace), nameSpace);

            await Task.WhenAll(createDeployment, createService).ConfigureAwait(false);

            port = port + 2;
        }

        int timesChecked = 0;
        while (true)
        {
            if (timesChecked == 16)
            {
                throw new TimeoutException("Waiting for kubernetes container readiness timed out at 5 minutes.");
            }
            if (timesChecked > 0)
            {
                await Task.Delay(5000 * timesChecked).ConfigureAwait(false);
            }

            var pods = await kubernetes.CoreV1.ListNamespacedPodAsync(nameSpace, labelSelector: "app")
                .ConfigureAwait(false);

            if (pods.Items.Count > 0)
            {
                var podsRunning = 0;

                foreach (var pod in pods)
                {
                    if (pod.Status.Phase.Equals("Running", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        podsRunning++;
                    }
                }

                if (podsRunning == pods.Items.Count)
                {
                    break;
                }
            }
            else
            {
                throw new InvalidOperationException("No pods found! Throwing");
            }

            timesChecked++;
        }
    }

    public IKubernetes GetKubernetesClient(string kubeConfigFilePath, string kubeConfigContent)
    {
        File.WriteAllText(kubeConfigFilePath, kubeConfigContent);
        return new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(new FileInfo(kubeConfigFilePath)));
    }

    public async Task<K3sContainer> StartAndGetContainer()
    {
        var container = new K3sBuilder()
            .WithImage("rancher/k3s")
            .WithPortBinding(6443, true)
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        return container;
    }

    public async Task DisposeAsync()
    {
        await Task.Run(() => { Kubernetes?.Dispose(); }).ConfigureAwait(false);
    }
}
