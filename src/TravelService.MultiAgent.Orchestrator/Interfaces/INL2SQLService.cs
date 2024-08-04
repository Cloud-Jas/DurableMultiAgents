namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
    public interface INL2SQLService
    {
        Task<string> GetSQLQueryAsync(string userPrompt, string semanticLayer);
    }
}