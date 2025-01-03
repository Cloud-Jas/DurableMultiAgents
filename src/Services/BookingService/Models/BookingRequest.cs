namespace BookingService.Models
{
   public class BookingRequest
   {
      public string UserId { get; set; }
      public string DepartureCity { get; set; }
      public string DestinationCity { get; set; }
      public string FromDestinationFlightId { get; set; }
      public string FromDestinationFlightPrice { get; set; }
      public string ToDestinationFlightId { get; set; }
      public string ToDestinationFlightPrice { get; set; }

   }
}
