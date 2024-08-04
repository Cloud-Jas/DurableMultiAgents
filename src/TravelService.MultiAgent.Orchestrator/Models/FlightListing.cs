using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class FlightListing
    {
        public string id { get; set; }
        public string flightNumber { get; set; }
        public string airlineCode { get; set; }
        public string departure { get; set; }
        public string destination { get; set; }
        public string departureTime { get; set; }
        public string price { get; set; }
        public string description { get; set; }
        public string airlineId { get; set; }
        public string aircraftType { get; set; }
        public int availableSeats { get; set; }
        public string duration { get; set; }
    }
}
