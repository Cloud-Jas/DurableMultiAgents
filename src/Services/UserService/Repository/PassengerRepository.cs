using Microsoft.EntityFrameworkCore;
using UserService.Interfaces;
using UserService.Models;

namespace UserService.Repository
{
    public class PassengerRepository : IPassengerRepository
   {
      private readonly UserServiceDbContext _context;

      public PassengerRepository(UserServiceDbContext context)
      {
         _context = context;
      }

      public async Task<Passenger?> GetPassengerByIdAsync(string userId)
      {
         return await _context.Passengers
             .FirstOrDefaultAsync(p => p.Id == userId);
      }
   }
}

