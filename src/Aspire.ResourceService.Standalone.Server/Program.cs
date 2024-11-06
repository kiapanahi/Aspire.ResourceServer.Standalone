using Aspire.ResourceService.Standalone.ResourceProvider;
using Aspire.ResourceService.Standalone.Server.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/info", (IServiceInformationProvider sip) => Results.Ok(sip.GetServiceInformation()));


app.Run();
