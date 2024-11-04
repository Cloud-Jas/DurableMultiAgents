using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
using Microsoft.Azure.Cosmos;
using SendGrid;
using Azure.Identity;
using StackExchange.Redis;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using TravelService.MultiAgent.Orchestrator.Models;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using static TravelService.MultiAgent.Orchestrator.Helper.Extenstions;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;
using AzureFunctions.Extensions.Middleware.Abstractions;
using AzureFunctions.Extensions.Middleware.Infrastructure;
using Microsoft.AspNetCore.Http;
using TravelService.MultiAgent.Orchestrator.Middlewares;
using Azure.Monitor.OpenTelemetry.Exporter;

#pragma warning disable SKEXP0010

var host = new HostBuilder()
     .ConfigureFunctionsWebApplication(applicationBuilder =>
     {
        applicationBuilder.UseFunctionContextAccessor();
     })
    .ConfigureServices(services =>
    {
       services.AddApplicationInsightsTelemetryWorkerService();
       services.ConfigureFunctionsApplicationInsights();
       services.AddLogging();
       services.AddFunctionContextAccessor();

       #region OpenTelemetry       
       var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName: "TravelService", serviceVersion: "1.0.0");
       var openTelemetryTracerProvider = Sdk.CreateTracerProviderBuilder()
               .AddOtlpExporter()
               .AddAzureMonitorTraceExporter(c => c.ConnectionString = Environment.GetEnvironmentVariable("APPINSIGHTS_CONNECTION_STRING"))
               .AddSource("TravelService")
               .SetSampler(new AlwaysOnSampler())
               .SetResourceBuilder(openTelemetryResourceBuilder)
               .Build();

       var metricsProvider = Sdk.CreateMeterProviderBuilder().AddAzureMonitorMetricExporter();

       services.AddSingleton<TracerProvider>(openTelemetryTracerProvider);
       services.AddSingleton<ILoggerProvider, OpenTelemetryLoggerProvider>();
       services.AddScoped<IActivityTriggerTracingHandler, ActivityTriggerTracingHandler>();
       services.AddScoped<IOrchestratorTriggerTracingHandler, OrchestratorTriggerTracingHandler>();
       services.AddScoped<IPluginTracingHandler, PluginTracingHandler>();
       services.Configure<OpenTelemetryLoggerOptions>((openTelemetryLoggerOptions) =>
       {
          openTelemetryLoggerOptions.SetResourceBuilder(openTelemetryResourceBuilder);
          openTelemetryLoggerOptions.IncludeFormattedMessage = true;
          openTelemetryLoggerOptions.AddConsoleExporter();
          openTelemetryLoggerOptions.AddAzureMonitorLogExporter(c=> c.ConnectionString = Environment.GetEnvironmentVariable("APPINSIGHTS_CONNECTION_STRING"));
       }
       );

       services.AddOpenTelemetry(b =>
       {
          b.IncludeFormattedMessage = true;
          b.IncludeScopes = true;
          b.ParseStateValues = true;
       });
       #endregion

       #region Middleware       
       services.AddTransient<IHttpMiddlewareBuilder, HttpMiddlewareBuilder>((sp) =>
       {
          var funcBuilder = new HttpMiddlewareBuilder(sp.GetRequiredService<IHttpContextAccessor>());
          funcBuilder.Use(new HttpExceptionMiddleware(sp.GetRequiredService<ILogger<HttpExceptionMiddleware>>()));
          funcBuilder.Use(new OpenTelemetryHttpMiddleware(sp.GetRequiredService<ILogger<OpenTelemetryHttpMiddleware>>(), openTelemetryTracerProvider.GetTracer("TravelService"), sp.GetRequiredService<TracingContextCache>()));
          return funcBuilder;
       });
       #endregion


       string cosmosdbAccountEndpoint = Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint");
       string openaiEndpoint = Environment.GetEnvironmentVariable("OpenAIEndpoint");
       string openaiChatCompletionDeploymentName = Environment.GetEnvironmentVariable("OpenAIChatCompletionDeploymentName");
       string openaiTextEmbeddingGenerationDeploymentName = Environment.GetEnvironmentVariable("OpenAITextEmbeddingGenerationDeploymentName");
       var userAssignedIdentityClientId = Environment.GetEnvironmentVariable("UserAssignedIdentity");

       var credentialOptions = new DefaultAzureCredentialOptions
       {
          TenantId = Environment.GetEnvironmentVariable("TenantId")
       };
       if (!string.IsNullOrEmpty(userAssignedIdentityClientId))
       {
          credentialOptions.ManagedIdentityClientId = userAssignedIdentityClientId;
       }
       services.AddSingleton(s =>
       {
          CosmosClientOptions options = new CosmosClientOptions()
          {
             ConnectionMode = ConnectionMode.Direct
          };

          return new CosmosClient(cosmosdbAccountEndpoint, new DefaultAzureCredential(credentialOptions), options);
       });

       services.AddSingleton<AzureOpenAIChatCompletionService>(provider =>
       {
          return new AzureOpenAIChatCompletionService(
               deploymentName: openaiChatCompletionDeploymentName,
               credentials: new DefaultAzureCredential(credentialOptions),
               endpoint: openaiEndpoint
           );
       });
       services.AddSingleton<IChatCompletionService, AzureOpenAIChatCompletionService>(pprovider =>
       {
          return new AzureOpenAIChatCompletionService(
               deploymentName: openaiChatCompletionDeploymentName,
               credentials: new DefaultAzureCredential(credentialOptions),
               endpoint: openaiEndpoint
           );
       });
       services.AddSingleton<AzureOpenAITextEmbeddingGenerationService>(provider =>
       {
          return new AzureOpenAITextEmbeddingGenerationService(deploymentName: openaiTextEmbeddingGenerationDeploymentName,
               endpoint: openaiEndpoint,
               credential: new DefaultAzureCredential(credentialOptions));
       });

       services.AddSingleton<IConnectionMultiplexer>(sp =>
       {
          var redisConfiguration = ConfigurationOptions.Parse(Environment.GetEnvironmentVariable("RedisConnectionString"), true);
          return ConnectionMultiplexer.Connect(redisConfiguration);
       });

       services.AddSingleton<SendGridClient>(provider =>
        {
           return new SendGridClient(Environment.GetEnvironmentVariable("SendGridApiKey"));
        });

       services.AddScoped<Kernel>(provider =>
       {
          var builder = Kernel.CreateBuilder();

          builder.AddAzureOpenAIChatCompletion(deploymentName: openaiChatCompletionDeploymentName,
               credentials: new DefaultAzureCredential(credentialOptions),
               endpoint: openaiEndpoint);
          return builder.Build();
       });
       services.AddScoped<TracingContextCache>();
       services.AddHttpContextAccessor();
       services.AddHttpClient<IPostmarkServiceClient, PostmarkServiceClient>(client =>
       {
          client.BaseAddress = new Uri("https://api.postmarkapp.com");
          client.DefaultRequestHeaders.Add("Accept", "application/json");
          client.DefaultRequestHeaders.Add("X-Postmark-Server-Token", Environment.GetEnvironmentVariable("PostmarkServerToken"));
       });
       services.AddScoped<IPromptyService, PromptyService>();
       services.AddScoped<IKernelService, KernelService>();
       services.AddScoped<INL2SQLService, NL2SQLService>();
       services.AddScoped<ICosmosClientService, CosmosClientService>();
       services.AddScoped<IPostmarkServiceClient, PostmarkServiceClient>();
    })
    .UseDefaultServiceProvider((context, options) =>
    {
       options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    })
    .Build();

host.Run();
