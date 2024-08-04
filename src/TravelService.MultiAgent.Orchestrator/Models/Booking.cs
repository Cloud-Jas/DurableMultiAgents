using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class Booking
    {
        public string id { get; set; }
        public string flightId { get; set; }
        public string passengerId { get; set; }
        public string bookingDate { get; set; }
        public string status { get; set; }
        public string seatNumber { get; set; }
        public string pricePaid { get; set; }
        public string paymentId { get; set; }
    }
}
