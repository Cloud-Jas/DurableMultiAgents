using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class Passenger
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PassportNumber { get; set; }
        public string Nationality { get; set; }
        public string Dob { get; set; }
        public string FrequentFlyerNumber { get; set; }
    }
}
