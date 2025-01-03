using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class PhoenixServiceInfo
    {
        [JsonPropertyName("phoenixServiceUrl")]
        public string PhoenixServiceUrl { get; set; }

        [JsonPropertyName("authToken")]
        public string AuthToken { get; set; }

        [JsonPropertyName("forwardingId")]
        public string ForwardingId { get; set; }
    }

    public class ServiceResponse
    {
        [JsonPropertyName("phoenixServiceInfo")]
        public PhoenixServiceInfo PhoenixServiceInfo { get; set; }

        [JsonPropertyName("allocationTime")]
        public DateTime AllocationTime { get; set; }
    }
}
