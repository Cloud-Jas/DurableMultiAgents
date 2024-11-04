using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;
using StackExchange.Redis;
using TravelService.MultiAgent.Orchestrator.Agents;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using System.Linq;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using TravelService.MultiAgent.Orchestrator.Models;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

namespace TravelService.MultiAgent.Orchestrator.DurableOrchestrators
{
   public class ManagerOrchestrator
   {
      private readonly TelemetryClient telemetryClient;
      private ICosmosClientService _cosmosClientService;
      private IConnectionMultiplexer redisConnection;
      private IOrchestratorTriggerTracingHandler _orchestratorTriggerTracingHandler;
      private readonly TracingContextCache _cache;


      public ManagerOrchestrator(TelemetryClient telemetry, ICosmosClientService cosmosClientService,
         IConnectionMultiplexer connectionMultiplexer, TracingContextCache cache, IOrchestratorTriggerTracingHandler orchestratorTriggerTracingHandler)
      {
         telemetryClient = telemetry;
         _cosmosClientService = cosmosClientService;
         redisConnection = connectionMultiplexer;
         _cache = cache;
         _orchestratorTriggerTracingHandler = orchestratorTriggerTracingHandler;
      }

      [Function(nameof(ManagerOrchestrator))]
      public async Task<string> RunOrchestrator(
              [OrchestrationTrigger] TaskOrchestrationContext context, RequestData requestData, ILogger logger)
      {
         try
         {
            telemetryClient.TrackTrace("Orchestrator started.", SeverityLevel.Information);

            var managerResponse = await context.CallActivityAsync<string>(nameof(ManagerAgent.RouteOrchestrators), requestData);

            requestData.ChatHistory.Add("## ManagerAgent: \n" + managerResponse);

            requestData.IntermediateResponse = requestData.UserQuery;

            string response = managerResponse;

            var orchestrators = Utility.GetOrchestratorNames();

            if (orchestrators.Contains(managerResponse))
            {
               var subOrchestratorResponse = await context.CallSubOrchestratorAsync<RequestData>(managerResponse, requestData);

               telemetryClient.TrackTrace("Sub Orchestration completed: " + managerResponse, SeverityLevel.Information);
               response = subOrchestratorResponse.IntermediateResponse;
               requestData.ChatHistory = subOrchestratorResponse.ChatHistory;
            }

            var agentNames = Utility.GetAgentNames();

            var agentChatHistory = requestData.ChatHistory
                .Where(chat => chat.StartsWith("##") && agentNames.Any(agent => chat.Contains(agent)))
                .ToList();

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
            return response;
         }
         catch (Exception ex)
         {
            telemetryClient.TrackException(ex);
            throw;
         }

      }
   }
}
