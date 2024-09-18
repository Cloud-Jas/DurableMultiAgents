using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
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
using Agent = TravelService.MultiAgent.Orchestrator.Contracts.Agent;

#pragma warning disable SKEXP0040 
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
namespace TravelService.MultiAgent.Orchestrator.Agents
{
   public class AutoGenAgent
   {
      private readonly ILogger<AutoGenAgent> _logger;
      private readonly Kernel _kernel;
      private readonly IPromptyService _prompty;
      private readonly IKernelService _kernelService;
      private readonly IConfiguration _configuration;
      private readonly ICosmosClientService _cosmosClientService;
      private readonly IServiceProvider _serviceProvider;

      public AutoGenAgent(ILogger<AutoGenAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService, IConfiguration configuration, ICosmosClientService cosmosClientService, IServiceProvider serviceProvider)
      {
         _logger = logger;
         _kernel = kernel;
         _prompty = prompty;
         _kernelService = kernelService;
         _configuration = configuration;
         _cosmosClientService = cosmosClientService;
         _serviceProvider = serviceProvider;
      }
      public async Task<ChatCompletionAgent> GetChatCompletionAgentAsync(RequestData requestData, string agentName, Kernel kernel)
      {
         var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", agentName, $"{agentName}Agent.prompty"), kernel, new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "userId",requestData.UserId },
                    {"history", requestData.ChatHistory },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId }
                });

         switch (agentName)
         {
            case "Flight":
               if (kernel.Plugins.FirstOrDefault(p => p.Name == "FlightPlugin") == null)
               {
                  kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new FlightPlugin(_serviceProvider)));
               }
               break;
            case "Weather":
               if (kernel.Plugins.FirstOrDefault(p => p.Name == "WeatherPlugin") == null)
               {
                  kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new WeatherPlugin(_serviceProvider)));
               }
               break;
            case "Booking":
               if (kernel.Plugins.FirstOrDefault(p => p.Name == "BookingPlugin") == null)
               {
                  kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new BookingPlugin(_serviceProvider)));
               }
               break;
         }

         ChatCompletionAgent agent = new ChatCompletionAgent
         {
            Arguments = new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "userId",requestData.UserId },
                    {"history", requestData.ChatHistory },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId }
                },
            Name = agentName + "Agent",
            Instructions = prompt,
            Kernel = kernel
         };

         return agent;
      }
      [Function(nameof(TriggerAutoGenAgent))]
      public async Task<string> TriggerAutoGenAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {
         try
         {
            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

            var terminateFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                Determine if the booking confirmation is sent to mail. If so, respond with a single word: yes.

                History:

                {{$history}}
                """
                );
            var selectionFunction = KernelFunctionFactory.CreateFromPrompt(
               $$$"""
               Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
               State only the name of the participant to take the next turn.

               Choose only from these participants:               
               -FlightAgent
               -WeatherAgent
               -BookingAgent

               Always follow these steps when selecting the next participant:              
                 - Get all flight options
                 - Once you get the flight options, then get the weather details of all possible locations
                 - And finally, book the flight based on best possible flight options and send confirmation mail

               Make sure to perform autonomously and don't ask for any user input.

               History:
               {{$history}}
               """
               );

            AgentGroupChat chat = new(await GetChatCompletionAgentAsync(requestData, "Flight", _kernel), await GetChatCompletionAgentAsync(requestData, "Weather", _kernel), await GetChatCompletionAgentAsync(requestData, "Booking", _kernel))
            {
               ExecutionSettings = new AgentGroupChatSettings
               {
                  TerminationStrategy =
                    new KernelFunctionTerminationStrategy(terminateFunction, _kernel)
                    {
                       Agents = [await GetChatCompletionAgentAsync(requestData, "Manager", _kernel)],
                       ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                       HistoryVariableName = "history",
                       Arguments = new KernelArguments
                       {
                          { "context", requestData.UserQuery },
                          { "userId",requestData.UserId },
                          { "userName", requestData.UserName},
                          { "email", requestData.UserMailId }
                       },
                       MaximumIterations = 3,
                       AgentVariableName = "agents"
                    },
                  SelectionStrategy = new KernelFunctionSelectionStrategy(await _prompty.GetKernelFuntionAsync(Path.Combine("Agents", "Manager", $"ManagerAgent.prompty"), _kernel, new KernelArguments
                {
                    { "context", requestData.UserQuery },
                    { "userId",requestData.UserId },
                    {"history", requestData.ChatHistory },
                    { "userName", requestData.UserName},
                    { "email", requestData.UserMailId }
                }), _kernel)
                  {                     
                     InitialAgent = await GetChatCompletionAgentAsync(requestData, "Manager", _kernel),                     
                     UseInitialAgentAsFallback = true,
                     AgentsVariableName = "agents",
                     HistoryVariableName = "history",
                     Arguments = new KernelArguments
                       {
                          { "context", requestData.UserQuery },
                          { "userId",requestData.UserId },
                          { "userName", requestData.UserName},
                          { "email", requestData.UserMailId }
                       }
                  }
               }
            };

            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, requestData.UserQuery));
            var intermediateResponse = string.Empty;
            await foreach (var content in chat.InvokeAsync())
            {
               Console.WriteLine();
               Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
               Console.WriteLine();              
               intermediateResponse = content.Content;
            }

            return intermediateResponse;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error occurred in FlightAgent.");
            throw;
         }
      }
   }
}
