using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.DurableOrchestrators;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;
#pragma warning disable OPENAI002

namespace TravelService.MultiAgent.Orchestrator
{
   public class FxTravelService
   {
      private readonly ICosmosClientService _cosmosClientService;
      private readonly INL2SQLService _nL2SQLService;      
      public FxTravelService(ICosmosClientService cosmosClientService, INL2SQLService nL2SQLService)
      {
         _cosmosClientService = cosmosClientService;
         _nL2SQLService = nL2SQLService;         
      }

      [Function("MultiAgentOrchestration")]
      public async Task<IActionResult> MultiAgentOrchestrationTrigger(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
          [DurableClient] DurableTaskClient client,
          FunctionContext executionContext)
      {
         ILogger logger = executionContext.GetLogger("MultiAgentOrchestration");

         string sessionId = req.Headers.GetValues("Session-Id")!.FirstOrDefault();

         string userId = req.Headers.GetValues("User-Id")!.FirstOrDefault();

         if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
         {
            return new BadRequestObjectResult("Session-Id or User-Id header is missing.");
         }

         string userQuery = await new StreamReader(req.Body).ReadToEndAsync();

         var requestData = JsonConvert.DeserializeObject<RequestData>(userQuery);

         if (requestData == null)
         {
            return new BadRequestObjectResult("Invalid request data.");
         }

         var userDetails = await _cosmosClientService.FetchUserDetailsAsync(userId);

         var chatHistory = await _cosmosClientService.FetchChatHistoryAsync(sessionId);

         requestData.ChatHistory = chatHistory;
         requestData.UserId = userId;
         requestData.UserName = userDetails.firstName;
         requestData.UserMailId = userDetails.email;

         string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
             nameof(ManagerOrchestrator), requestData);

         chatHistory.Add("User: " + requestData.UserQuery);

         await _cosmosClientService.StoreChatHistoryAsync(sessionId, requestData.UserQuery, requestData.UserId, requestData.UserName, false);

         var response = await client.CreateCheckStatusResponseAsync(req, instanceId);

         var statusQueryGetUri = response.Headers.GetValues("Location").First();

         string responseBody = await PollForCompletion(statusQueryGetUri, logger);

         return responseBody == "AcceptedResult" ? new AcceptedResult() : new OkObjectResult(responseBody);
      }

      // To mimic Request-Reply pattern in Azure durable functions I'm using polling to check the status of the orchestration
      // There could be a better way to do this, but I'm not aware of it.
      private async Task<string> PollForCompletion(string statusQueryGetUri, ILogger log)
      {
         JObject result = null;

         string content = string.Empty;

         bool isCompleted = false;

         DateTime startTime = DateTime.UtcNow;

         using HttpClient httpClient = new HttpClient();

         while (!isCompleted)
         {
            HttpResponseMessage response = await httpClient.GetAsync(statusQueryGetUri);

            response.EnsureSuccessStatusCode();

            content = await response.Content.ReadAsStringAsync();

            result = JObject.Parse(content);

            string runtimeStatus = result["runtimeStatus"]?.ToString()!;

            isCompleted = (runtimeStatus == "Completed" || runtimeStatus == "Failed");

            if (!isCompleted)
            {
               log.LogInformation($"Orchestration not yet completed. Status: {runtimeStatus}");

               // To Avoid timeout issues after 5 minutes, I have a buffer till 4 minutes,
               // after which I'll return AcceptedResult to handle the response in asynchronous request-reply pattern.

               if (DateTime.UtcNow - startTime > TimeSpan.FromMinutes(4))
               {
                  return "AcceptedResult";
               }

               await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
         }

         return result["output"]?.ToString();
      }

      [Function("ChatRealTimeAssistant")]
      public async Task<IActionResult> ChatRealTimeAssistant(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "realtime")] HttpRequestData req,
          [DurableClient] DurableTaskClient client,
          FunctionContext executionContext)
      {
         ILogger logger = executionContext.GetLogger("ChatRealTimeAssistant");

         string sessionId = req.Headers.GetValues("Session-Id")!.FirstOrDefault();

         string userId = req.Headers.GetValues("User-Id")!.FirstOrDefault();

         if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
         {
            return new BadRequestObjectResult("Session-Id or User-Id header is missing.");
         }

         string assistantType = req.Headers.GetValues("Agent-Type")!.FirstOrDefault();

         string request = await new StreamReader(req.Body).ReadToEndAsync();

         var realtimeRequest = JsonConvert.DeserializeObject<RealtimeRequest>(request);         

         if (realtimeRequest.SessionId == null || realtimeRequest.FunctionCallId == null)
         {
            return new BadRequestObjectResult("Invalid session");
         }

         var userDetails = await _cosmosClientService.FetchUserDetailsAsync(userId);

         var chatHistory = await _cosmosClientService.FetchChatHistoryAsync(sessionId);

         var requestData = new RequestData();

         requestData.ChatHistory = chatHistory;
         requestData.UserId = userId;
         requestData.FunctionCallId = realtimeRequest.FunctionCallId;
         requestData.UserQuery = realtimeRequest.UserQuery;
         requestData.SessionId = sessionId;
         requestData.UserName = userDetails.firstName;
         requestData.UserMailId = userDetails.email;
         requestData.AssistantType = assistantType;

         string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
             nameof(ManagerOrchestrator), requestData);       

         return new AcceptedResult();
      }

      [Function("ChatAssistant")]
      public async Task<IActionResult> ChatAssistant(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ChatAssistant")] HttpRequestData req,
          [DurableClient] DurableTaskClient client,
          FunctionContext executionContext)
      {
         ILogger logger = executionContext.GetLogger("ChatAssistant");

         string sessionId = req.Headers.GetValues("Session-Id")!.FirstOrDefault();

         string userId = req.Headers.GetValues("User-Id")!.FirstOrDefault();

         if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
         {
            return new BadRequestObjectResult("Session-Id or User-Id header is missing.");
         }

         string assistantType = req.Headers.GetValues("Agent-Type")!.FirstOrDefault();

         string userQuery = await new StreamReader(req.Body).ReadToEndAsync();

         var requestData = JsonConvert.DeserializeObject<RequestData>(userQuery);

         if (requestData == null)
         {
            return new BadRequestObjectResult("Invalid request data.");
         }

         var userDetails = await _cosmosClientService.FetchUserDetailsAsync(userId);

         var chatHistory = await _cosmosClientService.FetchChatHistoryAsync(sessionId);

         requestData.ChatHistory = chatHistory;
         requestData.UserId = userId;
         requestData.SessionId = sessionId;
         requestData.UserName = userDetails.firstName;
         requestData.UserMailId = userDetails.email;
         requestData.AssistantType = assistantType;

         string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
             nameof(ManagerOrchestrator), requestData);

         chatHistory.Add("User: " + requestData.UserQuery);

         await _cosmosClientService.StoreChatHistoryAsync(sessionId, requestData.UserQuery, requestData.UserId, requestData.UserName, false);

         return new AcceptedResult();
      }

      [Function("GetChatHistories")]
      public async Task<IActionResult> GetChatHistories(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chat/{sessionId}")] HttpRequestData req,
          string sessionId)
      {
         string userId = req.Headers.GetValues("User-Id")!.FirstOrDefault();

         if (string.IsNullOrEmpty(userId))
         {
            return new BadRequestObjectResult("User-Id header is missing.");
         }

         var chatRecords = await _cosmosClientService.FetchChatHistoriesAsync(sessionId, userId);

         if (chatRecords == null || !chatRecords.Any())
         {
            return new NotFoundResult();
         }

         var firstRecord = chatRecords.First();
         var customerFullName = firstRecord.CustomerName;

         var bookingDetailsResult = new BookingDetailsResult(
             sessionId,
             customerFullName,
             string.Empty,
             chatRecords.Select(cr => new BookingDetailsResultMessage(
                 cr.MessageId,
                 cr.Timestamp,
                 !cr.IsAssistant,
                 cr.Message)).ToList(),
             chatRecords.SelectMany(cr => cr.AgentMessages ?? new List<string>()).ToList()
         );

         return new OkObjectResult(bookingDetailsResult);

      }
      [Function("GetChatHistoryItems")]
      public async Task<IActionResult> GetChatHistorItems(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chat")] HttpRequestData req)
      {
         string userId = req.Headers.GetValues("User-Id")!.FirstOrDefault();

         if (string.IsNullOrEmpty(userId))
         {
            return new BadRequestObjectResult("User-Id header is missing.");
         }

         var chatRecords = await _cosmosClientService.FetchChatSummariesByUserIdAsync(userId);

         if (chatRecords == null || !chatRecords.Any())
         {
            return new NotFoundResult();
         }

         return new OkObjectResult(chatRecords);
      }
   }
}
