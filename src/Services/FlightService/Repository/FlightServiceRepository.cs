using FlightService.Interfaces;
using FlightService.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Repository
{
    public class FlightServiceRepository : IFlightServiceRepository
   {
      private readonly FlightServiceDbContext _context;
      private readonly ILogger<FlightServiceRepository> _logger;

      public FlightServiceRepository(FlightServiceDbContext context, ILogger<FlightServiceRepository> logger)
      {
         _context = context;
         _logger = logger;
      }

      public async Task<Airline?> FetchAirlineDetailsAsync(string airlineId)
      {
         return await _context.Airlines.FirstOrDefaultAsync(a => a.AirlineId == airlineId);
      }

      public async Task<List<Airport>> FetchAirportDetailsAsync(string departureCity, string destinationCity)
      {
         return await _context.Airports
             .Where(a => a.City.ToLower() == departureCity.ToLower() || a.City.ToLower() == destinationCity.ToLower())
             .ToListAsync();
      }

      public async Task<FlightListing?> FetchFlightListingAsync(string flightNumber)
      {
         return await _context.FlightListings.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
      }

      public async Task<List<FlightListing>> FetchFlightListingsAsync(string departureCode, string destinationCode, DateTime travelDate)
      {
         _logger.LogInformation($"Fetching flight listings for departure code {departureCode}, destination code {destinationCode}, and travel date {travelDate:yyyy-MM-dd}");

         _logger.LogInformation("Travel time Day: " + travelDate.Day);
         _logger.LogInformation("Travel time Month: " + travelDate.Month);
         _logger.LogInformation("Travel time Year: " + travelDate.Year);
         _logger.LogInformation("Travel time Date: " + travelDate.Date);

         return await _context.FlightListings
            .Where(f =>
                f.DepartureAirportCode.ToLower() == departureCode.ToLower() &&
                f.DestinationAirportCode.ToLower() == destinationCode.ToLower()).Take(2)
            .ToListAsync();
      }
   }

}
