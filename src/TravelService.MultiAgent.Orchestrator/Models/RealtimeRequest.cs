using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable OPENAI002

namespace TravelService.MultiAgent.Orchestrator.Models
{
   public class RealtimeRequest
   {      
      public string SessionId { get; set; }
      public string UserQuery { get; set; }
      public string FunctionCallId { get; set; }
   }
}
