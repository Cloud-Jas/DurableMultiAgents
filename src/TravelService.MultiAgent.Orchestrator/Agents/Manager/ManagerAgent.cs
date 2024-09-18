using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
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

        public ManagerAgent(ILogger<ManagerAgent> logger, Kernel kernel, IPromptyService prompty, IKernelService kernelService, ICosmosClientService cosmosClientService)
        {
            _logger = logger;
            _kernel = kernel;
            _prompty = prompty;
            _kernelService = kernelService;
            _cosmosClientService = cosmosClientService;
        }

        [Function(nameof(RouteOrchestrators))]
        public async Task<string> RouteOrchestrators([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
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
        }
        [Function(nameof(TriggerConsolidaterAgent))]
        public async Task<string> TriggerConsolidaterAgent([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
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
        }
        [Function(nameof(RouteAgents))]
        public async Task<string> RouteAgents([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
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
        }
        [Function(nameof(RouteDefaultAgents))]
        public async Task<string> RouteDefaultAgents([ActivityTrigger] RequestData requestData, FunctionContext executionContext)
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
        }
    }
}

