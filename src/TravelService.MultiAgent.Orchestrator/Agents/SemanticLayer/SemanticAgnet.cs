using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;
using Newtonsoft.Json;
using TravelService.MultiAgent.Orchestrator.Agents.Booking.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.Flight.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.SemanticLayer.Plugins;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;
using TravelService.MultiAgent.Orchestrator.Services;
using TravelService.Plugins.Common;

#pragma warning disable SKEXP0040 
namespace TravelService.MultiAgent.Orchestrator.Agents
{
    public class SemanticAgent
    {
        private readonly ILogger<SemanticAgent> _logger;
        private readonly Kernel _kernel;
        private readonly IPromptyService _prompty;
        private readonly IKernelService _kernelService;
        private readonly IConfiguration _configuration;
        private readonly ICosmosClientService _cosmosClientService;
        private readonly IServiceProvider _serviceProvider;

        public SemanticAgent(ILogger<SemanticAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService, IConfiguration configuration, ICosmosClientService cosmosClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _kernel = kernel;
            _prompty = prompty;
            _kernelService = kernelService;
            _configuration = configuration;
            _cosmosClientService = cosmosClientService;
            _serviceProvider = serviceProvider;
        }

        [Function(nameof(TriggerSemanticAgent))]
        public async Task<string> TriggerSemanticAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
        {
            try
            {
                _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new SemanticLayerPlugin(_serviceProvider)));
                _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

                var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "SemanticLayer", "SemanticAgent.prompty"), _kernel, new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "history", requestData.ChatHistory },
                    { "userId",requestData.UserId },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId },
                    { "semanticLayers", Helper.Utility.GetSemanticLayers() }
                });

                var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

                return result.Content!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SemanticAgent.");
                throw;
            }
        }
        [Function(nameof(TriggerVectorSemanticAgent))]
        public async Task<string> TriggerVectorSemanticAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
        {
            try
            {
                _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new VectorSearchPlugin(_serviceProvider)));
                _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

                var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "SemanticLayer", "VectorSemanticAgent.prompty"), _kernel, new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "history", requestData.ChatHistory },
                    { "userId",requestData.UserId },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId },
                    { "semanticLayers", Helper.Utility.GetVectorSemanticLayers() }
                });

                var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

                return result.Content!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SemanticAgent.");
                throw;
            }
        }
    }

}
