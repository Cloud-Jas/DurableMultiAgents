using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService
{
   public class UserServiceDbContext : DbContext
   {
      public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options)
          : base(options)
      {
      }

      public DbSet<Passenger> Passengers { get; set; }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<Passenger>()
             .HasKey(p => p.Id);

         base.OnModelCreating(modelBuilder);
      }
   }

}
