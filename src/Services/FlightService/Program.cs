using FlightService.Repository;
using FlightService;
using Microsoft.EntityFrameworkCore;
using FlightService.Interfaces;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using FlightService.Middlewares;
using FlightService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TracingContextCache>();

builder.Services.AddDbContext<FlightServiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetValue<string>("DbConnString"), sqlOptions =>
    {
       sqlOptions.EnableRetryOnFailure(
           maxRetryCount: 5,
           maxRetryDelay: TimeSpan.FromSeconds(30),
           errorNumbersToAdd: null
       );
    }));

builder.Services.AddScoped<IFlightServiceRepository,FlightServiceRepository>();
#region OpenTelemetry       
var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: "FlightService", serviceVersion: "1.0.0");
var openTelemetryTracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddOtlpExporter()
        .AddAzureMonitorTraceExporter(c => c.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"))
        .AddSource("FlightService")
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
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<OpenTelemetryMiddleware>();

app.Run();
