using Newtonsoft.Json;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class FlightServiceClient : IFlightServiceClient
   {
      private readonly HttpClient _httpClient;
      private readonly TracingContextCache _cache;
      public FlightServiceClient(HttpClient httpClient, TracingContextCache cache)
      {
         _httpClient = httpClient;
         _cache = cache;
      }

      public async Task<Airline> GetAirlineDetailsAsync(string airlineId)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);
            var response = await _httpClient.GetAsync($"Flights/airlines/{airlineId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Airline>(content);
         }
         catch (Exception ex)
         {
            return default;
         }
      }

      public async Task<List<Airport>?> GetAirportsAsync(string departureCity, string destinationCity)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);
            var response = await _httpClient.GetAsync($"Flights/airports?departureCity={departureCity}&destinationCity={destinationCity}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Airport>>(content);
         }
         catch (Exception ex)
         {
            return default;
         }
      }

      public async Task<List<FlightListing>?> GetFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);
            var response = await _httpClient.GetAsync($"Flights/flightlistings?departureCode={departureCode}&destinationCode={destinationCode}&travelDate={travelDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var flightListings = JsonConvert.DeserializeObject<List<FlightListing>>(content);
            return flightListings;
         }
         catch (Exception ex)
         {
            return default;
         }
      }

      public async Task<FlightListing?> GetFlightListingsAsync(string flightId)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);
            var response = await _httpClient.GetAsync($"Flights/flightlistings/{flightId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var flightListing = JsonConvert.DeserializeObject<FlightListing>(content);
            return flightListing;
         }
         catch (Exception ex)
         {
            return default;
         }
      }
   }
}
