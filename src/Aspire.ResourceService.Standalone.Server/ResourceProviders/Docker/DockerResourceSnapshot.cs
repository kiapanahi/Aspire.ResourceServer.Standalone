using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Docker.DotNet.Models;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Standalone.Server.ResourceProviders.Docker;

internal sealed class DockerResourceSnapshot : ResourceSnapshot
{
    public override string ResourceType { get; } = KnownResourceTypes.Container;

    protected override IEnumerable<(string Key, Value Value, bool IsSensitive)> GetProperties()
    {
        return [];
    }

    internal static DockerResourceSnapshot FromContainer(ContainerListResponse container)
    {

        return new DockerResourceSnapshot
        {
            Commands = [],
            CreationTimeStamp = container.Created,
            DisplayName = container.Names.First().Replace("/", ""),
            Environment = [],
            ExitCode = 0,
            HealthReports = [],
            Name = container.Names.First().Replace("/", ""),
            Relationships = [],
            StartTimeStamp = container.Created,
            State = container.State switch
            {
                "running" => KnownResourceStates.Running,
                "exited" => KnownResourceStates.Exited,
                _ => KnownResourceStates.Hidden
            },
            StateStyle = container.State switch
            {
                "running" => KnownResourceStateStyles.Success,
                "exited" => KnownResourceStateStyles.Warn,
                _ => KnownResourceStateStyles.Error
            },
            StopTimeStamp = null,
            Uid = container.ID,
            Urls = [],
            Volumes = []
        };
    }
}
