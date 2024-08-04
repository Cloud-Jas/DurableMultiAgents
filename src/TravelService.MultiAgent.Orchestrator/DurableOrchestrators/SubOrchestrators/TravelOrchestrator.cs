using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Agents;
using TravelService.MultiAgent.Orchestrator.Contracts;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace TravelService.MultiAgent.Orchestrator.DurableOrchestrators
{
    public class TravelOrchestrator
    {
        private readonly TelemetryClient telemetryClient;

        public TravelOrchestrator(TelemetryClient telemetry)
        {
            telemetryClient = telemetry;
        }

        [Function(nameof(TravelOrchestrator))]
        public async Task<RequestData> RunOrchestrator(
                           [OrchestrationTrigger] TaskOrchestrationContext context, RequestData requestData, ILogger logger)
        {
            telemetryClient.TrackTrace("Orchestrator started.", SeverityLevel.Information);

            var routeAgents = await context.CallActivityAsync<string>(nameof(ManagerAgent.RouteAgents), requestData);

            if (routeAgents == nameof(FlightAgent.TriggerFlightAgent))
            {

                var flightAgentResponse = await context.CallActivityAsync<ChatMessageContent>(nameof(FlightAgent.TriggerFlightAgent), requestData);

                requestData.ChatHistory.Add("FlightAgent: " + flightAgentResponse!.Content);

                requestData.IntermediateResponse = flightAgentResponse!.Content;

                // To make weather agent call mandatory for every flight agent call comment out the below line
                //routeAgents = await context.CallActivityAsync<string>(nameof(ManagerAgent.RouteAgents), requestData); 
            }

            if (routeAgents == nameof(WeatherAgent.TriggerWeatherAgent))
            {
                var weatherAgentResponse = await context.CallActivityAsync<ChatMessageContent>(nameof(WeatherAgent.TriggerWeatherAgent), requestData);

                requestData.ChatHistory.Add("WeatherAgent: " + weatherAgentResponse!.Content);

                requestData.IntermediateResponse = weatherAgentResponse!.Content;                
            }

            if(routeAgents == nameof(BookingAgent.TriggerBookingAgent))
            {

                var bookingAgentResponse = await context.CallActivityAsync<ChatMessageContent>(nameof(BookingAgent.TriggerBookingAgent), requestData);

                requestData.ChatHistory.Add("BookingAgent: " + bookingAgentResponse!.Content);

                requestData.IntermediateResponse = bookingAgentResponse!.Content;

                return requestData;
            }

            return requestData;

        }
    }
}
