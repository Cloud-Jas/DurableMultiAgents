using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
   public class ChatRecord
   {
      [JsonProperty("id")]
      public string MessageId { get; set; }
      [JsonProperty("customerName")]
      public string CustomerName { get; set; }
      [JsonProperty("customerId")]
      public string CustomerId { get; set; }
      [JsonProperty("isAssistant")]
      public bool IsAssistant { get; set; }
      [JsonProperty("sessionId")]
      public string SessionId { get; set; }
      [JsonProperty("message")]
      public string Message { get; set; }
      [JsonProperty("timestamp")]
      public DateTime Timestamp { get; set; }
   }
   public record BookingDetailsResult(string SessionId, string CustomerFullName, string? LongSummary, ICollection<BookingDetailsResultMessage> Messages);

   public record BookingDetailsResultMessage(string MessageId, DateTime CreatedAt, bool IsCustomerMessage, string MessageText);
   public class SessionSummary
   {
      public string SessionId { get; set; }
      public string LastMessage { get; set; }
      public DateTime LastMessageTimestamp { get; set; }
   }
}
