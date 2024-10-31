using Aspire.ResourceServer.Standalone.Server.Diagnostics;

using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IServiceInformationProvider, AssemblyServiceInformationProvider>();
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGrpcService<DashboardService>();

app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
