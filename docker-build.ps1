$ErrorActionPreference = "Stop"

$currentDate = Get-Date -Format "yyyyMMdd"

docker pull mcr.microsoft.com/dotnet/sdk:9.0
docker pull mcr.microsoft.com/dotnet/aspnet:9.0

docker buildx build --platform linux/amd64 -f Dockerfile `
  --build-arg BUILD_CONFIGURATION=Release `
  -t aspire-resource-server:latest `
  -t aspire-resource-server:$currentDate `
  .