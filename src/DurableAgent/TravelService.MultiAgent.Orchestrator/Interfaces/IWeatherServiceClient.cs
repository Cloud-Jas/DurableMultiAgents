using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.Interfaces
{
   public interface IWeatherServiceClient
   {
      Task<List<Weather>?> GetWeatherDetails(string city, DateTime travelDate);
   }
}
