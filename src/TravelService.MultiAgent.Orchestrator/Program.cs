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

#pragma warning disable SKEXP0010

var host = new HostBuilder()
     .ConfigureFunctionsWorkerDefaults()          
    .ConfigureServices(services =>
    {        
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging();

        string accountEndpoint = Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint");

        services.AddSingleton(s =>
        {
            CosmosClientOptions options = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct
            };

            return new CosmosClient(accountEndpoint, new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                TenantId = Environment.GetEnvironmentVariable("TenantId")
            }), options);
        });

        services.AddSingleton<AzureOpenAIChatCompletionService>(provider =>
        {
            return new AzureOpenAIChatCompletionService(
                deploymentName: "gpt4",
                credentials: new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = Environment.GetEnvironmentVariable("TenantId")
                }),
                endpoint: "https://azopenaigpt4turbo.openai.azure.com"
            );
        });

        services.AddSingleton<AzureOpenAITextEmbeddingGenerationService>(provider =>
        {
            return new AzureOpenAITextEmbeddingGenerationService(deploymentName: "embedding4",
                endpoint: "https://azopenaigpt4turbo.openai.azure.com",
                credential: new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = Environment.GetEnvironmentVariable("TenantId")
                }));
        });

        services.AddSingleton<SendGridClient>(provider =>
        {
            return new SendGridClient(Environment.GetEnvironmentVariable("SendGridApiKey"));
        });

        services.AddScoped<Kernel>(provider =>
        {
            var builder = Kernel.CreateBuilder();
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
