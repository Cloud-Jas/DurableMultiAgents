using FlightService.Models;

namespace FlightService.Interfaces
{
   public interface IFlightServiceRepository
   {
      Task<Airline?> FetchAirlineDetailsAsync(string airlineId);
      Task<List<Airport>> FetchAirportDetailsAsync(string departureCity, string destinationCity);
      Task<FlightListing?> FetchFlightListingAsync(string flightNumber);
      Task<List<FlightListing>> FetchFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate);
   }
}