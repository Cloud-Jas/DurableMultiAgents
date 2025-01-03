using Microsoft.Azure.Functions.Worker;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Contracts;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public interface IPluginTracingHandler
   {
      Task<string> ExecutePlugin(Func<Dictionary<string,string>, Task<string>> func, Dictionary<string,string> parameters);
   }
}