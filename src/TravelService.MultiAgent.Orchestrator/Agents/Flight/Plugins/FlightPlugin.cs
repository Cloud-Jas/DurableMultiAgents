using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using TravelService.MultiAgent.Orchestrator.Interfaces;
using TravelService.MultiAgent.Orchestrator.Services;
using TravelService.MultiAgent.Orchestrator.TracingDataHandlers;

namespace TravelService.MultiAgent.Orchestrator.Agents.Flight.Plugins
{
   public class FlightPlugin
   {
      private readonly IServiceProvider _serviceProvider;
      private readonly IPluginTracingHandler _pluginTracingHandler;
      public FlightPlugin(IServiceProvider serviceProvider)
      {
         _serviceProvider = serviceProvider;
         _pluginTracingHandler = serviceProvider.GetService<IPluginTracingHandler>() ?? throw new ArgumentNullException(nameof(IPluginTracingHandler));
      }

      [KernelFunction("GetAirportCode")]
      [Description("Provided the destination and departure city find the airport codes")]
      public async Task<string> GetAirportCodeAsync(
      [Description("Departure city")]
            string departureCity,
      [Description("Destination city")]
            string destinationCity)
      {

         var parameters = new Dictionary<string, string>
         {
            {"pluginName","GetAirpotCodePlugin" },
            { "departureCity", departureCity },
            { "destinationCity", destinationCity }
         };

         Func<Dictionary<string, string>, Task<string>> callAirportCodePlugin = async (inputs) =>
         {

            try
            {
               var departureCity = inputs["departureCity"];
               var destinationCity = inputs["destinationCity"];

               var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

               var airports = await cosmosService.FetchAirportDetailsAsync(departureCity, destinationCity);

               string response = "";

               var destinationAirport = airports.FirstOrDefault(a => string.Equals(a.city, destinationCity, StringComparison.OrdinalIgnoreCase));
               var departureAirport = airports.FirstOrDefault(a => string.Equals(a.city, departureCity, StringComparison.OrdinalIgnoreCase));

               response += $"Destination city: \"{destinationCity}\"  Destination Airport Code: \"{destinationAirport.code}\"  Destination Airport Name: \"{destinationAirport.name}\"\n";
               response += $"Departure city: \"{departureCity}\"  Departure Airport Code: \"{departureAirport.code}\"  Departure Airport Name: \"{departureAirport.name}\"\n";


               return response;
            }
            catch (Exception ex)
            {
               return "Error while looking for results!";
            }
         };

         return await _pluginTracingHandler.ExecutePlugin(callAirportCodePlugin, parameters);

      }

      [KernelFunction("GetFlightListings")]
      [Description("Get the list of flights available for the given airport codes and departure date range")]
      public async Task<string> GetDepartureFlightListingsAsync(
          [Description("Departure airport code")]
            string departureAirportCode,
          [Description("Destination airport code")]
            string destinationAirportCode,
          [Description("Departure date")]
            DateTime departureDate)
      {

         var parameters = new Dictionary<string, string>
         {
            {"pluginName", "GetDepartureFlightListingPlugin" },
            { "departureAirportCode", departureAirportCode },
            { "destinationAirportCode", destinationAirportCode },
            { "departureDate", departureDate.ToString() }
         };

         Func<Dictionary<string, string>, Task<string>> callFlightListingsPlugin = async (inputs) =>
         {
            try
            {
               var departureAirportCode = inputs["departureAirportCode"];
               var destinationAirportCode = inputs["destinationAirportCode"];
               var departureDate = DateTime.Parse(inputs["departureDate"]);

               var cosmosService = _serviceProvider.GetRequiredService<ICosmosClientService>();

               var flightListings = await cosmosService.FetchFlightListingsAsync(departureAirportCode, destinationAirportCode, departureDate);

               string response = "";

               foreach (var flight in flightListings)
               {
                  response += $"Flight Number: \"{flight.flightNumber}\"  Departure Time: \"{flight.departureTime}\"  Price: \"{flight.price}\"   Duration: \"{flight.duration}\"\n";
               }

               return response;
            }
            catch (Exception ex)
            {
               return "Error while looking for results!";
            }
         };

         return await _pluginTracingHandler.ExecutePlugin(callFlightListingsPlugin, parameters);
      }
   }
}
