using Aspire.ResourceServer.Standalone.ResourceLocator;
using Aspire.ResourceServer.Standalone.Server.Diagnostics;
using Aspire.ResourceServer.Standalone.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddServiceInformationProvider();
builder.Services.AddResourceProvider();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGrpcService<DashboardService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/info", (IServiceInformationProvider sip) => Results.Ok(sip.GetServiceInformation()));

app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
