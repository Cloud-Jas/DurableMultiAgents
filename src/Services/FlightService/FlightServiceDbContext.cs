namespace FlightService
{
   using FlightService.Models;
   using Microsoft.EntityFrameworkCore;

   public class FlightServiceDbContext : DbContext
   {
      public FlightServiceDbContext(DbContextOptions<FlightServiceDbContext> options)
          : base(options)
      {
      }

      public DbSet<Airport> Airports { get; set; }
      public DbSet<FlightListing> FlightListings { get; set; }
      public DbSet<Airline> Airlines { get; set; }
   }
}
