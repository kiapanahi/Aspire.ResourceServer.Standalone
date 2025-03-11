using Aspire.Dashboard.Model;
using Aspire.Hosting.Dashboard;
using Aspire.ResourceService.Standalone.Server.ResourceProviders.K8s.Models;
using Docker.DotNet.Models;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable CheckNamespace

namespace Aspire.ResourceService.Proto.V1;

public sealed partial class Resource
{
    internal static Resource FromDockerContainer(ContainerListResponse container)
    {
        var containerName = container.Names.First().Replace("/", "");
        var resource = new Resource
        {
            CreatedAt = Timestamp.FromDateTime(container.Created),
            State = container.State,
            DisplayName = containerName,
            ResourceType = KnownResourceTypes.Container,
            Name = containerName,
            Uid = container.ID
        };
        resource.Urls.Add(container.Ports.Where(p => !string.IsNullOrEmpty(p.IP))
            .Select(s => new Url
            {
                IsInternal = false,
                EndpointName = $"http://{s.IP}:{s.PublicPort}",
                FullUrl = $"http://{s.IP}:{s.PublicPort}",
                DisplayProperties = new()
                {
                    SortOrder = 0,
                    DisplayName = ""
                }
            }));
        return resource;
    }
    internal static Resource FromK8sContainer(KubernetesContainer container)
    {
        var resource = new Resource
        {
            CreatedAt = container.StartedAt,
            State = container.State,
            DisplayName = container.Name,
            ResourceType = KnownResourceTypes.Container,
            Name = container.Name,
            Uid = container.ContainerID
        };
        resource.Urls.Add(new Url()
        {
            IsInternal = false,
            EndpointName = $"http://{container.Name}:{container.Port}",
            FullUrl = $"http://{container.Name}:{container.Port}",
            DisplayProperties = new()
            {
                SortOrder = 0,
                DisplayName = ""
            }
        });
        return resource;
    }
    internal static Resource FromSnapshot(ResourceSnapshot snapshot)
    {
        var resource = new Resource
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            DisplayName = snapshot.DisplayName,
            Uid = snapshot.Uid,
            State = snapshot.State ?? "",
            StateStyle = snapshot.StateStyle ?? "",
        };

        if (snapshot.CreationTimeStamp.HasValue)
        {
            resource.CreatedAt = Timestamp.FromDateTime(snapshot.CreationTimeStamp.Value.ToUniversalTime());
        }

        if (snapshot.StartTimeStamp.HasValue)
        {
            resource.StartedAt = Timestamp.FromDateTime(snapshot.StartTimeStamp.Value.ToUniversalTime());
        }

        if (snapshot.StopTimeStamp.HasValue)
        {
            resource.StoppedAt = Timestamp.FromDateTime(snapshot.StopTimeStamp.Value.ToUniversalTime());
        }

        foreach (var env in snapshot.Environment)
        {
            resource.Environment.Add(new EnvironmentVariable { Name = env.Name, Value = env.Value ?? "", IsFromSpec = env.IsFromSpec });
        }

        foreach (var url in snapshot.Urls)
        {
            resource.Urls.Add(new Url { Name = url.Name, FullUrl = url.Url, IsInternal = url.IsInternal, IsInactive = url.IsInactive });
        }

        foreach (var relationship in snapshot.Relationships)
        {
            resource.Relationships.Add(new ResourceRelationship
            {
                ResourceName = relationship.ResourceName,
                Type = relationship.Type
            });
        }

        foreach (var property in snapshot.Properties)
        {
            resource.Properties.Add(new ResourceProperty { Name = property.Name, Value = property.Value, IsSensitive = property.IsSensitive });
        }

        foreach (var volume in snapshot.Volumes)
        {
            resource.Volumes.Add(new Volume
            {
                Source = volume.Source ?? string.Empty,
                Target = volume.Target,
                MountType = volume.MountType,
                IsReadOnly = volume.IsReadOnly
            });
        }

        foreach (var command in snapshot.Commands)
        {
            resource.Commands.Add(new ResourceCommand { Name = command.Name, DisplayName = command.DisplayName, DisplayDescription = command.DisplayDescription ?? string.Empty, Parameter = ResourceSnapshot.ConvertToValue(command.Parameter), ConfirmationMessage = command.ConfirmationMessage ?? string.Empty, IconName = command.IconName ?? string.Empty, IconVariant = MapIconVariant(command.IconVariant), IsHighlighted = command.IsHighlighted, State = MapCommandState(command.State) });
        }

        foreach (var report in snapshot.HealthReports)
        {
            var healthReport = new HealthReport { Key = report.Name, Description = report.Description ?? "", Exception = report.ExceptionText ?? "" };

            if (report.Status is not null)
            {
                healthReport.Status = MapHealthStatus(report.Status.Value);
            }

            resource.HealthReports.Add(healthReport);
        }

        return resource;

        static HealthStatus MapHealthStatus(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus healthStatus)
        {
            return healthStatus switch
            {
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => HealthStatus.Healthy,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => HealthStatus.Degraded,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => HealthStatus.Unhealthy,
                _ => throw new InvalidOperationException("Unknown health status: " + healthStatus),
            };
        }
    }

    private static IconVariant MapIconVariant(Hosting.ApplicationModel.IconVariant? iconVariant)
    {
        return iconVariant switch
        {
            Hosting.ApplicationModel.IconVariant.Regular => IconVariant.Regular,
            Hosting.ApplicationModel.IconVariant.Filled => IconVariant.Filled,
            null => IconVariant.Regular,
            _ => throw new InvalidOperationException("Unexpected icon variant: " + iconVariant)
        };
    }

    private static ResourceCommandState MapCommandState(Hosting.ApplicationModel.ResourceCommandState state)
    {
        return state switch
        {
            Hosting.ApplicationModel.ResourceCommandState.Enabled => ResourceCommandState.Enabled,
            Hosting.ApplicationModel.ResourceCommandState.Disabled => ResourceCommandState.Disabled,
            Hosting.ApplicationModel.ResourceCommandState.Hidden => ResourceCommandState.Hidden,
            _ => throw new InvalidOperationException("Unexpected state: " + state)
        };
    }
}
