namespace BookingService.Interfaces
{
   public interface ICosmosService
   {
      Task InsertBookingAsync(string userId,string departureCity,string destinationCity, string fromDestinationFlightId, string fromDestinationFlightPrice, string toDestinationFlightId, string toDestinationFlightPrice);
   }
}
