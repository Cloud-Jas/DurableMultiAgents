using Microsoft.SemanticKernel.Prompty;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;

#pragma warning disable SKEXP0040 
namespace TravelService.MultiAgent.Orchestrator.Services
{
    public class PromptyService : IPromptyService
    {        
        public PromptyService()
        {
            
        }

        public async Task<string> RenderPromptAsync(string filePath, Kernel kernel, KernelArguments? arguments)
        {
            var promptConfig = KernelFunctionPrompty.ToPromptTemplateConfig(File.ReadAllText(filePath));
            
            var promptTemplateFactory = new LiquidPromptTemplateFactory();
            
            var promptTemplate = promptTemplateFactory.Create(promptConfig);            

            return await promptTemplate.RenderAsync(kernel, arguments);
        }
    }
}
