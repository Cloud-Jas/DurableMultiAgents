using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
    public class PostmarkEmail
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
    }
}
