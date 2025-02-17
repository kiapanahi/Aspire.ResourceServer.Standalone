using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider.K8s;
using FluentAssertions;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using static Aspire.ResourceService.Standalone.Server.Tests.TestConfigurationBuilder.TestConfigurationBuilder;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public class KubernetesResourceProviderTests : IClassFixture<KubernetesFixture>,IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly KubernetesFixture _kubernetesFixture;
    private readonly IKubernetes _kubernetes;
    private readonly KubernetesResourceProvider _kubernetesResourceProvider;

    public KubernetesResourceProviderTests(KubernetesFixture kubernetesFixture)
    {
        _configuration = GetTestConfiguration("k8s");
        _kubernetesFixture = kubernetesFixture;

        if (_kubernetesFixture.Kubernetes is null) { throw new InvalidOperationException(); }

        _kubernetes = _kubernetesFixture.Kubernetes;

        var resourceProviderConfiguration = _configuration.GetSection("KubernetesResourceProviderConfiguration").Get<KubernetesResourceProviderConfiguration>();
        if (resourceProviderConfiguration is null) { throw new InvalidOperationException($"Invalid {nameof(KubernetesResourceProviderConfiguration)}"); }

        _kubernetesResourceProvider = new KubernetesResourceProvider(_kubernetes,
            Options.Create(resourceProviderConfiguration),
            NullLogger<KubernetesResourceProvider>.Instance);
    }

    [Fact]
    public async Task GetContainersFromKubernetes()
    {
        // Act
        var containers = await _kubernetesResourceProvider.GetKubernetesContainers();

        //Assert
        Assert.Equal(_kubernetesFixture.ContainerCount, containers.Count);
    }

    [Fact]
    public async Task GetResourcesAsyncShouldReturnResources()
    {
        // Act
        var (initialResources, _) =
            await _kubernetesResourceProvider.GetResources(CancellationToken.None).ConfigureAwait(true);

        // Assert
        initialResources.Count.Should().Be(_kubernetesFixture.ContainerCount);

        foreach (var resource in initialResources)
        {
            resource.State.Should().Be("Running");
        }
    }

    [Fact]
    public async Task GetResourceLogsShouldReturnLogs()
    {
        // Arrange
        var resultLogs = new List<ResourceLogEntry>();
        int cap = 50;
        var serviceNames = _kubernetesFixture?.Servicenames?.ToList() ?? new List<string>();

        async Task GetResourceLogs(string resource)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                int count = 0;
                await foreach (var log in _kubernetesResourceProvider.GetResourceLogs(resource, cts.Token)
                                   .ConfigureAwait(false))
                {
                    if (count < cap)
                    {
                        resultLogs.Add(log);
                    }
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException or IOException)
            {
                // cts.Task is cancelled mimicking the end of the log stream.
                // Swallow
            }
        }
        // Act
        var tasks = new List<Task>();

        foreach (string serviceName in serviceNames)
        {
            tasks.Add(GetResourceLogs(serviceName));
        }

        await Task.WhenAll(tasks);

        // Assert
        resultLogs.Count.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _kubernetesResourceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
