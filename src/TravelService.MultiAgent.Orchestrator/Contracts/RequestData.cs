
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
