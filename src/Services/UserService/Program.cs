using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UserService;
using UserService.Interfaces;
using UserService.Middlewares;
using UserService.Models;
using UserService.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TracingContextCache>();

builder.Services.AddDbContext<UserServiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetValue<string>("DbConnString")));

builder.Services.AddScoped<IPassengerRepository,PassengerRepository>();
#region OpenTelemetry       
var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: "UserService", serviceVersion: "1.0.0");
var openTelemetryTracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddOtlpExporter()
        .AddAzureMonitorTraceExporter(c => c.ConnectionString = builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTION_STRING"))
        .AddSource("UserService")
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
   openTelemetryLoggerOptions.AddAzureMonitorLogExporter(c => c.ConnectionString = builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTION_STRING"));
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
