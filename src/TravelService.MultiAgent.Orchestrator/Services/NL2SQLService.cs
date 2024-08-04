using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
    public class NL2SQLService : INL2SQLService
    {
        private readonly string tenantId;
        private readonly string subscriptionId;
        private readonly string resourceGroup;
        private readonly string databaseId;
        private readonly string databaseAccount;
        private readonly string containerId;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string FetchDetailsEndpoint;
        public NL2SQLService(IConfiguration configuration, HttpClient httpClient)
        {
            tenantId = configuration["TenantId"];
            subscriptionId = configuration["SubscriptionId"];
            resourceGroup = configuration["ResourceGroup"];
            databaseId = configuration["DatabaseId"];
            databaseAccount = configuration["DatabaseAccount"];
            containerId = configuration["ContainerId"];
            _httpClient = httpClient;
            FetchDetailsEndpoint = $"https://tools.cosmos.azure.com/api/controlplane/toolscontainer/cosmosaccounts/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{databaseAccount}/containerconnections/multicontainer";
        }

        public async Task<string> GetSQLQueryAsync(string userPrompt,string semanticLayer)
        {
            var credentials = new DefaultAzureCredential();

            var tokenResult = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }, tenantId: tenantId), CancellationToken.None);

            var (forwardingId, url, token) = await FetchDetailsAsync(tokenResult.Token,semanticLayer);
            
            return await GenerateSQLQueryAsync(userPrompt, url, token);
        }
        private async Task<(string forwardingId, string url, string token)> FetchDetailsAsync(string bearerToken, string semanticLayer)
        {
            var requestData = new
            {
                poolId = "query-copilot",
                databaseId = databaseId,
                containerId = semanticLayer,
                mode = "User"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");                        
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");           

            var response = await _httpClient.PostAsync(FetchDetailsEndpoint, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var phoenixResponse = System.Text.Json.JsonSerializer.Deserialize<ServiceResponse[]>(content, options);

                string forwardingId = phoenixResponse[0].PhoenixServiceInfo.ForwardingId;
                string url = phoenixResponse[0].PhoenixServiceInfo.PhoenixServiceUrl;
                string token = phoenixResponse[0].PhoenixServiceInfo.AuthToken;
                return (forwardingId, url, token);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        private async Task<string> GenerateSQLQueryAsync(string userPrompt, string url, string token)
        {
            var requestData = new { userPrompt };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("authorization", $"token {token}");                

                var response = await httpClient.PostAsync(url+ "public/generateSQLQuery", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var sqlResponse =  await response.Content.ReadAsStringAsync();
                    var sqlResponseObj = JsonConvert.DeserializeObject<NL2SQLResponse>(sqlResponse);
                    return sqlResponseObj.Sql;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
        }
    }
}
