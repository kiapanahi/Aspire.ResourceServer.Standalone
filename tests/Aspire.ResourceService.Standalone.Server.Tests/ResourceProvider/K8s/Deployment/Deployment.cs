using k8s.Models;

namespace Aspire.ResourceService.Standalone.Server.Tests.ResourceProvider.K8s.Deployment;

public static class DeploymentObjects
{
    public static V1Namespace GetTestNamespace(string nameSpace) => new V1Namespace
    {
        Metadata = new()
        {
            Name = nameSpace
        }
    };

    public static V1Service GetTestService(string serviceName, int port, string nameSpace) => new V1Service
    {
        Metadata = new V1ObjectMeta
        {
            Name = $"{serviceName}-service",
            NamespaceProperty = nameSpace
        },
        Spec = new V1ServiceSpec
        {
            Selector = new Dictionary<string, string> { { "app", serviceName } },
            Ports = new List<V1ServicePort>
            {
                new V1ServicePort
                {
                    Port = port,
                    TargetPort = port
                }
            }
        }
    };

    public static V1Deployment GetTestDeployment(string serviceName, int port, string nameSpace) => new V1Deployment
    {
        Metadata = new V1ObjectMeta
        {
            Name = $"{serviceName}-deployment",
            NamespaceProperty = nameSpace,
            Labels = new Dictionary<string, string> { { "app", serviceName } }
        },
        Spec = new V1DeploymentSpec
        {
            Replicas = 1,
            Selector = new V1LabelSelector
            {
                MatchLabels = new Dictionary<string, string> { { "app", serviceName } }
            },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta
                {
                    Labels = new Dictionary<string, string> { { "app", serviceName } }
                },
                Spec = new V1PodSpec
                {
                    Containers = new List<V1Container>
                    {
                        new V1Container
                        {
                            Name = serviceName,
                            Image = $"docker.io/library/{serviceName}",
                            Ports = new List<V1ContainerPort>
                            {
                                new V1ContainerPort { ContainerPort = port }
                            }
                        }
                    }
                }
            }
        }
    };
}
