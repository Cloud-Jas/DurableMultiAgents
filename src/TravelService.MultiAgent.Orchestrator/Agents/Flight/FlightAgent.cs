using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;
using TravelService.MultiAgent.Orchestrator.Agents.Flight.Plugins;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
using TravelService.Plugins.Common;

#pragma warning disable SKEXP0040 
#pragma warning disable SKEXP0110 
namespace TravelService.MultiAgent.Orchestrator.Agents
{
   public class FlightAgent

   {
      private readonly ILogger<FlightAgent> _logger;
      private readonly Kernel _kernel;
      private readonly IPromptyService _prompty;
      private readonly IKernelService _kernelService;
      private readonly IConfiguration _configuration;
      private readonly ICosmosClientService _cosmosClientService;
      private readonly IServiceProvider _serviceProvider;

      public FlightAgent(ILogger<FlightAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService, IConfiguration configuration, ICosmosClientService cosmosClientService, IServiceProvider serviceProvider)
      {
         _logger = logger;
         _kernel = kernel;
         _prompty = prompty;
         _kernelService = kernelService;
         _configuration = configuration;
         _cosmosClientService = cosmosClientService;
         _serviceProvider = serviceProvider;
      }

      [Function(nameof(TriggerFlightAgent))]
      public async Task<ChatMessageContent> TriggerFlightAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {
         try
         {
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new FlightPlugin(_serviceProvider)));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Flight", "FlightAgent.prompty"), _kernel, new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "history", requestData.ChatHistory },
                    { "userId",requestData.UserId },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId }
                });

            var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

            return result;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error occurred in FlightAgent.");
            throw;
         }
      }      
   }
}
