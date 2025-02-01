using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

public class KubernetesContainer
{
    public required string Name { get; set; }
    public required string ContainerID { get; set; }
    public required int Port { get; set; }
    public required string Image { get; set; }
    public required bool Ready { get; set; }
    public required int RestartCount { get; set; }
    public required string State { get; set; }
    public required Timestamp StartedAt { get; set; }

}
