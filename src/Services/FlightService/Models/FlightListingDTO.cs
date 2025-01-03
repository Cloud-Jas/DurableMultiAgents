using System.ComponentModel.DataAnnotations;

namespace FlightService.Models
{
   public class FlightListingDTO
   {
      public string Id { get; set; }
      public string FlightNumber { get; set; }
      public string AirlineId { get; set; }
      public string DepartureAirportCode { get; set; }
      public string DestinationAirportCode { get; set; }
      public DateTime DepartureTime { get; set; }
      public decimal Price { get; set; }
      public string Description { get; set; }
      public string AircraftType { get; set; }
      public int AvailableSeats { get; set; }
      public string Duration { get; set; }
   }

}
