using BookingService.Models;

namespace BookingService.Interfaces
{
    public interface IPostmarkServiceClient
    {
        public Task SendEmail(PostmarkEmail postmarkEmail);
    }
}
