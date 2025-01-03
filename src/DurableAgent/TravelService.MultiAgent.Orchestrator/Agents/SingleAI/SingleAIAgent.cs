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
using TravelService.MultiAgent.Orchestrator.Agents.Booking.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.Flight.Plugins;
using TravelService.MultiAgent.Orchestrator.Agents.Weather.Plugins;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
using TravelService.Plugins.Common;

#pragma warning disable SKEXP0040 
namespace TravelService.MultiAgent.Orchestrator.Agents
{
   public class SingleAIAgent
   {
      private readonly ILogger<SingleAIAgent> _logger;
      private readonly Kernel _kernel;
      private readonly IPromptyService _prompty;
      private readonly IKernelService _kernelService;
      private readonly IConfiguration _configuration;
      private readonly ICosmosClientService _cosmosClientService;
      private readonly IServiceProvider _serviceProvider;

      public SingleAIAgent(ILogger<SingleAIAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService, IConfiguration configuration, ICosmosClientService cosmosClientService, IServiceProvider serviceProvider)
      {
         _logger = logger;
         _kernel = kernel;
         _prompty = prompty;
         _kernelService = kernelService;
         _configuration = configuration;
         _cosmosClientService = cosmosClientService;
         _serviceProvider = serviceProvider;
      }

      [Function(nameof(TriggerSingleAIAgent))]
      public async Task<ChatMessageContent> TriggerSingleAIAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {
         try
         {
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new FlightPlugin(_serviceProvider)));
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new WeatherPlugin(_serviceProvider)));
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new BookingPlugin(_serviceProvider)));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "SingleAI", "SingleAIAgent.prompty"), _kernel, new KernelArguments
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
