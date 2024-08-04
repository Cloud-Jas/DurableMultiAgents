using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
    public interface ICosmosClientService
    {             
        Task StoreChatHistoryAsync(string sessionId, string message);
        Task<List<string>> FetchChatHistoryAsync(string sessionId);
        Task<List<Airport>> FetchAirportDetailsAsync(string departure, string destination);
        Task<List<FlightListing>> FetchFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate);
        Task<List<Weather>> FetchWeatherDetailsAsync(string city, DateTime travelDate);        
        Task InsertBookingAsync(string userId, string flightId, string flightPrice);
        Task<Passenger> FetchUserDetailsAsync(string userId);
        Task<List<dynamic>> FetchDetailsFromSemanticLayer(string queryPrompt, string containerId);
        Task<List<dynamic>> FetchDetailsFromVectorSemanticLayer(ReadOnlyMemory<float> embedding, string containerId);
    }
}