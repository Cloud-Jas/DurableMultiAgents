using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class WeatherServiceClient : IWeatherServiceClient
   {
      private readonly HttpClient _httpClient;
      private readonly TracingContextCache _cache;
      public WeatherServiceClient(HttpClient httpClient, TracingContextCache cache)
      {
         _httpClient = httpClient;
         _cache = cache;
      }
      public async Task<List<Weather>?> GetWeatherDetails(string city, DateTime travelDate)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);
            var response = await _httpClient.GetAsync($"Weather?city={city}&traveldate={travelDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Weather>>(content);
         }
         catch (Exception ex)
         {
            return default;
         }
      }
   }
}