using System.ComponentModel.DataAnnotations;

namespace Personelim.DTOs.Business
{
  
    public class CreateLocationRequest
    {
        [Required(ErrorMessage = "Lokasyon ismi zorunludur")]
        public string LocationName { get; set; } 

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
    
    public class UpdateLocationRequest
    {
        public string? LocationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}