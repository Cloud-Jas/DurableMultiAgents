using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.Services
{
    public class KernelService : IKernelService
    {
        private readonly AzureOpenAIChatCompletionService _chatCompletionService;

        public KernelService(AzureOpenAIChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        public async Task<ChatMessageContent> GetChatMessageContentAsync(Kernel kernel, string prompt, OpenAIPromptExecutionSettings? promptExecutionSettings)
        {
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings();

            if (promptExecutionSettings != null)
                settings = promptExecutionSettings;

            settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            return await _chatCompletionService.GetChatMessageContentAsync(
                prompt,
                executionSettings: settings,
                kernel: kernel
            );
        }
    }
}