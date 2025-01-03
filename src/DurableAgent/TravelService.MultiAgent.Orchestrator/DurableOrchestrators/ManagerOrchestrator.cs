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
using Microsoft.Azure.Cosmos;
using TravelService.MultiAgent.Orchestrator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TravelService.MultiAgent.Orchestrator.DurableOrchestrators
{
   public class ManagerOrchestrator
   {
      private readonly TelemetryClient telemetryClient;
      private ICosmosClientService _cosmosClientService;
      private IServiceProvider _serviceProvider; 
      private IUserServiceClient _userServiceClient;
      private IOrchestratorTriggerTracingHandler _orchestratorTriggerTracingHandler;
      private readonly TracingContextCache _cache;


      public ManagerOrchestrator(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         telemetryClient = serviceProvider.GetService<TelemetryClient>();
         _cosmosClientService = serviceProvider.GetService<ICosmosClientService>();
         _orchestratorTriggerTracingHandler = serviceProvider.GetService<IOrchestratorTriggerTracingHandler>();
         _cache = serviceProvider.GetService<TracingContextCache>();
         _userServiceClient = serviceProvider.GetService<IUserServiceClient>();
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

            await context.CallActivityAsync(nameof(ManagerAgent.StoreAndPubData), new StoreAndPubInput
            {
               RequestData = requestData,
               AgentChatHistory = agentChatHistory,
               Response = response
            });

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
