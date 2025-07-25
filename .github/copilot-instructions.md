# Aspire Resource Server Copilot Instructions

## Project Overview
This is a standalone gRPC resource server that bridges external container environments (Docker/Kubernetes) with the Aspire Dashboard, enabling teams to monitor non-Aspire workloads through the familiar Aspire UI.

## Architecture & Key Components

### Core Provider Pattern
The system uses a pluggable provider architecture via `IResourceProvider` interface in `src/Aspire.ResourceService.Standalone.Server/ResourceProviders/`:
- **Docker Provider**: Monitors Docker containers via Docker API, streams real-time logs and container events
- **Kubernetes Provider**: Monitors K8s pods via Kubernetes API, filters by namespace and service names
- Provider selection is configuration-driven via `ResourceProvider` setting (`"docker"` or `"k8s"`)

### gRPC Service Implementation
- `DashboardService.cs` implements the Aspire Dashboard gRPC contract from `src/proto/resource_service.proto`
- Two main endpoints: `WatchResources` (streaming resource updates) and `GetResourceLogs` (streaming logs)
- Uses async streaming with cancellation token support throughout

## Development Workflows

### Testing Different Environments
Use the convenience scripts rather than manual docker commands:
- **Local development**: `start-compose.ps1` - runs external containers, expects you to run the server locally
- **Containerized**: `start-dockerfile.ps1` - builds server image and runs everything in containers
- **Kubernetes**: `start-minikube.ps1` - deploys to minikube, requires port-forwarding for dashboard access

## Code Patterns & Conventions

Use latest version of C# and .NET SDK (e.g., .NET 8) for all code.

### Async Streaming Implementation
- All resource and log streaming uses `IAsyncEnumerable<T>` with `[EnumeratorCancellation]`
- Channel-based communication for decoupling event sources from consumers
- Proper cancellation token propagation through the entire call chain

### Error Handling in Providers
- Providers gracefully handle container/pod lifecycle events (creation, deletion, restarts)
- Failed resource lookups return `null` in change streams rather than throwing
- Log streaming handles I/O exceptions and cancellation gracefully

### Testing Infrastructure
- Docker provider tests use `Mock<IDockerClient>` for unit testing
- K8s provider tests use Testcontainers with K3s for integration testing
- Test configuration builder in `tests/TestConfigurationBuilder/` provides consistent config setup

## Critical Integration Points

### Aspire Dashboard Communication
- Dashboard expects gRPC on HTTP/2 (`Kestrel.EndpointDefaults.Protocols: Http2`)
- Environment variables: `DASHBOARD__RESOURCESERVICECLIENT__URL` points to this server
- Authentication disabled in dev: `DASHBOARD__FRONTEND__AUTHMODE: "Unsecured"`

When implementing new providers or extending existing ones, follow the established patterns for async streaming, proper resource lifecycle management, and graceful error handling. The test suites provide excellent examples of mocking strategies and integration testing approaches.