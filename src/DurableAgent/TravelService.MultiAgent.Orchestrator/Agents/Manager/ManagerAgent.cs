using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using StackExchange.Redis;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;
using TravelService.MultiAgent.Orchestrator.Services;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;
using TravelService.Plugins.Common;

namespace TravelService.MultiAgent.Orchestrator.Agents
{
   public class ManagerAgent
   {
      private readonly ILogger<ManagerAgent> _logger;
      private readonly Kernel _kernel;
      private readonly IPromptyService _prompty;
      private readonly IKernelService _kernelService;
      private readonly ICosmosClientService _cosmosClientService;
      private readonly IUserServiceClient _userServiceClient;
      private readonly IActivityTriggerTracingHandler _activityTriggerTracingHandler;
      private IConnectionMultiplexer redisConnection;

      public ManagerAgent(ILogger<ManagerAgent> logger, Kernel kernel,
         IPromptyService prompty, IKernelService kernelService,
         ICosmosClientService cosmosClientService, IActivityTriggerTracingHandler activityTriggerTracingHandler,
         IUserServiceClient userServiceClient, IConnectionMultiplexer redisConnection)
      {
         _logger = logger;
         _kernel = kernel;
         _prompty = prompty;
         _kernelService = kernelService;
         _cosmosClientService = cosmosClientService;
         _activityTriggerTracingHandler = activityTriggerTracingHandler;
         _userServiceClient = userServiceClient;
         this.redisConnection = redisConnection;
      }

      [Function(nameof(StoreAndPubData))]
      public async Task StoreAndPubData([ActivityTrigger] StoreAndPubInput input, FunctionContext executionContext)
      {
         try
         {
            var requestData = input.RequestData;
            var agentChatHistory = input.AgentChatHistory;
            var response = input.Response;
            _logger.LogInformation("Storing and publishing data for user query: {userQuery}.", requestData.UserQuery);

            await _cosmosClientService.StoreChatHistoryAsync(requestData.SessionId, response, requestData.UserId, requestData.UserName, true, agentChatHistory);

            if (requestData.AssistantType.Equals("Realtime"))
            {
               await redisConnection.GetSubscriber().PublishAsync(
               RedisChannel.Literal($"booking:{requestData.SessionId}"), requestData.FunctionCallId + "~" + response);
            }
            else
            {
               await redisConnection.GetSubscriber().PublishAsync(
              RedisChannel.Literal($"booking:{requestData.SessionId}"), "Updated");
            }
         }
         catch (Exception ex)
         {
            _logger.LogError("Error in StoreAndPubData: {ex}", ex);
         }
      }

      [Function(nameof(FormRequestData))]
      public async Task<RequestData> FormRequestData([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {
         Func<RequestData, FunctionContext, Task<RequestData>> callFormRequestData = async (requestData, executionContext) =>
         {
            _logger.LogInformation("Forming request data for user query: {userQuery}.", requestData.UserQuery);

            var userDetails = await _userServiceClient.GetPassengerByIdAsync(requestData.UserId);

            var chatHistory = await _cosmosClientService.FetchChatHistoryAsync(requestData.SessionId);

            requestData.ChatHistory = chatHistory;
            requestData.UserName = userDetails.FirstName;
            requestData.UserMailId = userDetails.Email;

            return requestData;
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callFormRequestData, requestData, executionContext);
      }

      [Function(nameof(RouteOrchestrators))]
      public async Task<string> RouteOrchestrators([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {
         Func<RequestData, FunctionContext, Task<string>> callRouteOrchestrators = async (requestData, executionContext) =>
         {
            _logger.LogInformation("Routing agents for user query: {userQuery}.", requestData.UserQuery);

            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Manager", "ManagerOrchestrator.prompty"), _kernel, new KernelArguments
            {
                { "context", requestData.UserQuery },
                { "history", requestData.ChatHistory },
                { "userId",requestData.UserId },
                { "userName", requestData.UserName},
                { "email", requestData.UserMailId },
                { "orchestrators", Helper.Utility.GetOrchestrators() }
            });

            var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

            try
            {
               var orchestrator = JsonConvert.DeserializeObject<Contracts.Orchestrator>(result.Content!);
               return orchestrator!.OrchestratorName;
            }
            catch (Exception ex)
            {
               return result.Content!;
            }
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callRouteOrchestrators, requestData, executionContext);
      }
      [Function(nameof(TriggerConsolidaterAgent))]
      public async Task<string> TriggerConsolidaterAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {

         Func<RequestData, FunctionContext, Task<string>> callConsolidaterAgent = async (requestData, executionContext) =>
         {
            _logger.LogInformation("Consolidater agent for user query: {userQuery}.", requestData.UserQuery);

            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Manager", "ManagerConsolidator.prompty"), _kernel, new KernelArguments
            {
                { "context", requestData.UserQuery},
                { "agentResponses", requestData.IntermediateResponse},
                { "history", requestData.ChatHistory },
                { "userId",requestData.UserId },
                { "userName", requestData.UserName},
                { "email", requestData.UserMailId }
            });

            var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

            return result.Content!;
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callConsolidaterAgent, requestData, executionContext);
      }
      [Function(nameof(RouteAgents))]
      public async Task<string> RouteAgents([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {

         Func<RequestData, FunctionContext, Task<string>> callRouteAgent = async (requestData, executionContext) =>
         {
            _logger.LogInformation("Route Agents for user query: {userQuery}.", requestData.UserQuery);

            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Manager", "ManagerAgent.prompty"), _kernel, new KernelArguments
            {
                { "context", requestData.UserQuery },
                { "history", requestData.ChatHistory },
                { "userId",requestData.UserId },
                { "userName", requestData.UserName},
                { "email", requestData.UserMailId },
                { "agents", Helper.Utility.GetAgents() }
            });

            var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

            try
            {
               var agents = JsonConvert.DeserializeObject<Agent>(result.Content!);
               return agents!.AgentName;
            }
            catch (Exception ex)
            { // Retry once if there is error in deserializing the result
               try
               {
                  var Retryresult = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);
                  var agents = JsonConvert.DeserializeObject<Agent>(Retryresult.Content!);
                  return agents!.AgentName;
               }
               catch (Exception ex1)
               {
                  return "NotFound";
               }
            }
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callRouteAgent, requestData, executionContext);
      }
      [Function(nameof(RouteDefaultAgents))]
      public async Task<string> RouteDefaultAgents([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
      {

         Func<RequestData, FunctionContext, Task<string>> callRouteDefaultAgent = async (requestData, executionContext) =>
         {
            _logger.LogInformation("Route default Agents for user query: {userQuery}.", requestData.UserQuery);

            _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new CalendarPlugin()));

            var prompt = await _prompty.RenderPromptAsync(Path.Combine("Agents", "Manager", "ManagerDefaultAgent.prompty"), _kernel, new KernelArguments
            {
                { "context", requestData.UserQuery },
                { "history", requestData.ChatHistory +"\n"+ requestData.IntermediateResponse },
                { "userId",requestData.UserId },
                { "userName", requestData.UserName},
                { "email", requestData.UserMailId },
                { "agents", Helper.Utility.GetDefaultAgents() }
            });

            var result = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);

            try
            {
               var agents = JsonConvert.DeserializeObject<Agent>(result.Content!);
               return agents!.AgentName;
            }
            catch (Exception ex)
            { // Retry once if there is error in deserializing the result
               try
               {
                  var Retryresult = await _kernelService.GetChatMessageContentAsync(_kernel, prompt);
                  var agents = JsonConvert.DeserializeObject<Agent>(Retryresult.Content!);
                  return agents!.AgentName;
               }
               catch (Exception ex1)
               {
                  return "NotFound";
               }
            }
         };

         return await _activityTriggerTracingHandler.ExecuteActivityTrigger(callRouteDefaultAgent, requestData, executionContext);
      }
   }
}

