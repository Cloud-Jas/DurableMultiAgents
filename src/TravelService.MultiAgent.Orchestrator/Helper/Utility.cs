using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Helper
{
    public static class Utility
    {       
        public static Array GetOrchestrators()
        {
            var orchestrators = new[] {
                new { name = "TravelOrchestrator", description = "Orchestrator for handling flight booking, weather check, vacation and trip planning related queries" },
                new {  name = "DefaultOrchestrator", description = "Orchestrator for handling queries related to Services/FAQs or booking history" }
            };

            return orchestrators;

        }
        public static Array GetSemanticLayers()
        {
            var containers = new[]
            {
                new {
                    name = "SemanticBookingLayer",
                    description = "Contains booking details with flight and passenger information denormalized."
                }
            };

            return containers;
        }
        public static Array GetVectorSemanticLayers()
        {
            var containers = new[]
            {
                new {
                    name = "SemanticBookingVectorLayer",
                    description = "Contains booking details with flight and passenger information denormalized and vectorized"
                }
            };

            return containers;
        }

        public static Array GetAgents()
        {
            var orchestrators = new[] {
                new { name = "TriggerFlightAgent", description = "A Flight Travel Agent assistant that helps users book flights, check flight status, and get information about flights."},
                new {  name = "TriggerWeatherAgent", description = "A Flight travel agent that helps customers with weather details before booking flights for their vacations."},
                new { name = "TriggerBookingAgent", description = "A Flight Booking Agent that helps customers find the best flight options for their vacations." }
            };

            return orchestrators;

        }

      public static List<string> GetAgentNames()
      {         
         return new List<string> { "FlightAgent", "WeatherAgent", "BookingAgent", "SemanticAgent", "VectorSearchAgent" };
      }


      public static Array GetDefaultAgents()
        {
            var orchestrators = new[] {
                new { name = "TriggerSemanticAgent", description = "Agent for handling semantic queries related to booking history"},
                new {  name = "TriggerFAQAgent", description = "Agent for handling queries related to FAQ and ContosoTravelAgency services related queries"}
            };

            return orchestrators;

        }
        public static List<string> GetOrchestratorNames()
        {
            var orchestrators = GetOrchestrators();
            var names = new List<string>();

            foreach (var orchestrator in orchestrators)
            {
                var nameProperty = orchestrator.GetType().GetProperty("name");
                if (nameProperty != null)
                {
                    var nameValue = nameProperty.GetValue(orchestrator)?.ToString();
                    if (!string.IsNullOrEmpty(nameValue))
                    {
                        names.Add(nameValue);
                    }
                }
            }

            return names;
        }
    }
}
