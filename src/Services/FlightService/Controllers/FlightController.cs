using Microsoft.AspNetCore.Mvc;

namespace FlightService.Controllers
{
   using FlightService.Interfaces;
   using FlightService.Models;
   using FlightService.Repository;
   using Microsoft.AspNetCore.Mvc;
   using System.Globalization;

   [ApiController]
   [Route("api/[controller]")]
   public class FlightsController : ControllerBase
   {
      private readonly IFlightServiceRepository _repository;
      private ILogger<FlightsController> _logger;

      public FlightsController(IFlightServiceRepository repository, ILogger<FlightsController> logger)
      {
         _repository = repository;
         _logger = logger;
      }

      [HttpGet("airlines/{airlineId}")]
      public async Task<IActionResult> GetAirlineDetails([FromRoute] string airlineId)
      {
         try
         {
            var airline = await _repository.FetchAirlineDetailsAsync(airlineId);
            var airlineDTO = new AirlineDTO
            {
               Id = airline.AirlineId,
               Name = airline.Name,
               Country = airline.Country,
               LogoUrl = airline.LogoUrl,
               City = airline.City,
               Code = airline.Code
            };
            if (airline == null)
            {
               _logger.LogInformation($"Airline with ID {airlineId} not found.");
               return NotFound();
            }

            _logger.LogInformation($"Fetched airline with ID {airlineId}");
            return Ok(airlineDTO);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An error occurred while fetching airline details.");
            return StatusCode(500, new { Message = "An error occurred while fetching airline details.", Details = ex.Message });
         }
      }

      [HttpGet("airports")]
      public async Task<IActionResult> GetAirports([FromQuery] string departureCity, [FromQuery] string destinationCity)
      {
         try
         {
            var airports = await _repository.FetchAirportDetailsAsync(departureCity, destinationCity);
            _logger.LogInformation($"Fetched {airports.Count} airports for departure city {departureCity} and destination city {destinationCity}");
            return Ok(airports);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An error occurred while fetching airports.");
            return StatusCode(500, new { Message = "An error occurred while fetching airports.", Details = ex.Message });
         }

      }

      [HttpGet("flightListings/{flightNumber}")]
      public async Task<IActionResult> GetFlightListing(string flightNumber)
      {
         try
         {
            var flightListing = await _repository.FetchFlightListingAsync(flightNumber);
            if (flightListing == null)
            {
               _logger.LogInformation($"Flight listing with flight number {flightNumber} not found.");
               return NotFound();
            }

            var flightListingDTO = new FlightListingDTO
            {
               Id = flightListing.FlightId,
               FlightNumber = flightListing.FlightNumber,
               AirlineId = flightListing.AirlineId,
               DepartureAirportCode = flightListing.DepartureAirportCode,
               DestinationAirportCode = flightListing.DestinationAirportCode,
               DepartureTime = DateTime.UtcNow.Date.Add(TimeSpan.Parse(flightListing.DepartureTime)),
               Price = flightListing.Price,
               Description = flightListing.Description,
               AircraftType = flightListing.AircraftType,
               AvailableSeats = flightListing.AvailableSeats,
               Duration = flightListing.Duration
            };

            _logger.LogInformation($"Fetched flight listing with flight number {flightNumber}");
            return Ok(flightListingDTO);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An error occurred while fetching flight listing.");
            return StatusCode(500, new { Message = "An error occurred while fetching flight listing.", Details = ex.Message });
         }
      }

      [HttpGet("flightlistings")]
      public async Task<IActionResult> GetFlightListings([FromQuery] string departureCode, [FromQuery] string destinationCode, [FromQuery] string travelDate)
      {
         try
         {
            var flightListings = await _repository.FetchFlightListingsAsync(departureCode, destinationCode,DateTime.Parse(travelDate));

            var flightListingDTos = flightListings.Select(flightListing => new FlightListingDTO
            {
               Id = flightListing.FlightId,
               FlightNumber = flightListing.FlightNumber,
               AirlineId = flightListing.AirlineId,
               DepartureAirportCode = flightListing.DepartureAirportCode,
               DestinationAirportCode = flightListing.DestinationAirportCode,
               DepartureTime = (DateTime.Parse(travelDate)).Date.Add(TimeSpan.Parse(flightListing.DepartureTime)),
               Price = flightListing.Price,
               Description = flightListing.Description,
               AircraftType = flightListing.AircraftType,
               AvailableSeats = flightListing.AvailableSeats,
               Duration = flightListing.Duration
            }).ToList();


            _logger.LogInformation($"Fetched {flightListings.Count} flight listings for departure code {departureCode}, destination code {destinationCode}, and travel date {travelDate}");
            return Ok(flightListings);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An error occurred while fetching flight listings.");
            return StatusCode(500, new { Message = "An error occurred while fetching flight listings.", Details = ex.Message });
         }
      }
   }
}
