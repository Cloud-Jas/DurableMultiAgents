using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IUserServiceClient
   {
      Task<Passenger?> GetPassengerByIdAsync(string userId);
   }
}
