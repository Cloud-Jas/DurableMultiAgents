using Microsoft.SemanticKernel;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IPromptyService
   {
      Task<string> RenderPromptAsync(string filePath, Kernel kernel, KernelArguments? arguments = null);
      Task<KernelFunction> GetKernelFuntionAsync(string filePath, Kernel kernel, KernelArguments? arguments);
   }

}
