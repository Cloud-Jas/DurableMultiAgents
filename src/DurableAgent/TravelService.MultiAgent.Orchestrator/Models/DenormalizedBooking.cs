using Newtonsoft.Json;
using System;

namespace TravelService.MultiAgent.Orchestrator.Models
{
   public class DenormalizedBooking
   {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("bookingDate")]
      public string BookingDate { get; set; }

      [JsonProperty("status")]
      public string Status { get; set; }

      [JsonProperty("passenger")]
      public PassengerDetails Passenger { get; set; }

      [JsonProperty("fromDestinationTicket")]
      public DenormalizedTicket FromDestinationTicket { get; set; }

      [JsonProperty("toDestinationTicket")]
      public DenormalizedTicket ToDestinationTicket { get; set; }

   }
   public class DenormalizedTicket
   {
      [JsonProperty("seatNumber")]
      public string SeatNumber { get; set; }

      [JsonProperty("flight")]
      public FlightDetails Flight { get; set; }

      [JsonProperty("pricePaid")]
      public string PricePaid { get; set; }
   }
   public class PassengerDetails
   {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("firstName")]
      public string FirstName { get; set; }

      [JsonProperty("lastName")]
      public string LastName { get; set; }

      [JsonProperty("email")]
      public string Email { get; set; }

      [JsonProperty("phone")]
      public string Phone { get; set; }
   }

   public class FlightDetails
   {
      [JsonProperty("flightId")]
      public string FlightId { get; set; }

      [JsonProperty("departureAirportCode")]
      public string DepartureAirportCode { get; set; }

      [JsonProperty("destinationAirportCode")]
      public string DestinationAirportCode { get; set; }

      [JsonProperty("duration")]
      public string Duration { get; set; }

      [JsonProperty("flightNumber")]
      public string FlightNumber { get; set; }

      [JsonProperty("departureTime")]
      public DateTime DepartureTime { get; set; }

      [JsonProperty("price")]
      public decimal Price { get; set; }

      [JsonProperty("availableSeats")]
      public int AvailableSeats { get; set; }

      [JsonProperty("aircraftType")]
      public string AircraftType { get; set; }

      [JsonProperty("airline")]
      public AirlineDetails Airline { get; set; }
   }

   public class AirlineDetails
   {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("name")]
      public string Name { get; set; }

      [JsonProperty("code")]
      public string Code { get; set; }

      [JsonProperty("city")]
      public string City { get; set; }

      [JsonProperty("country")]
      public string Country { get; set; }

      [JsonProperty("logoUrl")]
      public string LogoUrl { get; set; }
   }
}
