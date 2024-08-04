using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.Agents.Weather.Plugins
{
    public class WeatherPlugin
    {
        private readonly IServiceProvider _serviceProvider;
        public WeatherPlugin(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [KernelFunction("GetWeather")]
        [Description("Get the weather for the given city and date")]
        public async Task<string> GetWeatherAsync(
            [Description("City name")]
            string city,
            [Description("Date")]
            DateTime date
            )
        {
            var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

            var weather = await cosmosService.FetchWeatherDetailsAsync(city, date);

            string response = "";

            foreach (var w in weather)
            {
                response += $"City: \"{w.LocationName}\"  Date: \"{w.StartDate}\"  Temperature: \"{w.TemperatureCelsius}\"  Weather: \"{w.WeatherCondition}\"\n";
            }


            return response;
        }
    }
}
