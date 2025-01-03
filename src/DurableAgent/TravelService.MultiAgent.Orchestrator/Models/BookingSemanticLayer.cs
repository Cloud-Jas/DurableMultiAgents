using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
   public class Airlines
   {
      public List<Item> items { get; set; }
   }

   public class Data
   {
      public Passengers passengers { get; set; }
      public FlightListings flightListings { get; set; }
   }

   public class FlightListings
   {
      public List<Item> items { get; set; }
   }

   public class Item
   {
      public string Id { get; set; }
      public string FirstName { get; set; }
      public string LastName { get; set; }
      public string Email { get; set; }
      public string Phone { get; set; }
      public string FlightId { get; set; }
      public string DepartureAirportCode { get; set; }
      public string DestinationAirportCode { get; set; }
      public string Duration { get; set; }
      public string FlightNumber { get; set; }
      public DateTime DepartureTime { get; set; }
      public double Price { get; set; }
      public int AvailableSeats { get; set; }
      public string AircraftType { get; set; }
      public Airlines airlines { get; set; }
      public string AirlineId { get; set; }
      public string Name { get; set; }
      public string Code { get; set; }
      public string Country { get; set; }
      public string City { get; set; }
      public string LogoUrl { get; set; }
   }

   public class Passengers
   {
      public List<Item> items { get; set; }
   }

   public class BookingSemanticLayer
   {
      public Data data { get; set; }
   }
}
