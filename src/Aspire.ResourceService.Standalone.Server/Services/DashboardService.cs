using Aspire.ResourceService.Proto.V1;
using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Grpc.Core;

namespace Aspire.ResourceService.Standalone.Server.Services;

internal sealed class DashboardService : Proto.V1.DashboardService.DashboardServiceBase
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<DashboardService> _logger;
    private readonly IResourceProvider _resourceProvider;
    private readonly IServiceInformationProvider _serviceInformationProvider;

    public DashboardService(IServiceInformationProvider serviceInformationProvider,
        IResourceProvider resourceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DashboardService> logger)
    {
        _serviceInformationProvider = serviceInformationProvider;
        _resourceProvider = resourceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public override Task<ApplicationInformationResponse> GetApplicationInformation(
        ApplicationInformationRequest request, ServerCallContext context)
    {
        _logger.ReturningApplicationInformation();

        return Task.FromResult(new ApplicationInformationResponse
        {
            ApplicationName = _serviceInformationProvider.GetServiceInformation().Name
        });
    }

    public override async Task WatchResources(WatchResourcesRequest request, IServerStreamWriter<WatchResourcesUpdate> responseStream, ServerCallContext context)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping,
            context.CancellationToken);

        try
        {
            _logger.GettingResourcesFromResourceProvider();
            var (initialData, updates) = await _resourceProvider
                .GetResources(cts.Token)
                .ConfigureAwait(false);

            _logger.GotResourcesFromResourceProvider(initialData.Count);

            _logger.LogCompilingInitialResources();
            var data = new InitialResourceData();
            data.Resources.Add(initialData);
            _logger.LogInitialResourcesCompiled();

            _logger.WritingInitialResourcesToStream();
            await responseStream
                .WriteAsync(new WatchResourcesUpdate { InitialData = data }, CancellationToken.None)
                .ConfigureAwait(false);
            _logger.InitialResourcesWroteToStreamSuccessfully();

            await foreach (var resourceUpdate in updates.WithCancellation(cts.Token).ConfigureAwait(false))
            {
                // Skip empty updates.
                if (resourceUpdate is null)
                {
                    continue;
                }

                _logger.LogGotResourceUpdate(resourceUpdate);

                EnsureResourceUpdateNotEmpty(resourceUpdate);

                var changes = new WatchResourcesChanges { Value = { resourceUpdate } };

                await responseStream.WriteAsync(new WatchResourcesUpdate { Changes = changes }, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return.
        }
        catch (IOException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Cancelled writes throw IOException.
        }
        catch (Exception ex)
        {
            _logger.LogErrorWatchingResources(context.Method, ex);
            throw;
        }

        static void EnsureResourceUpdateNotEmpty(WatchResourcesChange resourceUpdate)
        {
            if (resourceUpdate.Delete is null && resourceUpdate.Upsert is null)
            {
                throw new InvalidOperationException("Resource update is empty.");
            }
        }
    }

    public override async Task WatchResourceConsoleLogs(WatchResourceConsoleLogsRequest request, IServerStreamWriter<WatchResourceConsoleLogsUpdate> responseStream, ServerCallContext context)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping,
            context.CancellationToken);

        var update = new WatchResourceConsoleLogsUpdate();
        var lineNumber = 0;
        try
        {
            await foreach (var log in _resourceProvider.GerResourceLogs(request.ResourceName, cts.Token)
                               .ConfigureAwait(false))
            {
                update.LogLines.Add(new ConsoleLogLine { Text = log.Line, IsStdErr = false, LineNumber = ++lineNumber });
                await responseStream.WriteAsync(update, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return.
        }
        catch (IOException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Cancelled writes throw IOException.
        }
    }
}

internal static partial class WatchResourcesLogs
{
    [LoggerMessage(LogLevel.Trace, "Returning application information")]
    public static partial void ReturningApplicationInformation(this ILogger logger);

    [LoggerMessage(Events.PreparingToGetResources, LogLevel.Trace,
        "Preparing to get resources from the resource provider")]
    public static partial void GettingResourcesFromResourceProvider(this ILogger logger);

    [LoggerMessage(Events.ResourcesReceived, LogLevel.Trace, "Received {Count} resources from resource provider")]
    public static partial void GotResourcesFromResourceProvider(this ILogger logger, int count);

    [LoggerMessage(Events.CompilingInitialResources, LogLevel.Trace, "Preparing to compile initial resources")]
    public static partial void LogCompilingInitialResources(this ILogger logger);

    [LoggerMessage(Events.InitialResourcesCompiled, LogLevel.Trace, "Initial resources compiled")]
    public static partial void LogInitialResourcesCompiled(this ILogger logger);

    [LoggerMessage(Events.SendingInitialResources, LogLevel.Trace, "Preparing to send initial resources")]
    public static partial void WritingInitialResourcesToStream(this ILogger logger);

    [LoggerMessage(Events.InitialResourcesSent, LogLevel.Trace, "Initial resources sent")]
    public static partial void InitialResourcesWroteToStreamSuccessfully(this ILogger logger);

    [LoggerMessage(Events.ErrorWatchingResources, LogLevel.Error, "Error executing service method {Method}")]
    public static partial void LogErrorWatchingResources(this ILogger logger, string method, Exception ex);

    [LoggerMessage(Events.ResourceUpdateReceived, LogLevel.Debug, "Got resource update: {Update}")]
    public static partial void LogGotResourceUpdate(this ILogger logger, WatchResourcesChange update);

    private struct Events
    {
        internal const int PreparingToGetResources = 101;
        internal const int ResourcesReceived = 102;
        internal const int CompilingInitialResources = 103;
        internal const int InitialResourcesCompiled = 104;
        internal const int SendingInitialResources = 105;
        internal const int InitialResourcesSent = 106;
        internal const int ResourceUpdateReceived = 107;
        internal const int ErrorWatchingResources = 501;
    }
}
