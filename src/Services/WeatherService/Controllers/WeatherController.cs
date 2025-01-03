using Microsoft.AspNetCore.Mvc;
using WeatherService.Interfaces;
using WeatherService.Services;

namespace WeatherService.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class WeatherController : ControllerBase
   {
      private readonly ICosmosService _cosmosService;
      private readonly ILogger<WeatherController> _logger;
      public WeatherController(ICosmosService cosmosService, ILogger<WeatherController> logger)
      {
         _cosmosService = cosmosService;
         _logger = logger;
      }

      [HttpGet]
      public async Task<IActionResult> GetWeatherDetails([FromQuery] string city, [FromQuery] string travelDate)
      {
         if (string.IsNullOrWhiteSpace(city) || travelDate == default)
         {
            return BadRequest(new { Message = "City and travel date are required." });
         }

         try
         {
            var weatherDetails = await _cosmosService.FetchWeatherDetailsAsync(city, DateTime.Parse(travelDate));

            if (weatherDetails == null || !weatherDetails.Any())
            {
               return NotFound(new { Message = $"No weather details found for {city} on {travelDate}." });
            }

            return Ok(weatherDetails);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An error occurred while fetching weather details.");
            return StatusCode(500, new { Message = "An error occurred while fetching weather details.", Details = ex.Message });
         }
      }
   }
}