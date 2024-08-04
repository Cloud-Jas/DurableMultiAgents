using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace TravelService.MultiAgent.SeedData
{
    class Program
    {
        private static readonly string EndpointUri = "YOUR_ENDPOINT";
        private static readonly string PrimaryKey = "YOUR_KEY";
        private static readonly string DatabaseId = "ContosoTravelAgency";

        private static CosmosClient cosmosClient;
        private static Database database; 
        private static Container flightsContainer;
        private static Container airlinesContainer;
        private static Container bookingsContainer;
        private static Container passengersContainer;
        private static Container airportsContainer;
        private static Container paymentsContainer;
        private static Container weatherContainer;
        private static Container bookingVectorSemanticContainer;

        private static Container semanticBookingContainer;       
        static async Task Main(string[] args)
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            await CreateDatabaseAsync();
            flightsContainer = database.GetContainer("FlightListings");
            airlinesContainer = database.GetContainer("Airlines");
            bookingsContainer = database.GetContainer("Bookings");
            passengersContainer = database.GetContainer("Passengers");
            airportsContainer = database.GetContainer("Airports");
            paymentsContainer = database.GetContainer("Payments");
            weatherContainer = database.GetContainer("Weathers");
            semanticBookingContainer = database.GetContainer("SemanticBookingLayer");
            bookingVectorSemanticContainer = database.GetContainer("SemanticBookingVectorLayer");

            //await SeedDataAsync("Airlines", GetAirlinesData());
            //await SeedDataAsync("FlightListings", GetFlightListingsData());
            //await SeedDataAsync("Bookings", GetBookingsData());
            //await SeedDataAsync("Passengers", GetPassengersData());
            //await SeedDataAsync("Airports", GetAirportsData());
            //await SeedDataAsync("Payments", GetPaymentsData());
            //await SeedDataAsync("Weather", GetWeatherData());
            //await SeedDataAsync("Calendar", GetCalendarData());            
            //await CreateDenormalizedBookingDetails();
            //await CreateSemanticBookingVectorLayer();            
            Console.WriteLine("Data seeding completed.");
        }
        private static async Task CreateDenormalizedFlightDetailsWithAirline()
        {
            var query = "SELECT * FROM c";
            var flights = flightsContainer.GetItemQueryIterator<FlightListing>(new QueryDefinition(query));
            while (flights.HasMoreResults)
            {
                foreach (var flight in await flights.ReadNextAsync())
                {
                    var airline = await airlinesContainer.ReadItemAsync<Airline>(flight.airlineId, new PartitionKey(flight.airlineId));
                    var denormalizedFlight = new
                    {
                        flight.id,
                        flight.flightNumber,
                        flight.departure,
                        flight.destination,
                        flight.departureTime,
                        flight.price,
                        flight.description,
                        flight.aircraftType,
                        flight.availableSeats,
                        flight.duration,
                        airline = new
                        {
                            airline.Resource.id,
                            airline.Resource.name,
                            airline.Resource.code,
                            airline.Resource.city,
                            airline.Resource.country,
                            airline.Resource.logoUrl
                        }
                    };

                    await flightsContainer.UpsertItemAsync(denormalizedFlight);
                }
            }
        }

        private static async Task CreateDenormalizedBookingDetails()
        {
            var query = "SELECT * FROM c";
            var bookings = bookingsContainer.GetItemQueryIterator<Booking>(new QueryDefinition(query));
            while (bookings.HasMoreResults)
            {
                foreach (var booking in await bookings.ReadNextAsync())
                {
                    var passenger = await passengersContainer.ReadItemAsync<Passenger>(booking.passengerId, new PartitionKey(booking.passengerId));
                    var flightQuery = new QueryDefinition("SELECT * FROM c WHERE c.flightNumber = @flightNumber")
                                .WithParameter("@flightNumber", booking.flightId);
                    var flightIterator = flightsContainer.GetItemQueryIterator<FlightListing>(flightQuery);
                    FlightListing flight = null;
                    if (flightIterator.HasMoreResults)
                    {
                        var flightResult = await flightIterator.ReadNextAsync();
                        flight = flightResult.Resource.First();
                    }

                    if (flight == null)
                    {
                        return;
                    }

                    var airline = await airlinesContainer.ReadItemAsync<Airline>(flight.airlineId, new PartitionKey(flight.airlineId));

                    var denormalizedBooking = new
                    {
                        booking.id,
                        booking.bookingDate,
                        booking.status,
                        booking.seatNumber,
                        booking.pricePaid,
                        passenger = new
                        {
                            passenger.Resource.id,
                            passenger.Resource.firstName,
                            passenger.Resource.lastName,
                            passenger.Resource.email,
                            passenger.Resource.phone
                        },
                        flight = new
                        {
                            flight.id,
                            flight.flightNumber,
                            flight.departure,
                            flight.destination,
                            flight.departureTime,
                            flight.price,
                            flight.aircraftType,
                            flight.availableSeats,
                            flight.duration,
                            airline = new
                            {
                                airline.Resource.id,
                                airline.Resource.name,
                                airline.Resource.code,
                                airline.Resource.city,
                                airline.Resource.country,
                                airline.Resource.logoUrl
                            }
                        }
                    };

                    await semanticBookingContainer.UpsertItemAsync(denormalizedBooking);
                }
            }
        }   
        private static async Task CreateSemanticBookingVectorLayer()
        {
            Collection<Embedding> collection = new Collection<Embedding>(new List<Embedding>()
              {
                  new Embedding()
                  {
                      Path = "/vector",
                      DataType = VectorDataType.Float32,
                      DistanceFunction = DistanceFunction.Cosine,
                      Dimensions = 1536,
                  }
              });
            ContainerProperties properties = new ContainerProperties(id: "SemanticBookingVectorLayer", partitionKeyPath: "/id")
            {
                VectorEmbeddingPolicy = new(collection),
                IndexingPolicy = new IndexingPolicy()
                {
                    VectorIndexes = new()
            {
                new VectorIndexPath()
                {
                    Path = "/vector",
                    Type = VectorIndexType.QuantizedFlat,
                }
            }
                },
            };
            properties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/vector/*" });

            await database.CreateContainerIfNotExistsAsync(properties);
        }

        private static async Task CreateDenormalizedFlightDetailsWithAirports()
        {
            var query = "SELECT * FROM c";
            var flights = flightsContainer.GetItemQueryIterator<FlightListing>(new QueryDefinition(query));
            while (flights.HasMoreResults)
            {
                foreach (var flight in await flights.ReadNextAsync())
                {
                    var departureAirport = await airportsContainer.ReadItemAsync<Airport>(flight.departure, new PartitionKey(flight.departure));
                    var destinationAirport = await airportsContainer.ReadItemAsync<Airport>(flight.destination, new PartitionKey(flight.destination));
                    var denormalizedFlight = new
                    {
                        flight.id,
                        flight.flightNumber,
                        flight.departureTime,
                        flight.price,
                        flight.description,
                        flight.aircraftType,
                        flight.availableSeats,
                        flight.duration,
                        departureAirport = new
                        {
                            departureAirport.Resource.id,
                            departureAirport.Resource.name,
                            departureAirport.Resource.city,
                            departureAirport.Resource.country,
                            departureAirport.Resource.timezone
                        },
                        destinationAirport = new
                        {
                            destinationAirport.Resource.id,
                            destinationAirport.Resource.name,
                            destinationAirport.Resource.city,
                            destinationAirport.Resource.country,
                            destinationAirport.Resource.timezone
                        }
                    };

                    await flightsContainer.UpsertItemAsync(denormalizedFlight);
                }
            }
        }

        private static async Task CreateDenormalizedPaymentDetails()
        {
            var query = "SELECT * FROM c";
            var payments = paymentsContainer.GetItemQueryIterator<Payment>(new QueryDefinition(query));
            while (payments.HasMoreResults)
            {
                foreach (var payment in await payments.ReadNextAsync())
                {
                    var booking = await bookingsContainer.ReadItemAsync<Booking>(payment.bookingId, new PartitionKey(payment.bookingId));
                    var passenger = await passengersContainer.ReadItemAsync<Passenger>(booking.Resource.passengerId, new PartitionKey(booking.Resource.passengerId));
                    var flight = await flightsContainer.ReadItemAsync<FlightListing>(booking.Resource.flightId, new PartitionKey(booking.Resource.flightId));
                    var denormalizedPayment = new
                    {
                        payment.id,
                        payment.amount,
                        payment.currency,
                        payment.paymentMethod,
                        payment.paymentDate,
                        payment.status,
                        booking = new
                        {
                            booking.Resource.id,
                            booking.Resource.bookingDate,
                            booking.Resource.status,
                            booking.Resource.seatNumber,
                            booking.Resource.pricePaid,
                            passenger = new
                            {
                                passenger.Resource.id,
                                passenger.Resource.firstName,
                                passenger.Resource.lastName,
                                passenger.Resource.email,
                                passenger.Resource.phone
                            },
                            flight = new
                            {
                                flight.Resource.id,
                                flight.Resource.flightNumber,
                                flight.Resource.departure,
                                flight.Resource.destination,
                                flight.Resource.departureTime,
                                flight.Resource.price,
                                flight.Resource.aircraftType,
                                flight.Resource.availableSeats,
                                flight.Resource.duration
                            }
                        }
                    };

                    await paymentsContainer.UpsertItemAsync(denormalizedPayment);
                }
            }
        }

        private static async Task CreateDenormalizedFlightDetailsWithWeather()
        {
            var query = "SELECT * FROM c";
            var flights = flightsContainer.GetItemQueryIterator<FlightListing>(new QueryDefinition(query));
            while (flights.HasMoreResults)
            {
                foreach (var flight in await flights.ReadNextAsync())
                {
                    var departureWeather = await weatherContainer.ReadItemAsync<Weather>(flight.departure, new PartitionKey(flight.departure));
                    var arrivalWeather = await weatherContainer.ReadItemAsync<Weather>(flight.destination, new PartitionKey(flight.destination));
                    var denormalizedFlight = new
                    {
                        flight.id,
                        flight.flightNumber,
                        flight.departureTime,
                        flight.price,
                        flight.description,
                        flight.aircraftType,
                        flight.availableSeats,
                        flight.duration,
                        departureWeather = new
                        {
                            departureWeather.Resource.LocationId,
                            departureWeather.Resource.LocationName,
                            departureWeather.Resource.Country,
                            departureWeather.Resource.WeatherCondition,
                            departureWeather.Resource.TemperatureCelsius,
                            departureWeather.Resource.Humidity,
                            departureWeather.Resource.WindSpeedKmh
                        },
                        arrivalWeather = new
                        {
                            arrivalWeather.Resource.LocationId,
                            arrivalWeather.Resource.LocationName,
                            arrivalWeather.Resource.Country,
                            arrivalWeather.Resource.WeatherCondition,
                            arrivalWeather.Resource.TemperatureCelsius,
                            arrivalWeather.Resource.Humidity,
                            arrivalWeather.Resource.WindSpeedKmh
                        }
                    };

                    await flightsContainer.UpsertItemAsync(denormalizedFlight);
                }
            }
        }
        private static async Task CreateDatabaseAsync()
        {
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            Console.WriteLine($"Created Database: {database.Id}");
        }

        private static async Task SeedDataAsync<T>(string containerId, List<T> data)
        {
            Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/id");
            Console.WriteLine($"Created Container: {container.Id}");

            foreach (var item in data)
            {
                await container.CreateItemAsync(item, new PartitionKey(item.GetType().GetProperty("id").GetValue(item, null).ToString()));
                Console.WriteLine($"Created item in {containerId}: {item.GetType().GetProperty("name")?.GetValue(item, null)}");
            }
        }
        private static List<CalendarEvent> GetCalendarData()
        {
            return new List<CalendarEvent>
        {
            new CalendarEvent { id = "1", UserId = "P001", Title = "Conference", Description = "Tech conference in NYC.", StartDate = new DateTime(2024, 01, 15, 09, 00, 00), EndDate = new DateTime(2024, 01, 15, 17, 00, 00), Location = "New York", EventType = "Conference" },
            new CalendarEvent { id = "2", UserId = "P001", Title = "Workshop", Description = "AI workshop in San Francisco.", StartDate = new DateTime(2024, 02, 10, 10, 00, 00), EndDate = new DateTime(2024, 02, 10, 15, 00, 00), Location = "San Francisco", EventType = "Workshop" },
            new CalendarEvent { id = "3", UserId = "P002", Title = "Birthday Party", Description = "John's birthday celebration.", StartDate = new DateTime(2024, 03, 05, 18, 00, 00), EndDate = new DateTime(2024, 03, 05, 22, 00, 00), Location = "Los Angeles", EventType = "Birthday" },
            new CalendarEvent { id = "4", UserId = "P002", Title = "Meeting", Description = "Monthly team meeting.", StartDate = new DateTime(2024, 04, 20, 14, 00, 00), EndDate = new DateTime(2024, 04, 20, 16, 00, 00), Location = "Chicago", EventType = "Meeting" },
            new CalendarEvent { id = "5", UserId = "P001", Title = "Vacation", Description = "Family vacation in Hawaii.", StartDate = new DateTime(2024, 05, 01, 00, 00, 00), EndDate = new DateTime(2024, 05, 10, 23, 59, 00), Location = "Hawaii", EventType = "Vacation" },
            new CalendarEvent { id = "6", UserId = "P002", Title = "Annual Review", Description = "Yearly performance review.", StartDate = new DateTime(2024, 06, 15, 09, 00, 00), EndDate = new DateTime(2024, 06, 15, 11, 00, 00), Location = "Seattle", EventType = "Meeting" },
            new CalendarEvent { id = "7", UserId = "P001", Title = "Workshop", Description = "AI and machine learning workshop.", StartDate = new DateTime(2024, 07, 12, 09, 00, 00), EndDate = new DateTime(2024, 07, 12, 17, 00, 00), Location = "Boston", EventType = "Workshop" },
            new CalendarEvent { id = "8", UserId = "P002", Title = "Conference", Description = "Annual tech conference.", StartDate = new DateTime(2024, 08, 25, 10, 00, 00), EndDate = new DateTime(2024, 08, 25, 18, 00, 00), Location = "San Diego", EventType = "Conference" },
            new CalendarEvent { id = "9", UserId = "P001", Title = "Team Outing", Description = "Team outing to the zoo.", StartDate = new DateTime(2024, 09, 10, 11, 00, 00), EndDate = new DateTime(2024, 09, 10, 15, 00, 00), Location = "Philadelphia", EventType = "Meeting" },
            new CalendarEvent { id = "10", UserId = "P002", Title = "Holiday", Description = "Christmas holidays.", StartDate = new DateTime(2024, 12, 20, 00, 00, 00), EndDate = new DateTime(2024, 12, 26, 23, 59, 00), Location = "New York", EventType = "Vacation" },
            new CalendarEvent { id = "11", UserId = "P001", Title = "Training", Description = "Corporate training session.", StartDate = new DateTime(2025, 01, 10, 09, 00, 00), EndDate = new DateTime(2025, 01, 10, 12, 00, 00), Location = "Dallas", EventType = "Workshop" },
            new CalendarEvent { id = "12", UserId = "P002", Title = "Networking Event", Description = "Networking event for professionals.", StartDate = new DateTime(2025, 02, 18, 17, 00, 00), EndDate = new DateTime(2025, 02, 18, 20, 00, 00), Location = "Houston", EventType = "Meeting" },
            new CalendarEvent { id = "13", UserId = "P001", Title = "Client Meeting", Description = "Meeting with a client.", StartDate = new DateTime(2025, 03, 12, 10, 00, 00), EndDate = new DateTime(2025, 03, 12, 12, 00, 00), Location = "Atlanta", EventType = "Meeting" },
            new CalendarEvent { id = "14", UserId = "P002", Title = "Seminar", Description = "Seminar on industry trends.", StartDate = new DateTime(2025, 04, 22, 09, 00, 00), EndDate = new DateTime(2025, 04, 22, 17, 00, 00), Location = "Boston", EventType = "Workshop" },
            new CalendarEvent { id = "15", UserId = "P001", Title = "Family Reunion", Description = "Family reunion and gathering.", StartDate = new DateTime(2025, 05, 15, 11, 00, 00), EndDate = new DateTime(2025, 05, 15, 19, 00, 00), Location = "Chicago", EventType = "Birthday" },
            new CalendarEvent { id = "16", UserId = "P002", Title = "Trade Show", Description = "Annual trade show.", StartDate = new DateTime(2025, 06, 18, 10, 00, 00), EndDate = new DateTime(2025, 06, 18, 18, 00, 00), Location = "Los Angeles", EventType = "Conference" },
            new CalendarEvent { id = "17", UserId = "P001", Title = "Team Building", Description = "Team building activities.", StartDate = new DateTime(2025, 07, 25, 08, 00, 00), EndDate = new DateTime(2025, 07, 25, 16, 00, 00), Location = "Denver", EventType = "Meeting" },
            new CalendarEvent { id = "18", UserId = "P002", Title = "Annual Picnic", Description = "Company annual picnic.", StartDate = new DateTime(2025, 08, 10, 12, 00, 00), EndDate = new DateTime(2025, 08, 10, 17, 00, 00), Location = "San Francisco", EventType = "Meeting" },
            new CalendarEvent { id = "19", UserId = "P001", Title = "Music Festival", Description = "Attend a music festival.", StartDate = new DateTime(2025, 09, 05, 14, 00, 00), EndDate = new DateTime(2025, 09, 05, 23, 59, 00), Location = "Austin", EventType = "Vacation" },
            new CalendarEvent { id = "20", UserId = "P002", Title = "Health Checkup", Description = "Routine health checkup.", StartDate = new DateTime(2025, 10, 15, 09, 00, 00), EndDate = new DateTime(2025, 10, 15, 10, 00, 00), Location = "Seattle", EventType = "Meeting" },
            new CalendarEvent { id = "21", UserId = "P001", Title = "Workshop", Description = "Workshop on new technology.", StartDate = new DateTime(2025, 11, 05, 09, 00, 00), EndDate = new DateTime(2025, 11, 05, 16, 00, 00), Location = "Philadelphia", EventType = "Workshop" },
            new CalendarEvent { id = "22", UserId = "P002", Title = "Holiday", Description = "Thanksgiving holiday.", StartDate = new DateTime(2025, 11, 26, 00, 00, 00), EndDate = new DateTime(2025, 11, 30, 23, 59, 00), Location = "New York", EventType = "Vacation" },
            new CalendarEvent { id = "23", UserId = "P001", Title = "Christmas Party", Description = "Office Christmas party.", StartDate = new DateTime(2025, 12, 15, 18, 00, 00), EndDate = new DateTime(2025, 12, 15, 22, 00, 00), Location = "Chicago", EventType = "Birthday" },
            new CalendarEvent { id = "24", UserId = "P002", Title = "New Year's Eve", Description = "New Year's Eve celebration.", StartDate = new DateTime(2025, 12, 31, 20, 00, 00), EndDate = new DateTime(2025, 12, 31, 23, 59, 00), Location = "Los Angeles", EventType = "Holiday" },
            new CalendarEvent { id = "25", UserId = "P001", Title = "Business Trip", Description = "Business trip to New York.", StartDate = new DateTime(2024, 01, 20, 09, 00, 00), EndDate = new DateTime(2024, 01, 23, 17, 00, 00), Location = "New York", EventType = "Meeting" },
            new CalendarEvent { id = "26", UserId = "P002", Title = "Project Deadline", Description = "Deadline for project submission.", StartDate = new DateTime(2024, 02, 28, 23, 59, 00), EndDate = new DateTime(2024, 02, 28, 23, 59, 00), Location = "San Francisco", EventType = "Meeting" },
            new CalendarEvent { id = "27", UserId = "P001", Title = "Travel", Description = "Travel to Los Angeles.", StartDate = new DateTime(2024, 03, 12, 08, 00, 00), EndDate = new DateTime(2024, 03, 12, 20, 00, 00), Location = "Los Angeles", EventType = "Vacation" },
            new CalendarEvent { id = "28", UserId = "P002", Title = "Team Meeting", Description = "Weekly team meeting.", StartDate = new DateTime(2024, 04, 09, 10, 00, 00), EndDate = new DateTime(2024, 04, 09, 11, 00, 00), Location = "Seattle", EventType = "Meeting" },
            new CalendarEvent { id = "29", UserId = "P001", Title = "Holiday", Description = "Spring break holiday.", StartDate = new DateTime(2024, 05, 20, 00, 00, 00), EndDate = new DateTime(2024, 05, 25, 23, 59, 00), Location = "Miami", EventType = "Vacation" },
            new CalendarEvent { id = "30", UserId = "P002", Title = "Client Presentation", Description = "Presentation to a new client.", StartDate = new DateTime(2024, 06, 15, 13, 00, 00), EndDate = new DateTime(2024, 06, 15, 15, 00, 00), Location = "Chicago", EventType = "Meeting" }
        };
        }
        private static List<Airline> GetAirlinesData()
        {
            return new List<Airline>
    {
        new Airline { id = "1", name = "American Airlines", code = "AA", country = "USA", city = "Dallas", logoUrl = "http://example.com/logo_aa.png" },
        new Airline { id = "2", name = "Delta Airlines", code = "DL", country = "USA", city = "Atlanta", logoUrl = "http://example.com/logo_dl.png" },
        new Airline { id = "3", name = "United Airlines", code = "UA", country = "USA", city = "Chicago", logoUrl = "http://example.com/logo_ua.png" },
        new Airline { id = "4", name = "Air Canada", code = "AC", country = "Canada", city = "Toronto", logoUrl = "http://example.com/logo_ac.png" },
        new Airline { id = "5", name = "WestJet", code = "WS", country = "Canada", city = "Calgary", logoUrl = "http://example.com/logo_ws.png" },
        new Airline { id = "6", name = "British Airways", code = "BA", country = "UK", city = "London", logoUrl = "http://example.com/logo_ba.png" },
        new Airline { id = "7", name = "Virgin Atlantic", code = "VS", country = "UK", city = "London", logoUrl = "http://example.com/logo_vs.png" },
        new Airline { id = "8", name = "Lufthansa", code = "LH", country = "Germany", city = "Frankfurt", logoUrl = "http://example.com/logo_lh.png" },
        new Airline { id = "9", name = "Air France", code = "AF", country = "France", city = "Paris", logoUrl = "http://example.com/logo_af.png" },
        new Airline { id = "10", name = "KLM", code = "KL", country = "Netherlands", city = "Amsterdam", logoUrl = "http://example.com/logo_kl.png" },
        new Airline { id = "11", name = "Qantas", code = "QF", country = "Australia", city = "Sydney", logoUrl = "http://example.com/logo_qf.png" },
        new Airline { id = "12", name = "Air New Zealand", code = "NZ", country = "New Zealand", city = "Auckland", logoUrl = "http://example.com/logo_nz.png" },
        new Airline { id = "13", name = "Japan Airlines", code = "JL", country = "Japan", city = "Tokyo", logoUrl = "http://example.com/logo_jl.png" },
        new Airline { id = "14", name = "ANA", code = "NH", country = "Japan", city = "Tokyo", logoUrl = "http://example.com/logo_nh.png" },
        new Airline { id = "15", name = "Cathay Pacific", code = "CX", country = "Hong Kong", city = "Hong Kong", logoUrl = "http://example.com/logo_cx.png" },
        new Airline { id = "16", name = "Singapore Airlines", code = "SQ", country = "Singapore", city = "Singapore", logoUrl = "http://example.com/logo_sq.png" },
        new Airline { id = "17", name = "Emirates", code = "EK", country = "UAE", city = "Dubai", logoUrl = "http://example.com/logo_ek.png" },
        new Airline { id = "18", name = "Etihad Airways", code = "EY", country = "UAE", city = "Abu Dhabi", logoUrl = "http://example.com/logo_ey.png" },
        new Airline { id = "19", name = "Qatar Airways", code = "QR", country = "Qatar", city = "Doha", logoUrl = "http://example.com/logo_qr.png" },
        new Airline { id = "20", name = "Turkish Airlines", code = "TK", country = "Turkey", city = "Istanbul", logoUrl = "http://example.com/logo_tk.png" },
        new Airline { id = "21", name = "Aeroflot", code = "SU", country = "Russia", city = "Moscow", logoUrl = "http://example.com/logo_su.png" },
        new Airline { id = "22", name = "Air India", code = "AI", country = "India", city = "Mumbai", logoUrl = "http://example.com/logo_ai.png" },
        new Airline { id = "23", name = "IndiGo", code = "6E", country = "India", city = "Gurgaon", logoUrl = "http://example.com/logo_6e.png" },
        new Airline { id = "24", name = "China Southern", code = "CZ", country = "China", city = "Guangzhou", logoUrl = "http://example.com/logo_cz.png" },
        new Airline { id = "25", name = "China Eastern", code = "MU", country = "China", city = "Shanghai", logoUrl = "http://example.com/logo_mu.png" },
        new Airline { id = "26", name = "Korean Air", code = "KE", country = "South Korea", city = "Seoul", logoUrl = "http://example.com/logo_ke.png" },
        new Airline { id = "27", name = "Asiana Airlines", code = "OZ", country = "South Korea", city = "Seoul", logoUrl = "http://example.com/logo_oz.png" },
        new Airline { id = "28", name = "Thai Airways", code = "TG", country = "Thailand", city = "Bangkok", logoUrl = "http://example.com/logo_tg.png" },
        new Airline { id = "29", name = "Malaysia Airlines", code = "MH", country = "Malaysia", city = "Kuala Lumpur", logoUrl = "http://example.com/logo_mh.png" },
        new Airline { id = "30", name = "Garuda Indonesia", code = "GA", country = "Indonesia", city = "Jakarta", logoUrl = "http://example.com/logo_ga.png" },
        new Airline { id = "31", name = "SpiceJet", code = "SG", country = "India", city = "Chennai", logoUrl = "http://example.com/logo_sg.png" },
        new Airline { id = "32", name = "GoAir", code = "G8", country = "India", city = "Chennai", logoUrl = "http://example.com/logo_g8.png" },
        new Airline { id = "33", name = "Vistara", code = "UK", country = "India", city = "Chennai", logoUrl = "http://example.com/logo_uk.png" },
        new Airline { id = "34", name = "AirAsia India", code = "I5", country = "India", city = "Goa", logoUrl = "http://example.com/logo_i5.png" },
        new Airline { id = "35", name = "GoAir", code = "G8", country = "India", city = "Goa", logoUrl = "http://example.com/logo_g8_goa.png" },
        new Airline { id = "36", name = "SpiceJet", code = "SG", country = "India", city = "Goa", logoUrl = "http://example.com/logo_sg_goa.png" }
        };
        }
        private static List<Weather> GetWeatherData()
        {
            return new List<Weather>
    {                    
        // Delhi, India
        new Weather { id = "W001", LocationId = "DEL", LocationName = "Delhi", Country = "India", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Sunny", TemperatureCelsius = 35, Humidity = 60, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W002", LocationId = "DEL", LocationName = "Delhi", Country = "India", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 34, Humidity = 65, WindSpeedKmh = 12, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W003", LocationId = "DEL", LocationName = "Delhi", Country = "India", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Thunderstorms", TemperatureCelsius = 32, Humidity = 75, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W004", LocationId = "DEL", LocationName = "Delhi", Country = "India", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Rainy", TemperatureCelsius = 30, Humidity = 80, WindSpeedKmh = 25, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Mumbai, India
        new Weather { id = "W011", LocationId = "BOM", LocationName = "Mumbai", Country = "India", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Cloudy", TemperatureCelsius = 32, Humidity = 70, WindSpeedKmh = 10, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W012", LocationId = "BOM", LocationName = "Mumbai", Country = "India", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Rainy", TemperatureCelsius = 30, Humidity = 75, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W013", LocationId = "BOM", LocationName = "Mumbai", Country = "India", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Thunderstorms", TemperatureCelsius = 29, Humidity = 80, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W014", LocationId = "BOM", LocationName = "Mumbai", Country = "India", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Clear", TemperatureCelsius = 31, Humidity = 65, WindSpeedKmh = 12, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Bangalore, India
        new Weather { id = "W021", LocationId = "BLR", LocationName = "Bangalore", Country = "India", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Rainy", TemperatureCelsius = 28, Humidity = 65, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W022", LocationId = "BLR", LocationName = "Bangalore", Country = "India", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Cloudy", TemperatureCelsius = 27, Humidity = 68, WindSpeedKmh = 18, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W023", LocationId = "BLR", LocationName = "Bangalore", Country = "India", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Clear", TemperatureCelsius = 26, Humidity = 70, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W024", LocationId = "BLR", LocationName = "Bangalore", Country = "India", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 27, Humidity = 66, WindSpeedKmh = 10, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        new Weather { id = "W029", LocationId = "MAA", LocationName = "Chennai", Country = "India", StartDate = new DateTime(2024, 09, 10, 09, 00, 00, DateTimeKind.Utc), EndDate = new DateTime(2024, 09, 10, 12, 00, 00, DateTimeKind.Utc), WeatherCondition = "Thunderstorms", TemperatureCelsius = 30, Humidity = 85, WindSpeedKmh = 25, LastUpdated = new DateTime(2024, 09, 10, 08, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W030", LocationId = "MAA", LocationName = "Chennai", Country = "India", StartDate = new DateTime(2024, 09, 10, 14, 00, 00, DateTimeKind.Utc), EndDate = new DateTime(2024, 09, 10, 17, 00, 00, DateTimeKind.Utc), WeatherCondition = "Clear", TemperatureCelsius = 32, Humidity = 70, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 09, 10, 13, 00, 00, DateTimeKind.Utc) },

        new Weather { id = "W031", LocationId = "GOI", LocationName = "Goa", Country = "India", StartDate = new DateTime(2024, 09, 15, 08, 00, 00, DateTimeKind.Utc), EndDate = new DateTime(2024, 09, 15, 11, 00, 00, DateTimeKind.Utc), WeatherCondition = "Clear", TemperatureCelsius = 29, Humidity = 75, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 09, 15, 07, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W032", LocationId = "GOI", LocationName = "Goa", Country = "India", StartDate = new DateTime(2024, 09, 15, 13, 00, 00, DateTimeKind.Utc), EndDate = new DateTime(2024, 09, 15, 16, 00, 00, DateTimeKind.Utc), WeatherCondition = "Thunderstorms", TemperatureCelsius = 28, Humidity = 80, WindSpeedKmh = 30, LastUpdated = new DateTime(2024, 09, 15, 12, 00, 00, DateTimeKind.Utc) },

       
        // London, UK
        new Weather { id = "W041", LocationId = "LHR", LocationName = "London", Country = "UK", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Drizzling", TemperatureCelsius = 18, Humidity = 75, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W042", LocationId = "LHR", LocationName = "London", Country = "UK", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Cloudy", TemperatureCelsius = 19, Humidity = 70, WindSpeedKmh = 18, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W043", LocationId = "LHR", LocationName = "London", Country = "UK", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 20, Humidity = 65, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W044", LocationId = "LHR", LocationName = "London", Country = "UK", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Clear", TemperatureCelsius = 22, Humidity = 60, WindSpeedKmh = 12, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Tokyo, Japan
        new Weather { id = "W051", LocationId = "HND", LocationName = "Tokyo", Country = "Japan", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Sunny", TemperatureCelsius = 28, Humidity = 65, WindSpeedKmh = 10, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W052", LocationId = "HND", LocationName = "Tokyo", Country = "Japan", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 29, Humidity = 60, WindSpeedKmh = 12, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W053", LocationId = "HND", LocationName = "Tokyo", Country = "Japan", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Thunderstorms", TemperatureCelsius = 27, Humidity = 70, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W054", LocationId = "HND", LocationName = "Tokyo", Country = "Japan", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Rainy", TemperatureCelsius = 26, Humidity = 75, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Sydney, Australia
        new Weather { id = "W061", LocationId = "SYD", LocationName = "Sydney", Country = "Australia", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Clear", TemperatureCelsius = 22, Humidity = 50, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W062", LocationId = "SYD", LocationName = "Sydney", Country = "Australia", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 23, Humidity = 55, WindSpeedKmh = 10, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W063", LocationId = "SYD", LocationName = "Sydney", Country = "Australia", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Rainy", TemperatureCelsius = 21, Humidity = 60, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W064", LocationId = "SYD", LocationName = "Sydney", Country = "Australia", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Thunderstorms", TemperatureCelsius = 20, Humidity = 65, WindSpeedKmh = 25, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Adding more cities and weather data...

        // Cairo, Egypt
        new Weather { id = "W071", LocationId = "CAI", LocationName = "Cairo", Country = "Egypt", StartDate = new DateTime(2024, 07, 01), EndDate = new DateTime(2024, 07, 07), WeatherCondition = "Sunny", TemperatureCelsius = 38, Humidity = 20, WindSpeedKmh = 12, LastUpdated = new DateTime(2024, 07, 01, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W072", LocationId = "CAI", LocationName = "Cairo", Country = "Egypt", StartDate = new DateTime(2024, 07, 08), EndDate = new DateTime(2024, 07, 14), WeatherCondition = "Hot", TemperatureCelsius = 40, Humidity = 15, WindSpeedKmh = 10, LastUpdated = new DateTime(2024, 07, 08, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W073", LocationId = "CAI", LocationName = "Cairo", Country = "Egypt", StartDate = new DateTime(2024, 07, 15), EndDate = new DateTime(2024, 07, 21), WeatherCondition = "Partly Cloudy", TemperatureCelsius = 37, Humidity = 25, WindSpeedKmh = 15, LastUpdated = new DateTime(2024, 07, 15, 10, 00, 00, DateTimeKind.Utc) },
        new Weather { id = "W074", LocationId = "CAI", LocationName = "Cairo", Country = "Egypt", StartDate = new DateTime(2024, 07, 22), EndDate = new DateTime(2024, 07, 31), WeatherCondition = "Clear", TemperatureCelsius = 35, Humidity = 30, WindSpeedKmh = 20, LastUpdated = new DateTime(2024, 07, 22, 10, 00, 00, DateTimeKind.Utc) },

        // Adding more cities and weather data as needed...
    };
        }

        private static List<FlightListing> GetFlightListingsData()
        {
            return new List<FlightListing>
    {
     // August Flights
    new FlightListing { id = "FL001", flightNumber = "G8C001", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-01T10:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL002", flightNumber = "I5C001", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-01T15:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL003", flightNumber = "G8C002", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-05T10:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL004", flightNumber = "I5C002", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-05T15:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL005", flightNumber = "G8C003", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-10T10:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL006", flightNumber = "I5C003", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-10T15:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL007", flightNumber = "G8C004", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-15T10:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL008", flightNumber = "I5C004", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-15T15:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL009", flightNumber = "G8C005", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-20T10:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL010", flightNumber = "I5C005", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-20T15:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL011", flightNumber = "G8C006", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-08-25T10:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL012", flightNumber = "I5C006", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-08-25T15:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    // September Flights
    new FlightListing { id = "FL013", flightNumber = "G8C007", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-01T10:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL014", flightNumber = "I5C007", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-01T15:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL015", flightNumber = "G8C008", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-05T10:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL016", flightNumber = "I5C008", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-05T15:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL017", flightNumber = "G8C009", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-10T10:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL018", flightNumber = "I5C009", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-10T15:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL019", flightNumber = "G8C010", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-15T10:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL020", flightNumber = "I5C010", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-15T15:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL021", flightNumber = "G8C011", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-20T10:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL022", flightNumber = "I5C011", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-20T15:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL023", flightNumber = "G8C012", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-09-25T10:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL024", flightNumber = "I5C012", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-09-25T15:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    // October Flights
    new FlightListing { id = "FL025", flightNumber = "G8C013", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-01T10:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL026", flightNumber = "I5C013", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-01T15:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL027", flightNumber = "G8C014", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-05T10:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL028", flightNumber = "I5C014", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-05T15:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL029", flightNumber = "G8C015", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-10T10:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL030", flightNumber = "I5C015", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-10T15:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL031", flightNumber = "G8C016", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-15T10:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL032", flightNumber = "I5C016", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-15T15:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL033", flightNumber = "G8C017", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-20T10:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL034", flightNumber = "I5C017", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-20T15:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL035", flightNumber = "G8C018", airlineCode = "G8", departure = "MAA", destination = "GOI", departureTime = "2024-10-25T10:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL036", flightNumber = "I5C018", airlineCode = "I5", departure = "MAA", destination = "GOI", departureTime = "2024-10-25T15:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL037", flightNumber = "G8G001", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-01T09:00:00Z", price = "210.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL038", flightNumber = "I5G001", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-01T14:00:00Z", price = "200.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL039", flightNumber = "G8G002", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-05T09:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL040", flightNumber = "I5G002", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-05T14:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL041", flightNumber = "G8G003", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-10T09:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL042", flightNumber = "I5G003", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-10T14:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL043", flightNumber = "G8G004", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-15T09:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL044", flightNumber = "I5G004", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-15T14:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL045", flightNumber = "G8G005", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-20T09:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL046", flightNumber = "I5G005", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-20T14:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL047", flightNumber = "G8G006", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-25T09:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL048", flightNumber = "I5G006", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-25T14:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL049", flightNumber = "G8G007", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-08-30T09:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL050", flightNumber = "I5G007", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-08-30T14:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    // September Flights
    new FlightListing { id = "FL051", flightNumber = "G8G008", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-01T09:00:00Z", price = "210.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL052", flightNumber = "I5G008", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-01T14:00:00Z", price = "200.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL053", flightNumber = "G8G009", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-05T09:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL054", flightNumber = "I5G009", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-05T14:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL055", flightNumber = "G8G010", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-10T09:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL056", flightNumber = "I5G010", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-10T14:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL057", flightNumber = "G8G011", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-15T09:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL058", flightNumber = "I5G011", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-15T14:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL059", flightNumber = "G8G012", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-20T09:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL060", flightNumber = "I5G012", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-20T14:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL061", flightNumber = "G8G013", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-25T09:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL062", flightNumber = "I5G013", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-25T14:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL063", flightNumber = "G8G014", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-09-30T09:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL064", flightNumber = "I5G014", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-09-30T14:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    // October Flights
    new FlightListing { id = "FL065", flightNumber = "G8G015", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-01T09:00:00Z", price = "210.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL066", flightNumber = "I5G015", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-01T14:00:00Z", price = "200.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL067", flightNumber = "G8G016", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-05T09:00:00Z", price = "220.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL068", flightNumber = "I5G016", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-05T14:00:00Z", price = "210.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL069", flightNumber = "G8G017", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-10T09:00:00Z", price = "230.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL070", flightNumber = "I5G017", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-10T14:00:00Z", price = "220.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL071", flightNumber = "G8G018", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-15T09:00:00Z", price = "240.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL072", flightNumber = "I5G018", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-15T14:00:00Z", price = "230.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL073", flightNumber = "G8G019", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-20T09:00:00Z", price = "250.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL074", flightNumber = "I5G019", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-20T14:00:00Z", price = "240.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL075", flightNumber = "G8G020", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-25T09:00:00Z", price = "260.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL076", flightNumber = "I5G020", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-25T14:00:00Z", price = "250.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },

    new FlightListing { id = "FL077", flightNumber = "G8G021", airlineCode = "G8", departure = "GOI", destination = "MAA", departureTime = "2024-10-30T09:00:00Z", price = "270.00", description = "one-way", airlineId = "35", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" },
    new FlightListing { id = "FL078", flightNumber = "I5G021", airlineCode = "I5", departure = "GOI", destination = "MAA", departureTime = "2024-10-30T14:00:00Z", price = "260.00", description = "one-way", airlineId = "34", aircraftType = "Airbus A320", availableSeats = 150, duration = "1.5h" }


    };
        }

        private static List<Booking> GetBookingsData()
        {
            return new List<Booking>
    {
        new Booking { id = "B001", passengerId = "P001", flightId = "FL001", bookingDate = "2024-07-20T10:00:00Z", status = "confirmed" },
        new Booking { id = "B002", passengerId = "P002", flightId = "FL002", bookingDate = "2024-07-21T11:00:00Z", status = "confirmed" },
        new Booking { id = "B003", passengerId = "P003", flightId = "FL003", bookingDate = "2024-07-22T12:00:00Z", status = "pending" },
        new Booking { id = "B004", passengerId = "P004", flightId = "FL004", bookingDate = "2024-07-23T13:00:00Z", status = "confirmed" },
        new Booking { id = "B005", passengerId = "P005", flightId = "FL005", bookingDate = "2024-07-24T14:00:00Z", status = "cancelled" },
        new Booking { id = "B006", passengerId = "P006", flightId = "FL006", bookingDate = "2024-07-25T15:00:00Z", status = "confirmed" },
        new Booking { id = "B007", passengerId = "P007", flightId = "FL007", bookingDate = "2024-07-26T16:00:00Z", status = "confirmed" },
        new Booking { id = "B008", passengerId = "P008", flightId = "FL008", bookingDate = "2024-07-27T17:00:00Z", status = "pending" },
        new Booking { id = "B009", passengerId = "P009", flightId = "FL009", bookingDate = "2024-07-28T18:00:00Z", status = "confirmed" },
        new Booking { id = "B010", passengerId = "P010", flightId = "FL010", bookingDate = "2024-07-29T19:00:00Z", status = "confirmed" },
        new Booking { id = "B011", passengerId = "P011", flightId = "FL011", bookingDate = "2024-07-30T20:00:00Z", status = "pending" },
        new Booking { id = "B012", passengerId = "P012", flightId = "FL012", bookingDate = "2024-07-31T21:00:00Z", status = "confirmed" },
        new Booking { id = "B013", passengerId = "P013", flightId = "FL013", bookingDate = "2024-08-01T22:00:00Z", status = "cancelled" },
        new Booking { id = "B014", passengerId = "P014", flightId = "FL014", bookingDate = "2024-08-02T23:00:00Z", status = "confirmed" },
        new Booking { id = "B015", passengerId = "P015", flightId = "FL015", bookingDate = "2024-08-03T00:00:00Z", status = "confirmed" },
        new Booking { id = "B016", passengerId = "P016", flightId = "FL016", bookingDate = "2024-08-04T01:00:00Z", status = "pending" },
        new Booking { id = "B017", passengerId = "P017", flightId = "FL017", bookingDate = "2024-08-05T02:00:00Z", status = "confirmed" },
        new Booking { id = "B018", passengerId = "P018", flightId = "FL018", bookingDate = "2024-08-06T03:00:00Z", status = "cancelled" },
        new Booking { id = "B019", passengerId = "P019", flightId = "FL019", bookingDate = "2024-08-07T04:00:00Z", status = "confirmed" },
        new Booking { id = "B020", passengerId = "P020", flightId = "FL020", bookingDate = "2024-08-08T05:00:00Z", status = "pending" }
    };
        }

        private static List<Passenger> GetPassengersData()
        {
            return new List<Passenger>
    {
        new Passenger { id = "P001", firstName = "John", lastName = "Doe", email = "john.doe@example.com", phone = "123-456-7890", passportNumber = "A12345678", nationality = "USA", dob = "1985-05-15", frequentFlyerNumber = "FF12345" },
        new Passenger { id = "P002", firstName = "Jane", lastName = "Smith", email = "jane.smith@example.com", phone = "098-765-4321", passportNumber = "B87654321", nationality = "Canada", dob = "1990-07-10", frequentFlyerNumber = "FF67890" },
        new Passenger { id = "P003", firstName = "Raj", lastName = "Patel", email = "raj.patel@example.com", phone = "987-654-3210", passportNumber = "C12345678", nationality = "India", dob = "1988-03-22", frequentFlyerNumber = "FF11223" },
        new Passenger { id = "P004", firstName = "Aarti", lastName = "Sharma", email = "aarti.sharma@example.com", phone = "876-543-2109", passportNumber = "D87654321", nationality = "India", dob = "1992-12-05", frequentFlyerNumber = "FF22334" },
        new Passenger { id = "P005", firstName = "Michael", lastName = "Johnson", email = "michael.johnson@example.com", phone = "654-321-0987", passportNumber = "E23456789", nationality = "USA", dob = "1982-11-30", frequentFlyerNumber = "FF33445" },
        new Passenger { id = "P006", firstName = "Emily", lastName = "Davis", email = "emily.davis@example.com", phone = "543-210-9876", passportNumber = "F34567890", nationality = "UK", dob = "1995-01-25", frequentFlyerNumber = "FF44556" },
        new Passenger { id = "P007", firstName = "Hiroshi", lastName = "Tanaka", email = "hiroshi.tanaka@example.com", phone = "321-654-9870", passportNumber = "G45678901", nationality = "Japan", dob = "1987-09-14", frequentFlyerNumber = "FF55667" },
        new Passenger { id = "P008", firstName = "Maria", lastName = "Gonzalez", email = "maria.gonzalez@example.com", phone = "210-987-6543", passportNumber = "H56789012", nationality = "Spain", dob = "1991-06-20", frequentFlyerNumber = "FF66778" },
        new Passenger { id = "P009", firstName = "Luca", lastName = "Rossi", email = "luca.rossi@example.com", phone = "789-012-3456", passportNumber = "I67890123", nationality = "Italy", dob = "1983-04-10", frequentFlyerNumber = "FF77889" },
        new Passenger { id = "P010", firstName = "Sofia", lastName = "Miller", email = "sofia.miller@example.com", phone = "890-123-4567", passportNumber = "J78901234", nationality = "Germany", dob = "1996-08-30", frequentFlyerNumber = "FF88990" },
        new Passenger { id = "P011", firstName = "Anil", lastName = "Kumar", email = "anil.kumar@example.com", phone = "345-678-9012", passportNumber = "K89012345", nationality = "India", dob = "1989-07-21", frequentFlyerNumber = "FF99001" },
        new Passenger { id = "P012", firstName = "Divakar", lastName = "Kumar", email = "divakark1805@gmail.com", phone = "+918754408261", passportNumber = "L90123456", nationality = "India", dob = "1994-05-18", frequentFlyerNumber = "FF10012" },
        new Passenger { id = "P013", firstName = "Chen", lastName = "Li", email = "chen.li@example.com", phone = "567-890-1234", passportNumber = "M01234567", nationality = "China", dob = "1980-11-05", frequentFlyerNumber = "FF11023" },
        new Passenger { id = "P014", firstName = "Omar", lastName = "Ahmed", email = "omar.ahmed@example.com", phone = "678-901-2345", passportNumber = "N12345678", nationality = "Egypt", dob = "1986-10-15", frequentFlyerNumber = "FF12034" },
        new Passenger { id = "P015", firstName = "Olivia", lastName = "Brown", email = "olivia.brown@example.com", phone = "789-012-3456", passportNumber = "O23456789", nationality = "Australia", dob = "1993-03-05", frequentFlyerNumber = "FF13045" },
        new Passenger { id = "P016", firstName = "Lucas", lastName = "Wilson", email = "lucas.wilson@example.com", phone = "890-123-4567", passportNumber = "P34567890", nationality = "USA", dob = "1987-12-20", frequentFlyerNumber = "FF14056" },
        new Passenger { id = "P017", firstName = "Mia", lastName = "Anderson", email = "mia.anderson@example.com", phone = "901-234-5678", passportNumber = "Q45678901", nationality = "Canada", dob = "1991-09-25", frequentFlyerNumber = "FF15067" },
        new Passenger { id = "P018", firstName = "Jorge", lastName = "Martinez", email = "jorge.martinez@example.com", phone = "012-345-6789", passportNumber = "R56789012", nationality = "Mexico", dob = "1985-06-30", frequentFlyerNumber = "FF16078" },
        new Passenger { id = "P019", firstName = "Emma", lastName = "Taylor", email = "emma.taylor@example.com", phone = "123-456-7891", passportNumber = "S67890123", nationality = "UK", dob = "1994-01-15", frequentFlyerNumber = "FF17089" },
        new Passenger { id = "P020", firstName = "Carlos", lastName = "Lopez", email = "carlos.lopez@example.com", phone = "234-567-8901", passportNumber = "T78901234", nationality = "Argentina", dob = "1988-04-10", frequentFlyerNumber = "FF18090" },
        new Passenger { id = "P021", firstName = "Ravi", lastName = "Singh", email = "ravi.singh@example.com", phone = "345-678-9012", passportNumber = "U89012345", nationality = "India", dob = "1981-08-25", frequentFlyerNumber = "FF19001" },
        new Passenger { id = "P022", firstName = "Meera", lastName = "Nair", email = "meera.nair@example.com", phone = "456-789-0123", passportNumber = "V90123456", nationality = "India", dob = "1993-12-10", frequentFlyerNumber = "FF20012" },
        new Passenger { id = "P023", firstName = "Hans", lastName = "Schmidt", email = "hans.schmidt@example.com", phone = "567-890-1234", passportNumber = "W01234567", nationality = "Germany", dob = "1979-11-05", frequentFlyerNumber = "FF21023" },
        new Passenger { id = "P024", firstName = "Fatima", lastName = "Ali", email = "fatima.ali@example.com", phone = "678-901-2345", passportNumber = "X12345678", nationality = "Pakistan", dob = "1985-03-15", frequentFlyerNumber = "FF22034" },
        new Passenger { id = "P025", firstName = "Olaf", lastName = "Johansson", email = "olaf.johansson@example.com", phone = "789-012-3456", passportNumber = "Y23456789", nationality = "Sweden", dob = "1992-07-10", frequentFlyerNumber = "FF23045" },
        new Passenger { id = "P026", firstName = "Julia", lastName = "Klein", email = "julia.klein@example.com", phone = "890-123-4567", passportNumber = "Z34567890", nationality = "Germany", dob = "1994-01-20", frequentFlyerNumber = "FF24056" },
        new Passenger { id = "P027", firstName = "Nina", lastName = "Petrov", email = "nina.petrov@example.com", phone = "901-234-5678", passportNumber = "A45678901", nationality = "Russia", dob = "1988-09-15", frequentFlyerNumber = "FF25067" },
        new Passenger { id = "P028", firstName = "David", lastName = "Morris", email = "david.morris@example.com", phone = "012-345-6789", passportNumber = "B56789012", nationality = "USA", dob = "1984-06-30", frequentFlyerNumber = "FF26078" },
        new Passenger { id = "P029", firstName = "Anna", lastName = "Wang", email = "anna.wang@example.com", phone = "123-456-7892", passportNumber = "C67890123", nationality = "China", dob = "1991-10-22", frequentFlyerNumber = "FF27089" },
        new Passenger { id = "P030", firstName = "Amir", lastName = "Khan", email = "amir.khan@example.com", phone = "234-567-8902", passportNumber = "D78901234", nationality = "Pakistan", dob = "1983-12-05", frequentFlyerNumber = "FF28090" },
        new Passenger { id = "P031", firstName = "Laura", lastName = "Fisher", email = "laura.fisher@example.com", phone = "345-678-9013", passportNumber = "E89012345", nationality = "Canada", dob = "1995-11-10", frequentFlyerNumber = "FF29001" },
        new Passenger { id = "P032", firstName = "James", lastName = "Lee", email = "james.lee@example.com", phone = "456-789-0124", passportNumber = "F90123456", nationality = "USA", dob = "1980-02-17", frequentFlyerNumber = "FF30012" },
        new Passenger { id = "P033", firstName = "Fatima", lastName = "Hassan", email = "fatima.hassan@example.com", phone = "567-890-1235", passportNumber = "G01234567", nationality = "Egypt", dob = "1987-05-21", frequentFlyerNumber = "FF31023" },
        new Passenger { id = "P034", firstName = "Derek", lastName = "Brown", email = "derek.brown@example.com", phone = "678-901-2346", passportNumber = "H12345678", nationality = "USA", dob = "1992-09-30", frequentFlyerNumber = "FF32034" },
        new Passenger { id = "P035", firstName = "Maya", lastName = "Singh", email = "maya.singh@example.com", phone = "789-012-3457", passportNumber = "I23456789", nationality = "India", dob = "1986-07-15", frequentFlyerNumber = "FF33045" },
        new Passenger { id = "P036", firstName = "Sandeep", lastName = "Mehta", email = "sandeep.mehta@example.com", phone = "890-123-4568", passportNumber = "J34567890", nationality = "India", dob = "1990-12-22", frequentFlyerNumber = "FF34056" },
        new Passenger { id = "P037", firstName = "Elena", lastName = "Martinez", email = "elena.martinez@example.com", phone = "901-234-5679", passportNumber = "K45678901", nationality = "Spain", dob = "1985-03-17", frequentFlyerNumber = "FF35067" },
        new Passenger { id = "P038", firstName = "Oliver", lastName = "Miller", email = "oliver.miller@example.com", phone = "012-345-6780", passportNumber = "L56789012", nationality = "UK", dob = "1991-11-23", frequentFlyerNumber = "FF36078" },
        new Passenger { id = "P039", firstName = "Ingrid", lastName = "Larsson", email = "ingrid.larsson@example.com", phone = "123-456-7893", passportNumber = "M67890123", nationality = "Sweden", dob = "1983-09-05", frequentFlyerNumber = "FF37089" },
        new Passenger { id = "P040", firstName = "Sophia", lastName = "Wilson", email = "sophia.wilson@example.com", phone = "234-567-8903", passportNumber = "N78901234", nationality = "Australia", dob = "1994-06-12", frequentFlyerNumber = "FF38090" }
    };
        }

        private static List<Airport> GetAirportsData()
        {
            return new List<Airport>
    {
        new Airport { id = "1", code = "JFK", name = "John F. Kennedy International Airport", city = "New York", country = "USA" },
        new Airport { id = "2", code = "LAX", name = "Los Angeles International Airport", city = "Los Angeles", country = "USA" },
        new Airport { id = "3", code = "ORD", name = "O'Hare International Airport", city = "Chicago", country = "USA" },
        new Airport { id = "4", code = "DFW", name = "Dallas/Fort Worth International Airport", city = "Dallas", country = "USA" },
        new Airport { id = "5", code = "ATL", name = "Hartsfield-Jackson Atlanta International Airport", city = "Atlanta", country = "USA" },
        new Airport { id = "6", code = "BOM", name = "Chhatrapati Shivaji Maharaj International Airport", city = "Mumbai", country = "India" },
        new Airport { id = "7", code = "DEL", name = "Indira Gandhi International Airport", city = "Delhi", country = "India" },
        new Airport { id = "8", code = "BLR", name = "Kempegowda International Airport", city = "Bangalore", country = "India" },
        new Airport { id = "9", code = "HYD", name = "Rajiv Gandhi International Airport", city = "Hyderabad", country = "India" },
        new Airport { id = "10", code = "MAA", name = "Chennai International Airport", city = "Chennai", country = "India" },
        new Airport { id = "11", code = "GOI", name = "Goa International Airport", city = "Goa", country = "India" },
        new Airport { id = "12", code = "SIN", name = "Singapore Changi Airport", city = "Singapore", country = "Singapore" },
        new Airport { id = "13", code = "HKG", name = "Hong Kong International Airport", city = "Hong Kong", country = "Hong Kong" },
        new Airport { id = "14", code = "HND", name = "Haneda Airport", city = "Tokyo", country = "Japan" },
        new Airport { id = "15", code = "NRT", name = "Narita International Airport", city = "Tokyo", country = "Japan" },
        new Airport { id = "16", code = "ICN", name = "Incheon International Airport", city = "Seoul", country = "South Korea" },
        new Airport { id = "17", code = "PEK", name = "Beijing Capital International Airport", city = "Beijing", country = "China" },
        new Airport { id = "18", code = "PVG", name = "Shanghai Pudong International Airport", city = "Shanghai", country = "China" },
        new Airport { id = "19", code = "DXB", name = "Dubai International Airport", city = "Dubai", country = "United Arab Emirates" },
        new Airport { id = "20", code = "AUH", name = "Abu Dhabi International Airport", city = "Abu Dhabi", country = "United Arab Emirates" },
        new Airport { id = "21", code = "JNB", name = "O.R. Tambo International Airport", city = "Johannesburg", country = "South Africa" },
        new Airport { id = "22", code = "CPT", name = "Cape Town International Airport", city = "Cape Town", country = "South Africa" },
        new Airport { id = "23", code = "SYD", name = "Sydney Kingsford Smith Airport", city = "Sydney", country = "Australia" },
        new Airport { id = "24", code = "MEL", name = "Melbourne Airport", city = "Melbourne", country = "Australia" },
        new Airport { id = "25", code = "LHR", name = "Heathrow Airport", city = "London", country = "United Kingdom" },
        new Airport { id = "26", code = "LGW", name = "Gatwick Airport", city = "London", country = "United Kingdom" },
        new Airport { id = "27", code = "CDG", name = "Charles de Gaulle Airport", city = "Paris", country = "France" },
        new Airport { id = "28", code = "ORY", name = "Orly Airport", city = "Paris", country = "France" },
        new Airport { id = "29", code = "FRA", name = "Frankfurt Airport", city = "Frankfurt", country = "Germany" },
        new Airport { id = "30", code = "MUC", name = "Munich Airport", city = "Munich", country = "Germany" },
        new Airport { id = "31", code = "MAD", name = "Adolfo Suárez Madrid-Barajas Airport", city = "Madrid", country = "Spain" },
        new Airport { id = "32", code = "BCN", name = "Barcelona-El Prat Airport", city = "Barcelona", country = "Spain" },
        new Airport { id = "33", code = "AMS", name = "Amsterdam Airport Schiphol", city = "Amsterdam", country = "Netherlands" },
        new Airport { id = "34", code = "ZRH", name = "Zurich Airport", city = "Zurich", country = "Switzerland" },
        new Airport { id = "35", code = "VIE", name = "Vienna International Airport", city = "Vienna", country = "Austria" },
        new Airport { id = "36", code = "LIS", name = "Humberto Delgado Airport", city = "Lisbon", country = "Portugal" },
        new Airport { id = "37", code = "IST", name = "Istanbul Airport", city = "Istanbul", country = "Turkey" },
        new Airport { id = "38", code = "TLV", name = "Ben Gurion Airport", city = "Tel Aviv", country = "Israel" },
        new Airport { id = "39", code = "CAI", name = "Cairo International Airport", city = "Cairo", country = "Egypt" },
        new Airport { id = "40", code = "DOH", name = "Hamad International Airport", city = "Doha", country = "Qatar" },
        new Airport { id = "41", code = "RUH", name = "King Khalid International Airport", city = "Riyadh", country = "Saudi Arabia" },
        new Airport { id = "42", code = "BKK", name = "Suvarnabhumi Airport", city = "Bangkok", country = "Thailand" },
        new Airport { id = "43", code = "KUL", name = "Kuala Lumpur International Airport", city = "Kuala Lumpur", country = "Malaysia" },
        new Airport { id = "44", code = "HKT", name = "Phuket International Airport", city = "Phuket", country = "Thailand" },
        new Airport { id = "45", code = "KIX", name = "Kansai International Airport", city = "Osaka", country = "Japan" },
        new Airport { id = "46", code = "NKG", name = "Nanjing Lukou International Airport", city = "Nanjing", country = "China" },
        new Airport { id = "47", code = "SFO", name = "San Francisco International Airport", city = "San Francisco", country = "USA" },
        new Airport { id = "48", code = "LAS", name = "McCarran International Airport", city = "Las Vegas", country = "USA" },
        new Airport { id = "49", code = "YVR", name = "Vancouver International Airport", city = "Vancouver", country = "Canada" },
        new Airport { id = "50", code = "YYZ", name = "Toronto Pearson International Airport", city = "Toronto", country = "Canada" },
        new Airport { id = "51", code = "MIA", name = "Miami International Airport", city = "Miami", country = "USA" },
        new Airport { id = "52", code = "BNA", name = "Nashville International Airport", city = "Nashville", country = "USA" },
        new Airport { id = "53", code = "SAN", name = "San Diego International Airport", city = "San Diego", country = "USA" },
        new Airport { id = "54", code = "SEA", name = "Seattle-Tacoma International Airport", city = "Seattle", country = "USA" },
        new Airport { id = "55", code = "DEN", name = "Denver International Airport", city = "Denver", country = "USA" },
        new Airport { id = "56", code = "JNB", name = "O.R. Tambo International Airport", city = "Johannesburg", country = "South Africa" },
        new Airport { id = "57", code = "LOS", name = "Murtala Muhammed International Airport", city = "Lagos", country = "Nigeria" },
        new Airport { id = "58", code = "KWI", name = "Kuwait International Airport", city = "Kuwait City", country = "Kuwait" },
        new Airport { id = "59", code = "TUN", name = "Tunis-Carthage International Airport", city = "Tunis", country = "Tunisia" },
        new Airport { id = "60", code = "AMM", name = "Queen Alia International Airport", city = "Amman", country = "Jordan" },
        new Airport { id = "61", code = "HKG", name = "Hong Kong International Airport", city = "Hong Kong", country = "Hong Kong" },
        new Airport { id = "62", code = "TPE", name = "Taipei Taoyuan International Airport", city = "Taipei", country = "Taiwan" },
        new Airport { id = "63", code = "SGN", name = "Tan Son Nhat International Airport", city = "Ho Chi Minh City", country = "Vietnam" },
        new Airport { id = "64", code = "HKT", name = "Phuket International Airport", city = "Phuket", country = "Thailand" },
        new Airport { id = "65", code = "CMB", name = "Bandaranaike International Airport", city = "Colombo", country = "Sri Lanka" },
        new Airport { id = "66", code = "CJU", name = "Jeju International Airport", city = "Jeju", country = "South Korea" },
        new Airport { id = "67", code = "KHH", name = "Kaohsiung International Airport", city = "Kaohsiung", country = "Taiwan" },
        new Airport { id = "68", code = "YUL", name = "Montréal-Pierre Elliott Trudeau International Airport", city = "Montreal", country = "Canada" },
        new Airport { id = "69", code = "YYC", name = "Calgary International Airport", city = "Calgary", country = "Canada" },
        new Airport { id = "70", code = "WAW", name = "Warsaw Chopin Airport", city = "Warsaw", country = "Poland" },
        new Airport { id = "71", code = "PRG", name = "Václav Havel Airport Prague", city = "Prague", country = "Czech Republic" },
        new Airport { id = "72", code = "BRU", name = "Brussels Airport", city = "Brussels", country = "Belgium" },
        new Airport { id = "73", code = "DTW", name = "Detroit Metropolitan Airport", city = "Detroit", country = "USA" },
        new Airport { id = "74", code = "CLT", name = "Charlotte Douglas International Airport", city = "Charlotte", country = "USA" },
        new Airport { id = "75", code = "SVO", name = "Sheremetyevo International Airport", city = "Moscow", country = "Russia" },
        new Airport { id = "76", code = "LED", name = "Pulkovo Airport", city = "Saint Petersburg", country = "Russia" },
        new Airport { id = "77", code = "TLV", name = "Ben Gurion International Airport", city = "Tel Aviv", country = "Israel" },
        new Airport { id = "78", code = "EWR", name = "Newark Liberty International Airport", city = "Newark", country = "USA" },
        new Airport { id = "79", code = "TPE", name = "Taipei Taoyuan International Airport", city = "Taipei", country = "Taiwan" },
        new Airport { id = "80", code = "GIG", name = "Galeão International Airport", city = "Rio de Janeiro", country = "Brazil" },
        new Airport { id = "81", code = "GRU", name = "São Paulo/Guarulhos–Governador André Franco Montoro International Airport", city = "São Paulo", country = "Brazil" },
        new Airport { id = "82", code = "MEX", name = "Mexico City International Airport", city = "Mexico City", country = "Mexico" },
        new Airport { id = "83", code = "BCN", name = "Barcelona-El Prat Airport", city = "Barcelona", country = "Spain" },
        new Airport { id = "84", code = "DUB", name = "Dublin Airport", city = "Dublin", country = "Ireland" },
        new Airport { id = "85", code = "GVA", name = "Geneva Airport", city = "Geneva", country = "Switzerland" },
        new Airport { id = "86", code = "ARN", name = "Stockholm Arlanda Airport", city = "Stockholm", country = "Sweden" },
        new Airport { id = "87", code = "BUD", name = "Budapest Ferenc Liszt International Airport", city = "Budapest", country = "Hungary" },
        new Airport { id = "88", code = "PRG", name = "Václav Havel Airport Prague", city = "Prague", country = "Czech Republic" },
        new Airport { id = "89", code = "TUN", name = "Tunis-Carthage International Airport", city = "Tunis", country = "Tunisia" },
        new Airport { id = "90", code = "AGP", name = "Málaga Airport", city = "Malaga", country = "Spain" },
        new Airport { id = "91", code = "NCE", name = "Nice Côte d'Azur Airport", city = "Nice", country = "France" },
        new Airport { id = "92", code = "OPO", name = "Francisco Sá Carneiro Airport", city = "Porto", country = "Portugal" },
        new Airport { id = "93", code = "FCO", name = "Leonardo da Vinci International Airport", city = "Rome", country = "Italy" },
        new Airport { id = "94", code = "LIS", name = "Humberto Delgado Airport", city = "Lisbon", country = "Portugal" },
        new Airport { id = "95", code = "GVA", name = "Geneva Airport", city = "Geneva", country = "Switzerland" },
        new Airport { id = "96", code = "CPH", name = "Copenhagen Airport", city = "Copenhagen", country = "Denmark" },
        new Airport { id = "97", code = "OSL", name = "Oslo Gardermoen Airport", city = "Oslo", country = "Norway" },
        new Airport { id = "98", code = "HEL", name = "Helsinki-Vantaa Airport", city = "Helsinki", country = "Finland" },
        new Airport { id = "99", code = "LHR", name = "Heathrow Airport", city = "London", country = "United Kingdom" },
        new Airport { id = "100", code = "GIG", name = "Galeão International Airport", city = "Rio de Janeiro", country = "Brazil" }
    };
        }

        private static List<Payment> GetPaymentsData()
        {
            return new List<Payment>
    {
        new Payment { id = "PMT001", bookingId = "B001", amount = "299.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-20T10:30:00Z", status = "completed" },
        new Payment { id = "PMT002", bookingId = "B002", amount = "199.99", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-07-21T11:30:00Z", status = "completed" },
        new Payment { id = "PMT003", bookingId = "B003", amount = "399.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-22T12:30:00Z", status = "pending" },
        new Payment { id = "PMT004", bookingId = "B004", amount = "159.99", currency = "USD", paymentMethod = "paypal", paymentDate = "2024-07-23T13:30:00Z", status = "completed" },
        new Payment { id = "PMT005", bookingId = "B005", amount = "250.00", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-24T14:30:00Z", status = "cancelled" },
        new Payment { id = "PMT006", bookingId = "B006", amount = "320.00", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-07-25T15:30:00Z", status = "completed" },
        new Payment { id = "PMT007", bookingId = "B007", amount = "450.00", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-26T16:30:00Z", status = "completed" },
        new Payment { id = "PMT008", bookingId = "B008", amount = "89.99", currency = "USD", paymentMethod = "paypal", paymentDate = "2024-07-27T17:30:00Z", status = "pending" },
        new Payment { id = "PMT009", bookingId = "B009", amount = "299.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-28T18:30:00Z", status = "completed" },
        new Payment { id = "PMT010", bookingId = "B010", amount = "109.99", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-07-29T19:30:00Z", status = "pending" },
        new Payment { id = "PMT011", bookingId = "B011", amount = "299.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-07-30T20:30:00Z", status = "completed" },
        new Payment { id = "PMT012", bookingId = "B012", amount = "199.99", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-07-31T21:30:00Z", status = "completed" },
        new Payment { id = "PMT013", bookingId = "B013", amount = "379.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-08-01T22:30:00Z", status = "cancelled" },
        new Payment { id = "PMT014", bookingId = "B014", amount = "129.99", currency = "USD", paymentMethod = "paypal", paymentDate = "2024-08-02T23:30:00Z", status = "completed" },
        new Payment { id = "PMT015", bookingId = "B015", amount = "229.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-08-03T00:30:00Z", status = "completed" },
        new Payment { id = "PMT016", bookingId = "B016", amount = "349.99", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-08-04T01:30:00Z", status = "pending" },
        new Payment { id = "PMT017", bookingId = "B017", amount = "299.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-08-05T02:30:00Z", status = "completed" },
        new Payment { id = "PMT018", bookingId = "B018", amount = "189.99", currency = "USD", paymentMethod = "paypal", paymentDate = "2024-08-06T03:30:00Z", status = "cancelled" },
        new Payment { id = "PMT019", bookingId = "B019", amount = "399.99", currency = "USD", paymentMethod = "debit card", paymentDate = "2024-08-07T04:30:00Z", status = "completed" },
        new Payment { id = "PMT020", bookingId = "B020", amount = "109.99", currency = "USD", paymentMethod = "credit card", paymentDate = "2024-08-08T05:30:00Z", status = "pending" }
    };
        }

        public class Airline
        {
            public string id { get; set; }
            public string name { get; set; }
            public string code { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string logoUrl { get; set; }
        }

        public class FlightListing
        {
            public string id { get; set; }
            public string flightNumber { get; set; }
            public string airlineCode { get; set; }
            public string departure { get; set; }
            public string destination { get; set; }
            public string departureTime { get; set; }
            public string price { get; set; }
            public string description { get; set; }
            public string airlineId { get; set; }
            public string aircraftType { get; set; }
            public int availableSeats { get; set; }
            public string duration { get; set; }
        }

        public class Booking
        {
            public string id { get; set; }
            public string flightId { get; set; }
            public string passengerId { get; set; }
            public string bookingDate { get; set; }
            public string status { get; set; }
            public string seatNumber { get; set; }
            public string pricePaid { get; set; }
            public string paymentId { get; set; }
        }

        public class Passenger
        {
            public string id { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string passportNumber { get; set; }
            public string nationality { get; set; }
            public string dob { get; set; }
            public string frequentFlyerNumber { get; set; }
        }

        public class Airport
        {
            public string id { get; set; }
            public string name { get; set; }
            public string code { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string timezone { get; set; }
        }

        public class Payment
        {
            public string id { get; set; }
            public string bookingId { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
            public string paymentMethod { get; set; }
            public string paymentDate { get; set; }
            public string status { get; set; }
        }
        public class Weather
        {
            public string id { get; set; }
            public string LocationId { get; set; }
            public string LocationName { get; set; }
            public string Country { get; set; }
            public DateTime StartDate { get; set; }  // Start date of the weather data range
            public DateTime EndDate { get; set; }    // End date of the weather data range
            public string WeatherCondition { get; set; }
            public double TemperatureCelsius { get; set; }
            public int Humidity { get; set; }
            public double WindSpeedKmh { get; set; }
            public DateTime LastUpdated { get; set; }
        }
        public class CalendarEvent
        {
            public string id { get; set; }
            public string UserId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Location { get; set; }
            public string EventType { get; set; }
        }
    }
}
