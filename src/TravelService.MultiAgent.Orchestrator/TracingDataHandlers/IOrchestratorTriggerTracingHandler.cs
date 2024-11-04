using Microsoft.DurableTask;
using TravelService.MultiAgent.Orchestrator.Contracts;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public interface IOrchestratorTriggerTracingHandler
   {
      Task<string> PopulateRootOrchestratorTracingData(Func<TaskOrchestrationContext, RequestData, Task<string?>> func, TaskOrchestrationContext context, RequestData requestData);
      Task<RequestData> PopulateSubOrchestratorTracingData(Func<TaskOrchestrationContext, RequestData, Task<RequestData>> func, TaskOrchestrationContext context, RequestData requestData);
   }
}