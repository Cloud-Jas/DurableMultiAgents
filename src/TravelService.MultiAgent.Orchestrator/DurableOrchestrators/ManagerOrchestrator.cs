using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Agents;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Helper;
using System.Linq;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.DurableOrchestrators
{
    public class ManagerOrchestrator
    {
        private readonly TelemetryClient telemetryClient;
        private ICosmosClientService _cosmosClientService; 

        public ManagerOrchestrator(TelemetryClient telemetry,ICosmosClientService cosmosClientService)
        {
            telemetryClient = telemetry;
            _cosmosClientService = cosmosClientService;
        }

        [Function(nameof(ManagerOrchestrator))]
        public async Task<string> RunOrchestrator(
                [OrchestrationTrigger] TaskOrchestrationContext context, RequestData requestData, ILogger logger)
        {
            try
            {
                telemetryClient.TrackTrace("Orchestrator started.", SeverityLevel.Information);

                var managerResponse = await context.CallActivityAsync<string>(nameof(ManagerAgent.RouteOrchestrators), requestData);

                requestData.ChatHistory.Add("ManagerAgent: " + managerResponse);

                requestData.IntermediateResponse = requestData.UserQuery;

                string response = managerResponse;

                var orchestrators = Utility.GetOrchestratorNames();

                if (orchestrators.Contains(managerResponse))
                {
                    var subOrchestratorResponse = await context.CallSubOrchestratorAsync<RequestData>(managerResponse, requestData);

                    telemetryClient.TrackTrace("Sub Orchestration completed: " + managerResponse, SeverityLevel.Information);

                    return subOrchestratorResponse.IntermediateResponse;
                }

                return response;
            }
            catch(Exception ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }

        }
    }
}
