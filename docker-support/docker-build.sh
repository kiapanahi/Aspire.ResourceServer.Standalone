#!/bin/sh

# Exit immediately if a command exits with a non-zero status.
set -e
# Ensure that the exit status of a pipeline is the status of the last command to exit with a non-zero status.
set -o pipefail

current_date=$(date +%Y%m%d)

docker pull mcr.microsoft.com/dotnet/sdk:9.0
docker pull mcr.microsoft.com/dotnet/aspnet:9.0

docker buildx build --platform linux/arm64,linux/amd64 -f Dockerfile \
  -t aspire-resource-server:latest \
  -t aspire-resource-server:$current_date \
  -t timdinh/aspire-resource-server:latest \
  -t timdinh/aspire-resource-server:$current_date \
  ..

docker push timdinh/aspire-resource-server:latest
docker push timdinh/aspire-resource-server:$current_date