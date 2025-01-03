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

            var travelStartTime = travelDate.ToString("HH:mm");

            var query = new QueryDefinition(
                    @"SELECT * 
                  FROM c 
                  WHERE LOWER(c.LocationName) = LOWER(@locationName) 
                  AND @travelStartTime >= c.StartTime 
                  AND @travelStartTime <= c.EndTime")
                .WithParameter("@locationName", city)
                .WithParameter("@travelStartTime", travelStartTime);

            var weatherDetails = new List<Weather>();

            using (var resultSetIterator = _container.GetItemQueryIterator<Weather>(query, requestOptions: _queryOptions))
            {
               while (resultSetIterator.HasMoreResults)
               {
                  var response = await resultSetIterator.ReadNextAsync();
                  weatherDetails.AddRange(response);
               }
            }

            if (weatherDetails.Count == 0)
            {
               var randomClimate = GenerateRandomWeather(city, travelStartTime);
               weatherDetails.Add(randomClimate);
            }

            return weatherDetails;
         }
         catch (Exception ex)
         {
            throw new ApplicationException("An error occurred while fetching weather details.", ex);
         }
      }
      private Weather GenerateRandomWeather(string city,string travelStartTime)
      {
         var random = new Random();

         var weatherConditions = new[] { "Sunny", "Cloudy", "Rainy", "Windy", "Stormy", "Snowy" };
         var temperatures = new[] { 10, 15, 20, 25, 30, 35, 40 };
         var humidity = random.Next(30, 80);
         var windSpeed = random.Next(5, 30);

         var randomWeather = new Weather
         {
            LocationName = city,
            StartTime = travelStartTime,
            EndTime = travelStartTime,
            WeatherCondition = weatherConditions[random.Next(weatherConditions.Length)],
            TemperatureCelsius = temperatures[random.Next(temperatures.Length)],
            Humidity = humidity,
            WindSpeedKmh = windSpeed
         };

         return randomWeather;
      }
   }
}