using System.Runtime.Serialization;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;

[DataContract]
public class K8sMessage
{
    [DataMember(Name = "containerState", EmitDefaultValue = false)]
    public required string ContainerState { get; set; }

    [DataMember(Name = "ContainerId", EmitDefaultValue = false)]
    public required string ContainerId { get; set; }

    [DataMember(Name = "PodName", EmitDefaultValue = false)]
    public required string PodName { get; set; }

    [DataMember(Name = "Type", EmitDefaultValue = false)]
    public required string Type { get; set; }
}
