using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TravelService.MultiAgent.Orchestrator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

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
        public FxBookingChangeFeed(ILoggerFactory loggerFactory, IConfiguration configuration, CosmosClient cosmosClient, AzureOpenAITextEmbeddingGenerationService azureOpenAITextEmbeddingGenerationService)
        {
            _logger = loggerFactory.CreateLogger<FxBookingChangeFeed>();
            databaseId = configuration.GetValue<string>("DatabaseId");
            client = cosmosClient;
            _azureOpenAITextEmbeddingGenerationService = azureOpenAITextEmbeddingGenerationService;
        }

        private static readonly string EndpointUri = Environment.GetEnvironmentVariable("CosmosDBEndpointUri");

        [Function("ProcessBookingChanges")]
        public async Task Run(
        [CosmosDBTrigger(
            databaseName: "ContosoTravelAgency",
            containerName: "Bookings",
            Connection = "cosmosDB",
            LeaseContainerName = "leases")] IReadOnlyList<Booking> bookings,
            ILogger log)
        {
            if (bookings != null && bookings.Count > 0)
            {
                foreach (var booking in bookings)
                {
                    var passengerContainer = client.GetContainer(databaseId, "Passengers");
                    var passenger = await passengerContainer.ReadItemAsync<Passenger>(
                        booking.passengerId, new PartitionKey(booking.passengerId));

                    var flightsContainer = client.GetContainer(databaseId, "FlightListings");
                    var flightQuery = new QueryDefinition("SELECT * FROM c WHERE c.flightNumber = @flightNumber")
                        .WithParameter("@flightNumber", booking.flightId);

                    var flightIterator = flightsContainer.GetItemQueryIterator<FlightListing>(flightQuery);
                    FlightListing flight = null;
                    if (flightIterator.HasMoreResults)
                    {
                        var flightResult = await flightIterator.ReadNextAsync();
                        flight = flightResult.FirstOrDefault();
                    }

                    if (flight == null)
                    {
                        continue;
                    }

                    var airlinesContainer = client.GetContainer(databaseId, "Airlines");
                    var airline = await airlinesContainer.ReadItemAsync<Airline>(
                        flight.airlineId, new PartitionKey(flight.airlineId));

                    var denormalizedBooking = new
                    {
                        booking.id,
                        booking.bookingDate,
                        booking.status,
                        booking.seatNumber,
                        booking.pricePaid,
                        passenger = new
                        {
                            passenger.Resource.id,
                            passenger.Resource.firstName,
                            passenger.Resource.lastName,
                            passenger.Resource.email,
                            passenger.Resource.phone
                        },
                        flight = new
                        {
                            flight.id,
                            flight.flightNumber,
                            flight.departure,
                            flight.destination,
                            flight.departureTime,
                            flight.price,
                            flight.aircraftType,
                            flight.availableSeats,
                            flight.duration,
                            airline = new
                            {
                                airline.Resource.id,
                                airline.Resource.name,
                                airline.Resource.code,
                                airline.Resource.city,
                                airline.Resource.country,
                                airline.Resource.logoUrl
                            }
                        }
                    };

                    var semanticBookingContainer = client.GetContainer(databaseId, "SemanticBookingLayer");
                    await semanticBookingContainer.UpsertItemAsync(denormalizedBooking, new PartitionKey(denormalizedBooking.id));

                    var semanticBookingVectorContainer = client.GetContainer(databaseId, "SemanticBookingVectorLayer");
                    var bookingVector = new
                    {
                        booking.id,
                        metadata = JsonConvert.SerializeObject(denormalizedBooking),
                        vector = (await _azureOpenAITextEmbeddingGenerationService.GenerateEmbeddingAsync(JsonConvert.SerializeObject(denormalizedBooking))).ToArray()
                    };

                    await semanticBookingVectorContainer.UpsertItemAsync(bookingVector, new PartitionKey(bookingVector.id));
                }
            }
        }
    }
    public class Airline
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string logoUrl { get; set; }
    }

}
