namespace WeatherService.Models
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
   }
}