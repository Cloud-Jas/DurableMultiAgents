using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Contracts
{
    public class Agent
    {
        public string AgentName { get; set; }
        public string AgentDescription { get; set; }
        public string TerminationStrategy { get; set; }

    }
}
