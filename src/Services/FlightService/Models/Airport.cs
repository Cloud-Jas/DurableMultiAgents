using System.ComponentModel.DataAnnotations;

namespace FlightService.Models
{
   public class Airport
   {
      [Key]
      public string AirportId { get; set; }
      public string Code { get; set; }
      public string Name { get; set; }
      public string City { get; set; }
      public string Country { get; set; }
   }
}
