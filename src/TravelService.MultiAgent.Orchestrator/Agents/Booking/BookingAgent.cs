using Azure.AI.Inference;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Agents.Booking.Plugins;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

#pragma warning disable SKEXP0040 
namespace TravelService.MultiAgent.Orchestrator.Agents
{
   public class BookingAgent
   {
      private readonly ILogger<BookingAgent> _logger;
      private readonly Kernel _kernel;
      private readonly IPromptyService _prompty;
      private readonly IKernelService _kernelService;
      private readonly IConfiguration _configuration;
      private readonly ICosmosClientService _cosmosClientService;
      private readonly IServiceProvider _serviceProvider;
      private readonly IActivityTriggerTracingHandler _activityTriggerTracingHandler;

      public BookingAgent(ILogger<BookingAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService,
         IConfiguration configuration, ICosmosClientService cosmosClientService, IServiceProvider serviceProvider,
         IActivityTriggerTracingHandler activityTriggerTracingHandler)
      {
         _logger = logger;
         _kernel = kernel;
         _prompty = prompty;
         _kernelService = kernelService;
         _configuration = configuration;
         _cosmosClientService = cosmosClientService;
         _serviceProvider = serviceProvider;
         _activityTriggerTracingHandler = activityTriggerTracingHandler;
      }

      [Function(nameof(TriggerBookingAgent))]
      public async Task<ChatMessageContent> TriggerBookingAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {

         Func<RequestData, FunctionContext, Task<ChatMessageContent>> callBookingAgent = async (requestData, executionContext) =>
         {
            try
            {
               _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new BookingPlugin(_serviceProvider)));

               var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Booking", "BookingAgent.prompty"), _kernel, new KernelArguments
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
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callBookingAgent,requestData, executionContext);
      }
   }
}
