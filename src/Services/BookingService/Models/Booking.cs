using Newtonsoft.Json;

namespace BookingService.Models
{
   public class Booking
   {
      [JsonProperty("id")]
      public string Id { get; set; }
      [JsonProperty("departureCity")]
      public string DepartureCity { get; set; }
      [JsonProperty("destinationCity")]
      public string DestinationCity { get; set; }
      [JsonProperty("passengerId")]
      public string PassengerId { get; set; }
      [JsonProperty("bookingDate")]
      public string BookingDate { get; set; }
      [JsonProperty("status")]
      public string Status { get; set; }
      [JsonProperty("fromDestinationTicket")]
      public Ticket FromDestinationTicket { get; set; }
      [JsonProperty("toDestinationTicket")]
      public Ticket ToDestinationTicket { get; set; }
   }
   public class Ticket
   {
      [JsonProperty("seatNumber")]
      public string SeatNumber { get; set; }

      [JsonProperty("flightId")]
      public string FlightId { get; set; }

      [JsonProperty("pricePaid")]
      public string PricePaid { get; set; }
   }
}
