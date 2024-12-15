using Aspire.ResourceService.Standalone.Server.ResourceProviders;

using Docker.DotNet;
using Docker.DotNet.Models;

using FluentAssertions;

using Google.Protobuf.WellKnownTypes;

using Moq;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;
public class DockerResourceProviderTests : IDisposable
{
    private readonly Mock<IDockerClient> _dockerClientMock;
    private readonly DockerResourceProvider _dockerResourceProvider;

    public DockerResourceProviderTests()
    {
        _dockerClientMock = new Mock<IDockerClient>();
        _dockerResourceProvider = new DockerResourceProvider(_dockerClientMock.Object);
    }

    [Fact]
    public async Task GetResourcesAsyncShouldReturnResources()
    {
        // Arrange
        var containers = new List<ContainerListResponse>
        {
            new() {
                ID = "1",
                Names = ["container1"],
                State = "running",
                Created = DateTime.UtcNow,
                Ports = [new() { IP = "127.0.0.1", PublicPort = 80 }]
            }
        };

        _dockerClientMock.Setup(c => c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containers);

        // Act
        var resources = await _dockerResourceProvider.GetResourcesAsync();

        // Assert
        resources.Should().HaveCount(1);
        var resource = resources.First();
        resource.Uid.Should().Be("1");
        resource.Name.Should().Be("container1");
        resource.State.Should().Be("running");
        resource.CreatedAt.Should().Be(Timestamp.FromDateTime(containers[0].Created));
        resource.Urls.Should().ContainSingle(url => url.FullUrl == "http://127.0.0.1:80");
    }

    [Fact]
    public async Task GettingContainersHitsTheCacheAfterFirstTime()
    {
        // Arrange
        var containers = new List<ContainerListResponse>
        {
            new() {
                ID = "1",
                Names = ["container1"],
                State = "running",
                Created = DateTime.UtcNow,
                Ports = [new() { IP = "127.0.0.1", PublicPort = 80 }]
            }
        };

        _dockerClientMock.Setup(c => c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containers);

        // Act
        for (var i = 0; i < 10; i++)
        {
            _ = await _dockerResourceProvider.GetResourcesAsync();
        }

        // Assert
        _dockerClientMock.Verify(c => c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GerResourceLogsShouldReturnLogs()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(400));

        var containers = new List<ContainerListResponse>
        {
            new() {
                ID = "1",
                Names = ["container1"]
            }
        };

        _dockerClientMock.Setup(c => c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), CancellationToken.None))
            .ReturnsAsync(containers);

        var logs = new List<string> { "log1", "log2" };

        _dockerClientMock
            .Setup(c => c.Containers.GetContainerLogsAsync(
                It.IsAny<string>(),
                It.IsAny<ContainerLogsParameters>(),
                cts.Token,
                It.IsAny<IProgress<string>>()))
            .Callback<string, ContainerLogsParameters, CancellationToken, IProgress<string>>((id, parameters, token, progress) =>
            {
                foreach (var log in logs)
                {
                    progress.Report(log);
                }
            })
            .Returns(Task.CompletedTask);

        // Act
        var resultLogs = new List<string>();
        try
        {
            await foreach (var log in _dockerResourceProvider.GerResourceLogs("container1", cts.Token).ConfigureAwait(false))
            {
                resultLogs.Add(log);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            // cts.Task is cancelled mimicking the end of the log stream.
            // Swallow
        }

        // Assert
        resultLogs.Should().BeEquivalentTo(logs);
    }

    public void Dispose()
    {
        _dockerResourceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
