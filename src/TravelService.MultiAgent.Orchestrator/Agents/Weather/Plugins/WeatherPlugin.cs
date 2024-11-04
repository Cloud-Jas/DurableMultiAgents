using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

namespace TravelService.MultiAgent.Orchestrator.Agents.Weather.Plugins
{
   public class WeatherPlugin
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly IPluginTracingHandler _pluginTracingHandler;
      public WeatherPlugin(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         _pluginTracingHandler = serviceProvider.GetService<IPluginTracingHandler>() ?? throw new ArgumentNullException(nameof(IPluginTracingHandler));
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

         var parameters = new Dictionary<string, string>
         {
            {"pluginName", "GetWeatherPlugin" },
            { "city", city },
            { "date", date.ToString() }
         };


         Func<Dictionary<string, string>, Task<string>> callWeatherPlugin = async (inputs) =>
         {

            try
            {
               var city = inputs["city"];
               var date = DateTime.Parse(inputs["date"]);

               var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

               var weather = await cosmosService.FetchWeatherDetailsAsync(city, date);

               string response = "";

               foreach (var w in weather)
               {
                  response += $"City: \"{w.LocationName}\"  Date: \"{w.StartDate}\"  Temperature: \"{w.TemperatureCelsius}\"  Weather: \"{w.WeatherCondition}\"\n";
               }
               return response;
            }
            catch (Exception ex)
            {
               return "Error while fetching weather details!";
            }
         };

         return await _pluginTracingHandler.ExecutePlugin(callWeatherPlugin, parameters);
      }
   }
}
