using System.ComponentModel.DataAnnotations;

namespace UserService.Models
{
   public class Passenger
   {
      [Key]
      public string Id { get; set; }
      public string FirstName { get; set; }
      public string LastName { get; set; }
      public string Email { get; set; }
      public string Phone { get; set; }
      public string PassportNumber { get; set; }
      public string Nationality { get; set; }
      public DateTime DOB { get; set; }
      public string FrequentFlyerNumber { get; set; }
   }

}
