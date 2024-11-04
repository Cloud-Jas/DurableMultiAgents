using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Models;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

namespace TravelService.MultiAgent.Orchestrator.Agents.Booking.Plugins
{
   public class BookingPlugin
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly IPluginTracingHandler _pluginTracingHandler;
      public BookingPlugin(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         _pluginTracingHandler = serviceProvider.GetService<IPluginTracingHandler>() ?? throw new ArgumentNullException(nameof(IPluginTracingHandler));
      }

      [KernelFunction("SendBookingConfirmationMail")]
      [Description("Send booking confirmation mail for the given user with the flight details")]
      public async Task<string> SendBookingConfirmationMailAsync(
          [Description("User Id")]
            string userId,
          [Description("User Email Id")]
            string userEmail,
          [Description("To destination Flight Id")]
            string toFlightId,
          [Description("From destination Flight Id")]
            string fromFlightId,
          [Description("To destination Flight Price")]
            string toflightPrice,
          [Description("From destination Flight Price")]
            string fromflightPrice,
          [Description("Booking confirmation mail summary in a email friendly format with <hr> <b> <br/> tags wherever relevant.")]
            string bookingMailConfirmationSummary
          )
      {

         var parameters = new Dictionary<string, string>
            {
               {"pluginName", "SendBookingConfirmationMailPlugin" },
               { "userId", userId },
               { "userEmail", userEmail },
               { "toFlightId", toFlightId },
               { "fromFlightId", fromFlightId },
               { "toflightPrice", toflightPrice },
               { "fromflightPrice", fromflightPrice },
               { "bookingMailConfirmationSummary", bookingMailConfirmationSummary }
            };

         Func<Dictionary<string, string>, Task<string>> callBookingConfirmationPlugin = async (inputs) =>
         {
            try
            {

               var userId = inputs["userId"];
               var userEmail = inputs["userEmail"];
               var toFlightId = inputs["toFlightId"];
               var fromFlightId = inputs["fromFlightId"];
               var toflightPrice = inputs["toflightPrice"];
               var fromflightPrice = inputs["fromflightPrice"];
               var bookingMailConfirmationSummary = inputs["bookingMailConfirmationSummary"];

               var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

               var emailService = _serviceProvider.GetRequiredService<IPostmarkServiceClient>();

               if (!string.IsNullOrWhiteSpace(toFlightId))
                  await cosmosService.InsertBookingAsync(userId, toFlightId, toflightPrice);
               if (!string.IsNullOrWhiteSpace(fromFlightId))
                  await cosmosService.InsertBookingAsync(userId, fromFlightId, fromflightPrice);

               await SendEmailAsync(emailService, userEmail, bookingMailConfirmationSummary);
            }
            catch (Exception ex)
            {
               return "Error sending booking confirmation mail.";
            }

            return bookingMailConfirmationSummary;
         };

         return await _pluginTracingHandler.ExecutePlugin(callBookingConfirmationPlugin, parameters);

      }
      public async Task SendEmailAsync(IPostmarkServiceClient postmarkServiceClient, string emailId, string emailBody)
      {

         string from = "contosotravelagency@iamdivakarkumar.com";
         string to = emailId;
         string subject = "Booking confirmation";
         string bookingConfirmation = emailBody;
         string htmlTemplate = @"<html><head><title>Booking confirmation</title><style>body {font-family: 'Arial', sans-serif;background-color: #f4f4f4;color: #333;margin: 0;padding: 0;}.container {max-width: 600px;margin: 20px auto;background-color: #fff;padding: 20px;border-radius: 8px;box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);}h1 {color: #007BFF;}p {line-height: 1.6;}.summary {background-color: #e9f5e6;padding: 10px;margin-top: 20px;border-radius: 8px;}.action-points {margin-top: 20px;}.action-points h2 {color: #007BFF;}.action-points ul {list-style-type: none;padding: 0;}.action-points li {margin-bottom: 10px;}.footer {margin-top: 20px;text-align: center;color: #555;}.blockquote {padding: 60px 80px 40px;position: relative;}.blockquote p {font-family: 'Utopia-italic';font-size: 35px;font-weight: 700px;text-align: center;}.blockquote:before {position: absolute;font-family: 'FontAwesome';top: 0;content:'\f10d';font-size: 200px;color: rgba(0,0,0,0.1);}.blockquote::after {content: '';top: 20px;left: 50%;margin-left: -100px;position: absolute;border-bottom: 3px solid #bf0024;height: 3px;width: 200px;}</style></head><body><div class='container'><h1>Your Booking Confirmation</h1><p>Hello,</p><div class='summary'>{{content}}</div><p>Thank you for using Contoso Travel agency for your vacations!</p><div class='footer'><p>Best regards,<br>Your ContosoTravelAgency Team</p></div></div></body></html>";
         string emailContent = htmlTemplate.Replace("{{content}}", bookingConfirmation);
         await postmarkServiceClient.SendEmail(new PostmarkEmail
         {
            From = from,
            Subject = subject,
            To = to,
            HtmlBody = emailContent,
         });

      }
   }
}
