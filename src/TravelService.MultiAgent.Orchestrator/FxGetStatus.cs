using Azure.Core;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.DurableOrchestrators;

namespace TravelService.MultiAgent.Orchestrator
{
    public class GetStatus
    {
        private readonly ILogger<GetStatus> _logger;
        public GetStatus(ILogger<GetStatus> logger)
        {
            _logger = logger;
        }
        [Function("GetStatus")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, methods: "get", Route = "status/{instanceId}")] HttpRequestData req,
                                                    [DurableClient] DurableTaskClient orchestrationClient,
                                                    string instanceId,
                                                    ILogger logger)
        {
            _logger.LogInformation($"Getting status for {instanceId}.");

            var status = await orchestrationClient.GetInstanceAsync(instanceId);

            if (status != null)
            {
                try
                {
                    _logger.LogInformation($"Status of {instanceId} is {status.RuntimeStatus}.");

                    if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                    {
                        string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}", req.Url.Scheme, req.Url.Host, instanceId);

                        string message = $"Your submission has been received. To get the status, go to: {checkStatusLocacion}";

                        ActionResult response = new AcceptedResult(checkStatusLocacion, message);

                        return response;
                    }
                    else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    {
                        var test = status.ToString();

                        _logger.LogInformation($"Response: {status}.");

                        return new OkObjectResult(status.SerializedCustomStatus);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting status for {instanceId}.");
                }
            }
            return new NotFoundObjectResult($"Something went wrong. '{instanceId}' not found.");
        }
    }
}