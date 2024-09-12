using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class CosmosClientService : ICosmosClientService
   {
      private readonly CosmosClient _cosmosClient;
      private Container _container;
      private readonly string databaseId;
      private readonly QueryRequestOptions _queryOptions;

      public CosmosClientService(CosmosClient cosmosClient, IConfiguration configuration)
      {
         _cosmosClient = cosmosClient;
         databaseId = configuration.GetValue<string>("DatabaseId");
         _queryOptions = new QueryRequestOptions
         {
            MaxItemCount = -1,
            MaxConcurrency = -1
         };
      }

      public async Task InsertBookingAsync(string userId, string flightId, string flightPrice)
      {
         _container = _cosmosClient.GetContainer(databaseId, "Bookings");

         Booking booking = new Booking
         {
            id = Guid.NewGuid().ToString(),
            passengerId = userId,
            bookingDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            status = "Confirmed",
            seatNumber = "A1",
            flightId = flightId,
            pricePaid = flightPrice
         };

         await _container.CreateItemAsync(booking);
      }

      public async Task<List<dynamic>> FetchDetailsFromSemanticLayer(string queryPrompt, string containerId)
      {
         _container = _cosmosClient.GetContainer(databaseId, containerId);

         var query = new QueryDefinition(queryPrompt);

         var results = new List<dynamic>();

         using (var resultSetIterator = _container.GetItemQueryIterator<dynamic>(query, requestOptions: _queryOptions))
         {
            while (resultSetIterator.HasMoreResults)
            {
               var response = await resultSetIterator.ReadNextAsync();
               results.AddRange(response);
            }
         }

         return results;
      }

      public async Task<List<dynamic>> FetchDetailsFromVectorSemanticLayer(ReadOnlyMemory<float> embedding, string containerId)
      {
         _container = _cosmosClient.GetContainer(databaseId, containerId);

         var queryDefinition = new QueryDefinition($@"
                    SELECT Top @topN
                        x.id, x.metadata, x.similarityScore
                    FROM 
                        (
                            SELECT c.id,c.metadata, VectorDistance(c.vector, @embedding, false) as similarityScore FROM c
                        ) x
                    WHERE x.similarityScore > @similarityScore
                    ORDER BY x.similarityScore desc
                ");
         queryDefinition.WithParameter("@similarityScore", 0.5);
         queryDefinition.WithParameter("@embedding", embedding.ToArray());
         queryDefinition.WithParameter("@topN", 1);

         var results = new List<dynamic>();

         using (var resultSetIterator = _container.GetItemQueryIterator<dynamic>(queryDefinition, requestOptions: _queryOptions))
         {
            while (resultSetIterator.HasMoreResults)
            {
               var response = await resultSetIterator.ReadNextAsync();
               results.AddRange(response);
            }
         }

         return results;
      }

      public async Task<List<Airport>> FetchAirportDetailsAsync(string departure, string destination)
      {
         _container = _cosmosClient.GetContainer(databaseId, "Airports");

         var query = new QueryDefinition("SELECT * FROM c WHERE LOWER(c.city) IN (LOWER(@departure), LOWER(@destination))")
             .WithParameter("@departure", departure)
             .WithParameter("@destination", destination);

         var airports = new List<Airport>();

         using (var resultSetIterator = _container.GetItemQueryIterator<Airport>(query, requestOptions: _queryOptions))
         {
            while (resultSetIterator.HasMoreResults)
            {
               var response = await resultSetIterator.ReadNextAsync();
               airports.AddRange(response);
            }
         }

         return airports;
      }

      public async Task<List<Weather>> FetchWeatherDetailsAsync(string city, DateTime travelDate)
      {
         try
         {
            _container = _cosmosClient.GetContainer(databaseId, "Weather");

            var query = new QueryDefinition(
               @"SELECT * 
                       FROM c 
                       WHERE LOWER(c.LocationName) = LOWER(@locationName) 
                       AND c.StartDate >= DateTimeAdd('hour', -1, @startDateTime) 
                       AND c.StartDate <= DateTimeAdd('hour', 1, @endDateTime)")
               .WithParameter("@locationName", city)
               .WithParameter("@startDateTime", travelDate.ToString("yyyy-MM-ddTHH:mm:ssZ"))
               .WithParameter("@endDateTime", travelDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            var weatherDetails = new List<Weather>();

            using (var resultSetIterator = _container.GetItemQueryIterator<Weather>(query, requestOptions: _queryOptions))
            {
               while (resultSetIterator.HasMoreResults)
               {
                  var response = await resultSetIterator.ReadNextAsync();
                  weatherDetails.AddRange(response);
               }
            }

            return weatherDetails;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }

      public async Task<List<FlightListing>> FetchFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate)
      {
         try
         {
            _container = _cosmosClient.GetContainer(databaseId, "FlightListings");

            var query = new QueryDefinition(
                @"SELECT * FROM c WHERE LOWER(c.departure) = LOWER(@departureCode) AND LOWER(c.destination) = LOWER(@destinationCode) 
                        AND DateTimePart('day', c.departureTime) = @day 
                        AND DateTimePart('month', c.departureTime) = @month 
                        AND DateTimePart('year', c.departureTime) = @year")
                .WithParameter("@departureCode", departureCode)
                .WithParameter("@destinationCode", destinationCode)
                .WithParameter("@day", travelDate.Day)
                .WithParameter("@month", travelDate.Month)
                .WithParameter("@year", travelDate.Year);

            var flights = new List<FlightListing>();

            using (var resultSetIterator = _container.GetItemQueryIterator<FlightListing>(query, requestOptions: _queryOptions))
            {
               while (resultSetIterator.HasMoreResults)
               {
                  var response = await resultSetIterator.ReadNextAsync();
                  flights.AddRange(response);
               }
            }

            return flights;
         }
         catch (Exception ex)
         {
            throw ex;
         }
      }

      public async Task<List<string>> FetchChatHistoryAsync(string sessionId)
      {
         _container = _cosmosClient.GetContainer(databaseId, "ChatHistory");

         var query = new QueryDefinition("SELECT top 20 * FROM c WHERE c.sessionId = @sessionId ORDER BY c.Timestamp desc")
             .WithParameter("@sessionId", sessionId);

         var chatHistory = new List<string>();

         using (var resultSetIterator = _container.GetItemQueryIterator<ChatRecord>(query, requestOptions: _queryOptions))
         {
            while (resultSetIterator.HasMoreResults)
            {
               var response = await resultSetIterator.ReadNextAsync();
               chatHistory.AddRange(response.Select(record => record.Message));
            }
         }

         return chatHistory;
      }

      public async Task<List<ChatRecord>> FetchChatHistoriesAsync(string sessionId, string userId)
      {
         _container = _cosmosClient.GetContainer(databaseId, "ChatHistory");

         var query = new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId and c.customerId= @customerId ORDER BY c.Timestamp asc")
             .WithParameter("@sessionId", sessionId)
             .WithParameter("@customerId", userId);

         var chatHistory = new List<ChatRecord>();

         using (var resultSetIterator = _container.GetItemQueryIterator<ChatRecord>(query, requestOptions: _queryOptions))
         {
            while (resultSetIterator.HasMoreResults)
            {
               var response = await resultSetIterator.ReadNextAsync();
               chatHistory.AddRange(response);
            }
         }

         return chatHistory;
      }
      public async Task<List<SessionSummary>> FetchChatSummariesByUserIdAsync(string userId)
      {
         _container = _cosmosClient.GetContainer(databaseId, "ChatHistory");

         var query = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId ORDER BY c.timestamp DESC")
             .WithParameter("@customerId", userId);

         var iterator = _container.GetItemQueryIterator<ChatRecord>(query);
         var chatRecords = new List<ChatRecord>();

         while (iterator.HasMoreResults)
         {
            var response = await iterator.ReadNextAsync();
            chatRecords.AddRange(response);
         }

         var sessionSummaries = chatRecords
             .GroupBy(cr => cr.SessionId)
             .Select(g => new SessionSummary
             {
                SessionId = g.Key,
                LastMessage = g.First().Message,
                LastMessageTimestamp = g.First().Timestamp
             })
             .ToList();

         return sessionSummaries;
      }

      public async Task StoreChatHistoryAsync(string sessionId, string message, string customerId, string customerName, bool isAssistant)
      {
         _container = _cosmosClient.GetContainer(databaseId, "ChatHistory");

         var chatRecord = new ChatRecord
         {
            SessionId = sessionId,
            CustomerName = customerName,
            CustomerId = customerId,
            IsAssistant = isAssistant,
            MessageId = Guid.NewGuid().ToString(),
            Message = message,
            Timestamp = DateTime.UtcNow
         };

         await _container.CreateItemAsync(chatRecord, new PartitionKey(sessionId));
      }

      public async Task<Passenger> FetchUserDetailsAsync(string userId)
      {
         try
         {
            _container = _cosmosClient.GetContainer(databaseId, "Passengers");

            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @userId")
            .WithParameter("@userId", userId);

            using (var resultSetIterator = _container.GetItemQueryIterator<Passenger>(query, requestOptions: _queryOptions))
            {
               while (resultSetIterator.HasMoreResults)
               {
                  var response = await resultSetIterator.ReadNextAsync();
                  var user = response.FirstOrDefault();
                  if (user != null)
                  {
                     return user;
                  }
               }
            }
            return null;
         }
         catch (Exception ex)
         {
            throw;
         }
      }
   }
}
