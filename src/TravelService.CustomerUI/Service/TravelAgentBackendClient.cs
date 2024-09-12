using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static TravelService.CustomerUI.Components.Layout.MainLayout;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TravelService.CustomerUI.Clients.Backend;

public class TravelAgentBackendClient(HttpClient http)
{
   public async Task<string> TriggerMultiAgentOrchestrationAsync(string request, string sessionId, string userId)
   {
      var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ChatAssistant")
      {
         Content = new StringContent(JsonSerializer.Serialize(new { userQuery = request }), Encoding.UTF8, "application/json")
      };
      httpRequest.Headers.Add("Session-Id", sessionId);
      httpRequest.Headers.Add("User-Id", userId);

      var response = await http.SendAsync(httpRequest);
      return await response.Content.ReadAsStringAsync();
   }

   public async Task<BookingDetailsResult> GetBookingMessagesAsync(string sessionId,string userId)
   {
      var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/chat/{sessionId}")
      {
         Headers =
               {
                   { "User-Id", userId }
               }
      };

      var response = await http.SendAsync(httpRequest);
      var json = await response.Content.ReadAsStringAsync();
      var result = JsonConvert.DeserializeObject<BookingDetailsResult>(Convert.ToString((JObject.Parse(json))["Value"]));
      return result;
   }

   public async Task<List<SessionSummary>> GetBookingMessagesByUserIdAsync(string userId)
   {
      var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/chat")
      {
         Headers =
               {
                   { "User-Id", userId }
               }
      };

      var response = await http.SendAsync(httpRequest);
      var json = await response.Content.ReadAsStringAsync();
      var result = JsonConvert.DeserializeObject<List<SessionSummary>>(Convert.ToString((JObject.Parse(json))["Value"]));
      return result;
   }
}

public record BookingDetailsResult(string SessionId, string CustomerFullName, string? LongSummary, ICollection<BookingDetailsResultMessage> Messages);

public record BookingDetailsResultMessage(string MessageId, DateTime CreatedAt, bool IsCustomerMessage, string MessageText);
