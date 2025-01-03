using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0110 

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IKernelService
   {
      Task<ChatMessageContent> GetChatMessageContentAsync(Kernel kernel, string prompt, OpenAIPromptExecutionSettings? settings = null);
      Task<ChatMessageContent> GetChatMessageContentAsync(Kernel kernel, AgentGroupChat chat);
    }
}
