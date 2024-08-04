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

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
