using BookingService.Interfaces;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class BookingController : ControllerBase
   {
      private readonly ICosmosService _cosmosDbService;
      private readonly IPostmarkServiceClient _postmarkServiceClient;
      private ILogger<BookingController> _logger;

      public BookingController(ICosmosService cosmosDbService, IPostmarkServiceClient postmarkServiceClient, ILogger<BookingController> logger)
      {
         _cosmosDbService = cosmosDbService;
         _postmarkServiceClient = postmarkServiceClient;
         _logger = logger;
      }

      [HttpPost]
      public async Task<IActionResult> AddBooking([FromBody] BookingRequest bookingRequest)
      {
         try
         {
            await _cosmosDbService.InsertBookingAsync(bookingRequest.UserId,bookingRequest.DepartureCity,bookingRequest.DestinationCity, bookingRequest.FromDestinationFlightId,bookingRequest.FromDestinationFlightPrice,bookingRequest.ToDestinationFlightId, bookingRequest.ToDestinationFlightPrice);
            return Ok("Booking created successfully.");
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error in AddBooking");
            return StatusCode(500, $"Internal server error: {ex.Message}");
         }
      }

      [HttpPost("SendEmail")]
      public async Task<IActionResult> SendEmail([FromBody] PostmarkEmail postmarkEmail)
      {
         try
         {
            await _postmarkServiceClient.SendEmail(postmarkEmail);
            return Ok("Email sent successfully.");
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error in SendEmail");
            return StatusCode(500, $"Internal server error: {ex.Message}");
         }
      }

   }
}
