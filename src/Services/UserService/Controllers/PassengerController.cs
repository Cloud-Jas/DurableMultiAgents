using Microsoft.AspNetCore.Mvc;
using UserService.Interfaces;
using UserService.Repository;

namespace UserService.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class PassengerController : ControllerBase
   {
      private readonly IPassengerRepository _repository;

      public PassengerController(IPassengerRepository repository)
      {
         _repository = repository;
      }

      [HttpGet("{userId}")]
      public async Task<IActionResult> GetPassengerById([FromRoute] string userId)
      {
         try
         {
            var passenger = await _repository.GetPassengerByIdAsync(userId);

            if (passenger == null)
            {
               return NotFound(new { Message = $"Passenger with ID {userId} not found." });
            }

            return Ok(passenger);
         }
         catch (Exception ex)
         {
            return StatusCode(500, $"Internal server error: {ex.Message}");
         }
      }
   }
}
