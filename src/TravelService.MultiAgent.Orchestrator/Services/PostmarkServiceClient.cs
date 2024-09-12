using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Services
{
   public class PostmarkServiceClient : IPostmarkServiceClient
   {
      private readonly HttpClient _httpClient;

      public PostmarkServiceClient(HttpClient httpClient)
      {
         _httpClient = httpClient;
      }

      public async Task SendEmail(PostmarkEmail postmarkEmail)
      {
         if (Environment.GetEnvironmentVariable("PostmarkServerToken") == null || Environment.GetEnvironmentVariable("PostmarkServerToken") == string.Empty)
         {
            await Task.CompletedTask;
         }
         else
         {
            string postmarkApiUrl = "https://api.postmarkapp.com/email";
            _httpClient.BaseAddress = new Uri("https://api.postmarkapp.com");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", Environment.GetEnvironmentVariable("PostmarkServerToken"));
            string postData = $"{{\"From\":\"{postmarkEmail.From}\",\"To\":\"{postmarkEmail.To}\",\"Subject\":\"{postmarkEmail.Subject}\",\"HtmlBody\":\"{postmarkEmail.HtmlBody}\"}}";
            var content = new StringContent(postData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(postmarkApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
               string responseBody = await response.Content.ReadAsStringAsync();
               throw new Exception($"Failed to send email. Status code: {response.StatusCode}. Response body: {responseBody}");
            }
         }
      }
   }
}
