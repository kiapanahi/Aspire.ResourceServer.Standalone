using Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider;

public sealed class DockerResourceProviderTests : IDisposable
{
    private readonly Mock<IDockerClient> _dockerClientMock;
    private readonly DockerResourceProvider _dockerResourceProvider;

    public DockerResourceProviderTests()
    {
        _dockerClientMock = new Mock<IDockerClient>();
        _dockerResourceProvider = new DockerResourceProvider(_dockerClientMock.Object, NullLogger<DockerResourceProvider>.Instance);
    }

    [Fact]
    public async Task GetResourcesAsyncShouldReturnResources()
    {
        // Arrange
        var containers = new List<ContainerListResponse>
        {
            new()
            {
                ID = "1",
                Names = ["container1"],
                State = "running",
                Created = DateTime.UtcNow,
                Ports = [new() { IP = "127.0.0.1", PublicPort = 80 }]
            }
        };

        _dockerClientMock.Setup(c =>
                c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containers);

        // Act
        var (initialResources, updateStream) =
            await _dockerResourceProvider.GetResources(CancellationToken.None).ConfigureAwait(true);

        // Assert
        initialResources.Should().ContainSingle();
        var resource = initialResources[0];
        resource.Uid.Should().Be("1");
        resource.Name.Should().Be("container1");
        resource.State.Should().Be("running");
        resource.CreatedAt.Should().Be(Timestamp.FromDateTime(containers[0].Created));
        resource.Urls.Should().ContainSingle(url => url.FullUrl == "http://127.0.0.1:80");
    }

    [Fact]
    public async Task GetContainersFromDocker()
    {
        // Arrange
        var containers = new List<ContainerListResponse>
        {
            new()
            {
                ID = "1",
                Names = ["container1"],
                State = "running",
                Created = DateTime.UtcNow,
                Ports = [new() { IP = "127.0.0.1", PublicPort = 80 }]
            }
        };

        _dockerClientMock.Setup(c =>
                c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(containers);

        // Act
        for (var i = 0; i < 10; i++)
        {
            _ = await _dockerResourceProvider.GetContainers().ConfigureAwait(true);
        }

        // Assert
        _dockerClientMock.Verify(
            c => c.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()),
            Times.Exactly(10));
    }

    public void Dispose()
    {
        _dockerResourceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
