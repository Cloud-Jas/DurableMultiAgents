using System.ComponentModel.DataAnnotations;

namespace FlightService.Models
{
   public class FlightListing
   {
      [Key]
      public string FlightId { get; set; }
      public string FlightNumber { get; set; }
      public string AirlineId { get; set; }
      public string DepartureAirportCode { get; set; }
      public string DestinationAirportCode { get; set; }
      public string DepartureTime { get; set; }
      public decimal Price { get; set; }
      public string Description { get; set; }
      public string AircraftType { get; set; }
      public int AvailableSeats { get; set; }
      public string Duration { get; set; }
   }
}
