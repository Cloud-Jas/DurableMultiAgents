using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using BookingService.Interfaces;
using BookingService.Services;
using BookingService.Middlewares;
using BookingService.Models;
using Microsoft.Azure.Cosmos;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TracingContextCache>();
builder.Services.AddScoped<ICosmosService, CosmosService>();

#region OpenTelemetry       
var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: "BookingService", serviceVersion: "1.0.0");
var openTelemetryTracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddOtlpExporter()
        .AddAzureMonitorTraceExporter(c => c.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"))
        .AddSource("BookingService")
        .SetSampler(new AlwaysOnSampler())
        .SetResourceBuilder(openTelemetryResourceBuilder)
        .Build();

var metricsProvider = Sdk.CreateMeterProviderBuilder().AddAzureMonitorMetricExporter();

builder.Services.AddSingleton<TracerProvider>(openTelemetryTracerProvider);
builder.Services.AddSingleton<ILoggerProvider, OpenTelemetryLoggerProvider>();
builder.Services.Configure<OpenTelemetryLoggerOptions>((openTelemetryLoggerOptions) =>
{
   openTelemetryLoggerOptions.SetResourceBuilder(openTelemetryResourceBuilder);
   openTelemetryLoggerOptions.IncludeFormattedMessage = true;
   openTelemetryLoggerOptions.AddConsoleExporter();
   openTelemetryLoggerOptions.AddAzureMonitorLogExporter(c => c.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"));
}
);
#endregion
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IPostmarkServiceClient, PostmarkServiceClient>(client =>
{
   client.BaseAddress = new Uri("https://api.postmarkapp.com");
   client.DefaultRequestHeaders.Add("Accept", "application/json");
   client.DefaultRequestHeaders.Add("X-Postmark-Server-Token", builder.Configuration.GetValue<string>("PostmarkServerToken"));
});
string cosmosdbAccountEndpoint = builder.Configuration.GetValue<string>("CosmosDBAccountEndpoint");
var userAssignedIdentityClientId = builder.Configuration.GetValue<string>("ManagedIdentityClientId");

var credentialOptions = new DefaultAzureCredentialOptions
{
   TenantId = builder.Configuration.GetValue<string>("TenantId")
};
if (!string.IsNullOrEmpty(userAssignedIdentityClientId))
{
   credentialOptions.ManagedIdentityClientId = userAssignedIdentityClientId;
}
builder.Services.AddSingleton(s =>
{
   CosmosClientOptions options = new CosmosClientOptions()
   {
      ConnectionMode = ConnectionMode.Direct
   };

   return new CosmosClient(cosmosdbAccountEndpoint, new DefaultAzureCredential(credentialOptions), options);
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<OpenTelemetryMiddleware>();

app.Run();
