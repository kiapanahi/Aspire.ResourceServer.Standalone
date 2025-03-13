docker rm -v -f aspire-dashboard;

docker run -d -p 18888:18888 -p 4317:18889 `
--name aspire-dashboard `
-e 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true' `
-e 'DOTNET_RESOURCE_SERVICE_ENDPOINT_URL=http://host.docker.internal:7007' `
-e 'Dashboard__ResourceServiceClient__AuthMode=Unsecured' `
mcr.microsoft.com/dotnet/aspire-dashboard:9.0