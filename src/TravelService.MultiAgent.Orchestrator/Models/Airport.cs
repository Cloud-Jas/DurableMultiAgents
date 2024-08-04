using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class Airport
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string timezone { get; set; }
    }

}
