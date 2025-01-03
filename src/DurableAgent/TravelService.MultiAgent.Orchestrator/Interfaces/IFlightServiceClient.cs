using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IFlightServiceClient
   {
      Task<Airline> GetAirlineDetailsAsync(string airlineId);
      Task<List<Airport>?> GetAirportsAsync(string departureCity, string destinationCity);
      Task<List<FlightListing>?> GetFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate);
      Task<FlightListing?> GetFlightListingsAsync(string flightId);
   }
}
