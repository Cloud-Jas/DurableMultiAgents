namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
    public interface IFabricAuthService
    {
      Task<string> GetAccessTokenAsync();

   }
}