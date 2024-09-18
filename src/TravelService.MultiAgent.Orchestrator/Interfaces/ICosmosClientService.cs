using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface ICosmosClientService
   {
      Task StoreChatHistoryAsync(string sessionId, string message, string customerId, string customerName, bool isAssistant, List<string>? agentMessages = null);
      Task<List<SessionSummary>> FetchChatSummariesByUserIdAsync(string userId);
      Task<List<string>> FetchChatHistoryAsync(string sessionId);
      Task<List<ChatRecord>> FetchChatHistoriesAsync(string sessionId, string userId);
      Task<List<Airport>> FetchAirportDetailsAsync(string departure, string destination);
      Task<List<FlightListing>> FetchFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate);
      Task<List<Weather>> FetchWeatherDetailsAsync(string city, DateTime travelDate);
      Task InsertBookingAsync(string userId, string flightId, string flightPrice);
      Task<Passenger> FetchUserDetailsAsync(string userId);
      Task<List<dynamic>> FetchDetailsFromSemanticLayer(string queryPrompt, string containerId);
      Task<List<dynamic>> FetchDetailsFromVectorSemanticLayer(ReadOnlyMemory<float> embedding, string containerId);
   }
}