using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Newtonsoft.Json;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

namespace TravelService.MultiAgent.Orchestrator.Agents.SemanticLayer.Plugins
{

   public class VectorSearchPlugin
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly IPluginTracingHandler _pluginTracingHandler;
      public VectorSearchPlugin(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         _pluginTracingHandler = serviceProvider.GetService<IPluginTracingHandler>() ?? throw new ArgumentNullException(nameof(IPluginTracingHandler));
      }

      [KernelFunction("SimilaritySearchAsync")]
      [Description("Search for similarities based on the user query")]
      public async Task<string> SimilaritySearchAsync(
         [Description("Query prompt")]
            string prompt,
         [Description("Container Id")]
           string containerId
         )
      {

         var parameters = new Dictionary<string, string>
         {
            {"pluginName", "VectorSimilaritySearchPlugin" },
            { "containerId", containerId },
            { "prompt", prompt }
         };

         Func<Dictionary<string, string>, Task<string>> callSimilaritySearchAgent = async (inputs) =>
         {

            var prompt = inputs["prompt"];
            var containerId = inputs["containerId"];

            try
            {
               var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

               var embeddingService = _serviceProvider.GetRequiredService<AzureOpenAITextEmbeddingGenerationService>();

               var embeddingQuery = await embeddingService.GenerateEmbeddingAsync(prompt);

               var response = await cosmosService.FetchDetailsFromVectorSemanticLayer(embeddingQuery, containerId);

               return response.Count > 0 ? JsonConvert.SerializeObject(response) : "No information found!";
            }
            catch (Exception ex)
            {
               return "Error retreiveing infomration";
            }
         };

         return await _pluginTracingHandler.ExecutePlugin(callSimilaritySearchAgent, parameters);
      }
   }
}
