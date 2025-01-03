
using OpenTelemetry.Trace;
using System.Security.Policy;

namespace TravelService.MultiAgent.Orchestrator.Contracts
{
   public class RequestData
   {
      public RequestData()
      {
      }
      public string UserQuery { get; set; }
      public string UserName { get; set; }
      public string UserMailId { get; set; }
      public List<string> ChatHistory { get; set; }
      public string SessionId { get; set; }
      public string UserId { get; set; }
      public string? AssistantType { get; set; }
      public string? FunctionCallId { get; set; }
      public string IntermediateResponse { get; set; }
      public Dictionary<string, object> OrchestratorTracingCache { get; set; } = new Dictionary<string, object>();
      public Dictionary<string, object> SubOrchestratorTracingCache { get; set; } = new Dictionary<string, object>();
      public Dictionary<string, object> ParentTracingCache { get; set; } = new Dictionary<string, object>();      
      public HashSet<string> InstanceIds { get; set; } = new HashSet<string>();
      public void Set(RequestData? requestData)
      {
         this.SessionId = requestData.SessionId;
         this.UserId = requestData.UserId;
         this.UserQuery = requestData.UserQuery;
         this.UserName = requestData.UserName;
         this.ChatHistory = requestData.ChatHistory;
      }
   }
}
