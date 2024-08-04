using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.Agents.SemanticLayer.Plugins
{

    public class SemanticLayerPlugin
    {
        private readonly IServiceProvider _serviceProvider;
        public SemanticLayerPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction("Generate_Copilot_prompt")]
        [Description("Create copilot prompt for the given context")]
        public async Task<string> GetSQLFromSemanticLayer(
            [Description("Name of the Container")]
            string containerId,
            [Description("Id of the User")]
            string userId,
            [Description("Query prompt")]
            string prompt
            )
        {
            try
            {
                var nl2SqlService = _serviceProvider.GetRequiredService<INL2SQLService>();
                var cosmosClientService = _serviceProvider.GetRequiredService<ICosmosClientService>();
                var sql = await nl2SqlService.GetSQLQueryAsync(prompt, containerId);
                var response = await cosmosClientService.FetchDetailsFromSemanticLayer(sql, containerId);
                return response.Count > 0 ? JsonConvert.SerializeObject(response) : "No information found!";
            }
            catch(Exception ex)
            {
                return "Error while looking for results!";
            }
        }
    }
}
