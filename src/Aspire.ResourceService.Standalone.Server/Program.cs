using Aspire.ResourceService.Standalone.Server.Diagnostics;
using Aspire.ResourceService.Standalone.Server.ResourceProviders;
using Aspire.ResourceService.Standalone.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddServiceInformationProvider();
builder.Services.AddResourceProvider();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapDefaultEndpoints();
app.MapGet("/info", (IServiceInformationProvider sip) => Results.Ok(sip.GetServiceInformation()));

app.MapGrpcService<DashboardService>();

app.Run();
