#!/bin/bash
# This script runs the aspire-dashboard Docker container.

docker run -d \
  -p 18888:18888 \
  -p 4317:18889 \
  -e 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true' \
  -e 'DOTNET_RESOURCE_SERVICE_ENDPOINT_URL=http://host.docker.internal:7007' \
  -e 'Dashboard__ResourceServiceClient__AuthMode=Unsecured' \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:9.0
