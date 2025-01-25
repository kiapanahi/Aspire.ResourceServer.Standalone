FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

ENV DOTNET_CLI_TELEMETRY_OPTOUT=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR "/src"
COPY . .

RUN dotnet restore "/src/src/Aspire.ResourceService.Standalone.Server/Aspire.ResourceService.Standalone.Server.csproj"

RUN dotnet build   "/src/src/Aspire.ResourceService.Standalone.Server/Aspire.ResourceService.Standalone.Server.csproj" -f net9.0 -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "/src/src/Aspire.ResourceService.Standalone.Server/Aspire.ResourceService.Standalone.Server.csproj" -f net9.0 -c $BUILD_CONFIGURATION -o /app/publish 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aspire.ResourceService.Standalone.Server.dll"]
