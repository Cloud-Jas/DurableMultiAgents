using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TravelService.MultiAgent.Orchestrator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Google.Apis.Auth.OAuth2;
using GraphQL.Client.Http;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using TravelService.MultiAgent.Orchestrator.Interfaces;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
namespace TravelService.MultiAgent.Orchestrator
{
   public class FxBookingChangeFeed
   {
      private readonly ILogger _logger;
      private readonly string databaseId;
      private readonly CosmosClient client;
      private readonly AzureOpenAITextEmbeddingGenerationService _azureOpenAITextEmbeddingGenerationService;
      private readonly string FabricClientId;
      private readonly IFabricGraphQLService _fabricGraphQLService;
      private readonly IUserServiceClient userServiceClient;
      private readonly IFlightServiceClient flightServiceClient;

      public FxBookingChangeFeed(ILoggerFactory loggerFactory, IConfiguration configuration
         , CosmosClient cosmosClient, AzureOpenAITextEmbeddingGenerationService azureOpenAITextEmbeddingGenerationService
         , IFabricGraphQLService fabricGraphQLService, IUserServiceClient userServiceClient, IFlightServiceClient flightServiceClient)
      {
         _logger = loggerFactory.CreateLogger<FxBookingChangeFeed>();
         databaseId = configuration.GetValue<string>("DatabaseId");
         client = cosmosClient;
         _azureOpenAITextEmbeddingGenerationService = azureOpenAITextEmbeddingGenerationService;
         FabricClientId = Environment.GetEnvironmentVariable("Fabric__ClientId");
         _fabricGraphQLService = fabricGraphQLService;
         this.userServiceClient = userServiceClient;
         this.flightServiceClient = flightServiceClient;
      }

      private static readonly string EndpointUri = Environment.GetEnvironmentVariable("CosmosDBEndpointUri");

      [Function("ProcessBookingChanges")]
      public async Task Run(
      [CosmosDBTrigger(
            databaseName: "BookingService",
            containerName: "Bookings",
            Connection = "cosmosDB",
            LeaseContainerName = "leases")] IReadOnlyList<Booking> bookings,
          ILogger log)
      {
         if (bookings != null && bookings.Count > 0)
         {
            try
            {
               foreach (var booking in bookings)
               {
                  var denormalizedBooking = (!string.IsNullOrWhiteSpace(FabricClientId)) ?
                     await DenormalizedBookingDataUsingFabricGraphQL(booking) : await DenormalizedBookingDataUsingAggregator(booking);

                  if (denormalizedBooking != null)
                  {
                     var semanticBookingContainer = client.GetContainer(databaseId, "SemanticBookingLayer");
                     await semanticBookingContainer.UpsertItemAsync(denormalizedBooking, new PartitionKey(denormalizedBooking.Id));

                     var semanticBookingVectorContainer = client.GetContainer(databaseId, "SemanticBookingVectorLayer");
                     var bookingVector = new
                     {
                        id = booking.Id,
                        metadata = JsonConvert.SerializeObject(denormalizedBooking),
                        vector = (await _azureOpenAITextEmbeddingGenerationService.GenerateEmbeddingAsync(JsonConvert.SerializeObject(denormalizedBooking))).ToArray()
                     };

                     await semanticBookingVectorContainer.UpsertItemAsync(bookingVector, new PartitionKey(bookingVector.id));
                  }
               }
            }
            catch (Exception ex)
            {
               _logger.LogError($"Error processing booking changes: {ex.Message}");
            }
         }
      }

      private async Task<DenormalizedBooking> DenormalizedBookingDataUsingAggregator(Booking booking)
      {
         try
         {
            var passenger = await userServiceClient.GetPassengerByIdAsync(booking.PassengerId);

            var denormalizedBooking = new DenormalizedBooking
            {
               Id = booking.Id,
               BookingDate = booking.BookingDate,
               Status = booking.Status,
               Passenger = new PassengerDetails
               {
                  Id = passenger.Id,
                  FirstName = passenger.FirstName,
                  LastName = passenger.LastName,
                  Email = passenger.Email,
                  Phone = passenger.Phone
               }
            };

            if (booking.ToDestinationTicket != null)
            {
               var toDestinationFlight = await flightServiceClient.GetFlightListingsAsync(booking.ToDestinationTicket.FlightId);
               var toDestinationAirline = await flightServiceClient.GetAirlineDetailsAsync(toDestinationFlight.AirlineId);
               denormalizedBooking.ToDestinationTicket = new DenormalizedTicket
               {
                  SeatNumber = booking.ToDestinationTicket.SeatNumber,
                  PricePaid = booking.ToDestinationTicket.PricePaid,
                  Flight = new FlightDetails
                  {
                     FlightId = booking.ToDestinationTicket.FlightId,
                     DepartureAirportCode = toDestinationFlight.DepartureAirportCode,
                     DestinationAirportCode = toDestinationFlight.DestinationAirportCode,
                     Duration = toDestinationFlight.Duration,
                     FlightNumber = toDestinationFlight.FlightNumber,
                     DepartureTime = toDestinationFlight.DepartureTime,
                     Price = toDestinationFlight.Price,
                     AvailableSeats = toDestinationFlight.AvailableSeats,
                     AircraftType = toDestinationFlight.AircraftType,
                     Airline = new AirlineDetails
                     {
                        Id = toDestinationFlight.AirlineId,
                        Name = toDestinationAirline.Name,
                        Code = toDestinationAirline.Code,
                        City = toDestinationAirline.City,
                        Country = toDestinationAirline.Country,
                        LogoUrl = toDestinationAirline.LogoUrl
                     }
                  }
               };
            }
            if (booking.FromDestinationTicket != null)
            {
               var fromDestinationFlight = await flightServiceClient.GetFlightListingsAsync(booking.FromDestinationTicket.FlightId);
               var fromDestinationAirline = await flightServiceClient.GetAirlineDetailsAsync(fromDestinationFlight.AirlineId);
               denormalizedBooking.FromDestinationTicket = new DenormalizedTicket
               {
                  SeatNumber = booking.FromDestinationTicket.SeatNumber,
                  PricePaid = booking.FromDestinationTicket.PricePaid,
                  Flight = new FlightDetails
                  {
                     FlightId = booking.FromDestinationTicket.FlightId,
                     DepartureAirportCode = fromDestinationFlight.DepartureAirportCode,
                     DestinationAirportCode = fromDestinationFlight.DestinationAirportCode,
                     Duration = fromDestinationFlight.Duration,
                     FlightNumber = fromDestinationFlight.FlightNumber,
                     DepartureTime = fromDestinationFlight.DepartureTime,
                     Price = fromDestinationFlight.Price,
                     AvailableSeats = fromDestinationFlight.AvailableSeats,
                     AircraftType = fromDestinationFlight.AircraftType,
                     Airline = new AirlineDetails
                     {
                        Id = fromDestinationFlight.AirlineId,
                        Name = fromDestinationAirline.Name,
                        Code = fromDestinationAirline.Code,
                        City = fromDestinationAirline.City,
                        Country = fromDestinationAirline.Country,
                        LogoUrl = fromDestinationAirline.LogoUrl
                     }
                  }
               };
            }
            return denormalizedBooking;
         }
         catch (Exception ex)
         {
            _logger.LogError($"Error denormalizing booking data: {ex.Message}");
            return default;
         }
      }

      private async Task<DenormalizedBooking> DenormalizedBookingDataUsingFabricGraphQL(Booking booking)
      {
         try
         {
            var passenger = new Passenger();
            var flight = new FlightListing();
            var airline = new Airline();

            var denormalizedBooking = new DenormalizedBooking
            {
               Id = booking.Id,
               BookingDate = booking.BookingDate,
               Status = booking.Status
            };

            if (booking.ToDestinationTicket != null)
            {
               var bookingSemanticLayer = await _fabricGraphQLService.FetchBookingDetailsAsync(booking.PassengerId, booking.ToDestinationTicket.FlightId);

               var parsedResponse = JObject.Parse(JsonConvert.SerializeObject(bookingSemanticLayer));

               if (parsedResponse["passengers"]["items"].HasValues)
               {
                  passenger = parsedResponse["passengers"]["items"][0].ToObject<Passenger>();
               }

               if (parsedResponse["flightListings"]["items"].HasValues)
               {
                  flight = parsedResponse["flightListings"]["items"][0].ToObject<FlightListing>();
                  airline = parsedResponse["flightListings"]["items"][0]["airlines"]["items"][0].ToObject<Airline>();
               }

               denormalizedBooking.Passenger = new PassengerDetails
               {
                  Id = passenger.Id,
                  FirstName = passenger.FirstName,
                  LastName = passenger.LastName,
                  Email = passenger.Email,
                  Phone = passenger.Phone
               };

               denormalizedBooking.ToDestinationTicket = new DenormalizedTicket
               {
                  SeatNumber = booking.ToDestinationTicket.SeatNumber,
                  PricePaid = booking.ToDestinationTicket.PricePaid,
                  Flight = new FlightDetails
                  {
                     FlightId = booking.ToDestinationTicket.FlightId,
                     DepartureAirportCode = flight.DepartureAirportCode,
                     DestinationAirportCode = flight.DestinationAirportCode,
                     Duration = flight.Duration,
                     FlightNumber = flight.FlightNumber,
                     DepartureTime = flight.DepartureTime,
                     Price = flight.Price,
                     AvailableSeats = flight.AvailableSeats,
                     AircraftType = flight.AircraftType,
                     Airline = new AirlineDetails
                     {
                        Id = flight.AirlineId,
                        Name = airline.Name,
                        Code = airline.Code,
                        City = airline.City,
                        Country = airline.Country,
                        LogoUrl = airline.LogoUrl
                     }
                  }
               };
            }

            if (booking.FromDestinationTicket != null)
            {
               var bookingSemanticLayer = await _fabricGraphQLService.FetchBookingDetailsAsync(booking.PassengerId, booking.FromDestinationTicket.FlightId);

               var parsedResponse = JObject.Parse(JsonConvert.SerializeObject(bookingSemanticLayer));

               if (parsedResponse["passengers"]["items"].HasValues)
               {
                  passenger = parsedResponse["passengers"]["items"][0].ToObject<Passenger>();
               }

               denormalizedBooking.Passenger = new PassengerDetails
               {
                  Id = passenger.Id,
                  FirstName = passenger.FirstName,
                  LastName = passenger.LastName,
                  Email = passenger.Email,
                  Phone = passenger.Phone
               };

               if (parsedResponse["flightListings"]["items"].HasValues)
               {
                  flight = parsedResponse["flightListings"]["items"][0].ToObject<FlightListing>();
                  airline = parsedResponse["flightListings"]["items"][0]["airlines"]["items"][0].ToObject<Airline>();
               }

               denormalizedBooking.FromDestinationTicket = new DenormalizedTicket
               {
                  SeatNumber = booking.FromDestinationTicket.SeatNumber,
                  PricePaid = booking.FromDestinationTicket.PricePaid,
                  Flight = new FlightDetails
                  {
                     FlightId = booking.FromDestinationTicket.FlightId,
                     DepartureAirportCode = flight.DepartureAirportCode,
                     DestinationAirportCode = flight.DestinationAirportCode,
                     Duration = flight.Duration,
                     FlightNumber = flight.FlightNumber,
                     DepartureTime = flight.DepartureTime,
                     Price = flight.Price,
                     AvailableSeats = flight.AvailableSeats,
                     AircraftType = flight.AircraftType,
                     Airline = new AirlineDetails
                     {
                        Id = flight.AirlineId,
                        Name = airline.Name,
                        Code = airline.Code,
                        City = airline.City,
                        Country = airline.Country,
                        LogoUrl = airline.LogoUrl
                     }
                  }
               };
            }

            return denormalizedBooking;
         }
         catch (Exception ex)
         {
            _logger.LogError($"Error denormalizing booking data: {ex.Message}");
            return default;
         }
      }
   }
}
