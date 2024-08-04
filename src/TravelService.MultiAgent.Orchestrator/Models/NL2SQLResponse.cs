using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class NL2SQLResponse
    {
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        [JsonProperty("sql")]
        public string Sql { get; set; }

        [JsonProperty("explanation")]
        public string Explanation { get; set; }

        [JsonProperty("generateStart")]
        public DateTime GenerateStart { get; set; }

        [JsonProperty("generateEnd")]
        public DateTime GenerateEnd { get; set; }
    }
}
