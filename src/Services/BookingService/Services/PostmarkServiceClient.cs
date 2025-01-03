using BookingService.Interfaces;
using BookingService.Models;
using System.Text;

namespace BookingService.Services
{
   public class PostmarkServiceClient : IPostmarkServiceClient
   {
      private readonly HttpClient _httpClient;
      private readonly IConfiguration _configuration;

      public PostmarkServiceClient(HttpClient httpClient,IConfiguration configuration)
      {
         _httpClient = httpClient;
         _configuration = configuration;
      }

      public async Task SendEmail(PostmarkEmail postmarkEmail)
      {
         if (_configuration.GetValue<string>("PostmarkServerToken") == null || _configuration.GetValue<string>("PostmarkServerToken") == string.Empty)
         {
            await Task.CompletedTask;
         }
         else
         {
            string postData = $"{{\"From\":\"{postmarkEmail.From}\",\"To\":\"{postmarkEmail.To}\",\"Subject\":\"{postmarkEmail.Subject}\",\"HtmlBody\":\"{postmarkEmail.HtmlBody}\"}}";
            var content = new StringContent(postData, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync("/email", content);
            if (!response.IsSuccessStatusCode)
            {
               string responseBody = await response.Content.ReadAsStringAsync();
               throw new Exception($"Failed to send email. Status code: {response.StatusCode}. Response body: {responseBody}");
            }
         }
      }
   }
}
