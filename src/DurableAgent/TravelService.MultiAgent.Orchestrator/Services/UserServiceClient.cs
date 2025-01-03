using Newtonsoft.Json;
using StackExchange.Redis;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class UserServiceClient : IUserServiceClient
   {
      private readonly HttpClient _httpClient;
      private readonly TracingContextCache _cache;
      private readonly IConnectionMultiplexer _redisConnection;

      public UserServiceClient(HttpClient httpClient, TracingContextCache cache, IConnectionMultiplexer redisConnection)
      {
         _httpClient = httpClient;
         _cache = cache;
         _redisConnection = redisConnection;
      }

      public async Task<Passenger?> GetPassengerByIdAsync(string userId)
      {
         try
         {
            var redisDb = _redisConnection.GetDatabase();

            var cacheKey = $"Passenger_{userId}";
            var cachedData = await redisDb.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
               return JsonConvert.DeserializeObject<Passenger>(cachedData);
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);

            var response = await _httpClient.GetAsync($"Passenger/{userId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var passenger = JsonConvert.DeserializeObject<Passenger>(content);

            if (passenger != null)
            {
               await redisDb.StringSetAsync(cacheKey, JsonConvert.SerializeObject(passenger), TimeSpan.FromMinutes(30));
            }

            return passenger;
         }
         catch (Exception ex)
         {
            return default;
         }
      }
   }
}
