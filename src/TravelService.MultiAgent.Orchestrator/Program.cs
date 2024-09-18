using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
using Microsoft.Azure.Cosmos;
using TravelService.MultiAgent.Orchestrator.Contracts;
using SendGrid;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using TravelService.MultiAgent.Orchestrator.Agents.Flight.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.Weather.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.Booking.Plugins;

#pragma warning disable SKEXP0010

var host = new HostBuilder()
     .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
       services.AddApplicationInsightsTelemetryWorkerService();
       services.ConfigureFunctionsApplicationInsights();
       services.AddLogging();

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
       options.ValidateScopes = true;
    })
    .Build();

host.Run();
