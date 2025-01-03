using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Models
{
   public class Weather
   {
      public string id { get; set; }
      public string LocationId { get; set; }
      public string LocationName { get; set; }
      public string Country { get; set; }
      public string StartTime { get; set; }
      public string EndTime { get; set; }
      public string WeatherCondition { get; set; }
      public double TemperatureCelsius { get; set; }
      public int Humidity { get; set; }
      public double WindSpeedKmh { get; set; }
      public DateTime LastUpdated { get; set; }
   }
}
