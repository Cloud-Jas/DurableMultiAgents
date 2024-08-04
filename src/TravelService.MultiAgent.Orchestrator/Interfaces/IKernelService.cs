using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
    public interface IKernelService
    {
        Task<ChatMessageContent> GetChatMessageContentAsync(Kernel kernel, string prompt, OpenAIPromptExecutionSettings? settings = null);
    }
}
