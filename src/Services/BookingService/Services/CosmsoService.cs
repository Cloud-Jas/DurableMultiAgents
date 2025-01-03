using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;
using BookingService.Models;
using BookingService.Interfaces;

namespace BookingService.Services
{
   public class CosmosService : ICosmosService
   {
      private readonly CosmosClient _cosmosClient;
      private readonly QueryRequestOptions _queryOptions;
      private Container _container;
      private readonly string databaseId;

      public CosmosService(CosmosClient cosmosClient, IConfiguration configuration)
      {
         _cosmosClient = cosmosClient;
         databaseId = configuration!.GetValue<string>("DatabaseId")!;
         _queryOptions = new QueryRequestOptions
         {
            MaxItemCount = -1,
            MaxConcurrency = -1
         };
      }

      public async Task InsertBookingAsync(string userId, string departureCity, string destinationCity, string fromDestinationFlightId, string fromDestinationFlightPrice, string toDestinationFlightId, string toDestinationFlightPrice)
      {
         _container = _cosmosClient.GetContainer(databaseId, "Bookings");

         var booking = new Booking
         {
            Id = Guid.NewGuid().ToString(),
            PassengerId = userId,
            DepartureCity = departureCity,
            DestinationCity = destinationCity,
            BookingDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Status = "Confirmed",
         };

         if (!string.IsNullOrWhiteSpace(fromDestinationFlightId))
         {
            booking.FromDestinationTicket = new Ticket
            {
               SeatNumber = "1A",
               FlightId = fromDestinationFlightId,
               PricePaid = fromDestinationFlightPrice
            };
         }

         if (!string.IsNullOrWhiteSpace(toDestinationFlightId))
         {
            booking.ToDestinationTicket = new Ticket
            {
               SeatNumber = "1B",
               FlightId = toDestinationFlightId,
               PricePaid = toDestinationFlightPrice
            };
         }

         await _container.CreateItemAsync(booking);
      }
   }
}
