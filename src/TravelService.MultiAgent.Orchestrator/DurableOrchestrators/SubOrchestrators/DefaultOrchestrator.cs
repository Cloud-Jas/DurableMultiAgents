using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TravelService.MultiAgent.Orchestrator.Agents;
using TravelService.MultiAgent.Orchestrator.Contracts;

namespace TravelService.MultiAgent.Orchestrator.DurableOrchestrators
{
   public class DefaultOrchestrator
   {
      private readonly TelemetryClient telemetryClient;

      public DefaultOrchestrator(TelemetryClient telemetry)
      {
         telemetryClient = telemetry;
      }

      [Function(nameof(DefaultOrchestrator))]
      public async Task<RequestData> RunOrchestrator(
                         [OrchestrationTrigger] TaskOrchestrationContext context, RequestData requestData, ILogger logger)
      {
         telemetryClient.TrackTrace("Orchestrator started.", SeverityLevel.Information);

         requestData.UserQuery = requestData.IntermediateResponse;

         var routeAgents = await context.CallActivityAsync<string>(nameof(ManagerAgent.RouteDefaultAgents), requestData);

         if (routeAgents == nameof(SemanticAgent.TriggerSemanticAgent))
         {

            var tasks = new List<Task<RequestData>>();

            // Fan-out requests to multiple agents

            var semanticAgentTask = context.CallActivityAsync<string>(nameof(SemanticAgent.TriggerSemanticAgent), requestData);

            var vectorSearchAgentTask = context.CallActivityAsync<string>(nameof(SemanticAgent.TriggerVectorSemanticAgent), requestData);

            await Task.WhenAll(semanticAgentTask, vectorSearchAgentTask);

            requestData.IntermediateResponse = "SemanticAgent: " + semanticAgentTask.Result + "\n" + "VectorSearchAgent: " + vectorSearchAgentTask.Result;

            requestData.ChatHistory.Add("## SemanticAgent: \n" + semanticAgentTask.Result + "\n" + "## VectorSearchAgent: \n" + vectorSearchAgentTask.Result);

            // Fan-in results to Consolidator Agent

            var consolidatorAgentTask = await context.CallActivityAsync<string>(nameof(ManagerAgent.TriggerConsolidaterAgent), requestData);
            requestData.IntermediateResponse = consolidatorAgentTask;           

            return requestData;
         }

         requestData.IntermediateResponse = "No agents found for the given query.";

         return requestData;
      }
   }
}
