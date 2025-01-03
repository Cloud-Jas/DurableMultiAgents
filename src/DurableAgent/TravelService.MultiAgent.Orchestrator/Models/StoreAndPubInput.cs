using Microsoft.DurableTask;
using TravelService.MultiAgent.Orchestrator.Contracts;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class StoreAndPubInput
    {
        public RequestData RequestData { get; set; }
        public List<string> AgentChatHistory { get; set; }
        public string Response { get; set; }
    }
}