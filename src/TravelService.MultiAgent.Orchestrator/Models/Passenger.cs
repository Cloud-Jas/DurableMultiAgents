using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class Passenger
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string passportNumber { get; set; }
        public string nationality { get; set; }
        public string dob { get; set; }
        public string frequentFlyerNumber { get; set; }
    }
}
