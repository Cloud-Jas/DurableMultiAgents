using UserService.Models;

namespace UserService.Interfaces
{
    public interface IPassengerRepository
    {
        Task<Passenger?> GetPassengerByIdAsync(string userId);
    }
}