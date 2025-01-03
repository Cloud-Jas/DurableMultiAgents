using WeatherService.Models;

namespace WeatherService.Interfaces
{
   public interface ICosmosService
   {
      Task<List<Weather>> FetchWeatherDetailsAsync(string city, DateTime travelDate);

   }
}
