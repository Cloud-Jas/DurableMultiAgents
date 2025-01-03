using Microsoft.Azure.Cosmos;
using System.ComponentModel;
using System.Configuration;
using WeatherService.Interfaces;
using WeatherService.Models;
using Container = Microsoft.Azure.Cosmos.Container;

namespace WeatherService.Services
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

      public async Task<List<Weather>> FetchWeatherDetailsAsync(string city, DateTime travelDate)
      {
         try
         {
            _container = _cosmosClient.GetContainer(databaseId, "Weather");

            var query = new QueryDefinition(
                @"SELECT * 
                  FROM c 
                  WHERE LOWER(c.LocationName) = LOWER(@locationName) 
                  AND c.StartDate >= DateTimeAdd('hour', -2, @startDateTime) 
                  AND c.StartDate <= DateTimeAdd('hour', 2, @endDateTime)")
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
            throw new ApplicationException("An error occurred while fetching weather details.", ex);
         }
      }
   }
}