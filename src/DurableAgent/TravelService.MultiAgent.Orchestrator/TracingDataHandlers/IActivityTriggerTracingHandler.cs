using Microsoft.Azure.Functions.Worker;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Contracts;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public interface IActivityTriggerTracingHandler
   {
      Task<TResponse> ExecuteActivityTrigger<TResponse>(Func<RequestData, FunctionContext, Task<TResponse>> func, RequestData requestData, FunctionContext executionContext);
   }
}