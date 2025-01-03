using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.Services
{
    public class FabricAuthService : IFabricAuthService
   {
      private readonly string _clientId;
      private readonly string _clientSecret;
      private readonly string _authority;

      public FabricAuthService(IConfiguration configuration)
      {
         _clientId = configuration["Fabric:ClientId"];
         _clientSecret = configuration["Fabric:ClientSecret"];
         _authority = $"https://login.microsoftonline.com/{configuration["Fabric:TenantId"]}";
      }

      public async Task<string> GetAccessTokenAsync()
      {
         var app = ConfidentialClientApplicationBuilder.Create(_clientId)
             .WithClientSecret(_clientSecret)
             .WithAuthority(_authority)
             .Build();

         var scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };
         var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
         return result.AccessToken;
      }
   }
}
