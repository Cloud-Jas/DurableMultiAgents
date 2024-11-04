using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

namespace TravelService.MultiAgent.Orchestrator.Agents.SemanticLayer.Plugins
{

   public class SemanticLayerPlugin
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly IPluginTracingHandler _pluginTracingHandler;
      public SemanticLayerPlugin(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         _pluginTracingHandler = serviceProvider.GetService<IPluginTracingHandler>() ?? throw new ArgumentNullException(nameof(IPluginTracingHandler));
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

         var parameters = new Dictionary<string, string>
         {
            {"pluginName","NL2SQLRetreivalPlugin" },
            { "containerId", containerId },
            { "userId", userId },
            { "prompt", prompt }
         };

         Func<Dictionary<string, string>, Task<string>> callSemanticAgent = async (inputs) =>
         {
            var cId = inputs["containerId"];
            var uId = inputs["userId"];
            var prmt = inputs["prompt"];

            try
            {
               var nl2SqlService = _serviceProvider.GetRequiredService<INL2SQLService>();
               var cosmosClientService = _serviceProvider.GetRequiredService<ICosmosClientService>();
               var sql = await nl2SqlService.GetSQLQueryAsync(prmt, cId);
               var response = await cosmosClientService.FetchDetailsFromSemanticLayer(sql, cId);
               return response.Count > 0 ? JsonConvert.SerializeObject(response) : "No information found!";
            }
            catch (Exception ex)
            {
               return "Error while looking for results!";
            }
         };

         return await _pluginTracingHandler.ExecutePlugin(callSemanticAgent,parameters);
      }
   }
}
