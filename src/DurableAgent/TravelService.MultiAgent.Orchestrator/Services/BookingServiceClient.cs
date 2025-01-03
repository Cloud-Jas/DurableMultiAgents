using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class BookingServiceClient : IBookingServiceClient
   {
      private readonly HttpClient _httpClient;
      private readonly TracingContextCache _cache;
      private readonly IConnectionMultiplexer _redisConnection;
      private readonly ILogger<BookingServiceClient> _logger;

      public BookingServiceClient(HttpClient httpClient, TracingContextCache cache, IConnectionMultiplexer redisConnection,ILogger<BookingServiceClient> logger)
      {
         _httpClient = httpClient;
         _cache = cache;
         _redisConnection = redisConnection;
         _logger = logger;
      }

      public async Task InsertBookingAsync(string userId, string departureCity, string destinationCity, string fromDestinationflightId, string fromDestinationflightPrice, string toDestinationFlightId, string toDestinationFlightPrice)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);

            var jsonContent = JsonConvert.SerializeObject(new
            {
               UserId = userId,
               DepartureCity = departureCity,
               DestinationCity = destinationCity,
               FromDestinationFlightId = fromDestinationflightId,
               FromDestinationFlightPrice = fromDestinationflightPrice,
               ToDestinationFlightId = toDestinationFlightId,
               ToDestinationFlightPrice = toDestinationFlightPrice
            });
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"Booking", httpContent);
            response.EnsureSuccessStatusCode();
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error while inserting booking");
         }

      }

      public async Task SendEmail(PostmarkEmail postmarkEmail)
      {
         try
         {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.AddOpenTelemetryHeaders(_cache);

            var jsonContent = JsonConvert.SerializeObject(postmarkEmail);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"Booking/sendemail", httpContent);
            response.EnsureSuccessStatusCode();
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error while sending email");
         }
      }
   }
}
