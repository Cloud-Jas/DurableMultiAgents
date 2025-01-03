using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Interfaces;

namespace TravelService.MultiAgent.Orchestrator.Services
{
    public class FabricGraphQLService : IFabricGraphQLService
   {
      private readonly IFabricAuthService _authService;
      private readonly string _graphqlUri;

      public FabricGraphQLService(IConfiguration configuration, IFabricAuthService authService)
      {
         _authService = authService;
         _graphqlUri = configuration["Fabric:GraphQLUri"];
      }

      public async Task<dynamic> FetchBookingDetailsAsync(string passengerId, string flightId)
      {
         var token = await _authService.GetAccessTokenAsync();
         using var client = new GraphQLHttpClient(_graphqlUri, new NewtonsoftJsonSerializer())
         {
            HttpClient = { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", token) } }
         };
         client.Options.IsValidResponseToDeserialize = response => response.IsSuccessStatusCode;

         var query = new GraphQLHttpRequest
         {
            Variables = new { passengerId, flightId },
            Query = @"
                    query GetBookingDetails($passengerId: String!, $flightId: String!) {
                        passengers(filter: { Id: { eq: $passengerId } }) {
                            items { Id, FirstName, LastName, Email, Phone }
                        }
                        flightListings(filter: { FlightNumber: { eq: $flightId } }) {
                            items {
                                FlightId, DepartureAirportCode, DestinationAirportCode, Duration, FlightNumber,
                                DepartureTime, Price, AvailableSeats, AircraftType,
                                airlines { items { AirlineId, Name, Code, Country, City, LogoUrl } }
                            }
                        }
                    }"
         };

         var response = await client.SendQueryAsync<dynamic>(query);
         return response.Data;
      }
   }
}
