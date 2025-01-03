namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IFabricGraphQLService
   {
      Task<dynamic> FetchBookingDetailsAsync(string passengerId, string flightId);
   }
}