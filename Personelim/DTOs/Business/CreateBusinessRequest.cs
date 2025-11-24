using System.ComponentModel.DataAnnotations;

namespace Personelim.DTOs.Business
{
    public class CreateBusinessRequest
    {
        [Required(ErrorMessage = "Şirket ismi zorunludur")]
        public string Name { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Adres zorunludur")]
        public string Address { get; set; }
        
        [Required(ErrorMessage = "Telefon zorunludur")]
        public string PhoneNumber { get; set; }
        
        [Required]
        public int ProvinceId { get; set; }
        
        [Required]
        public int DistrictId { get; set; }
        
        [Required(ErrorMessage = "Lokasyon ismi (Örn: Merkez, Ofis 1) zorunludur")]
        public string LocationName { get; set; }
        
        [Required(ErrorMessage = "Enlem (Latitude) zorunludur")]
        public double Latitude { get; set; }
        
        [Required(ErrorMessage = "Boylam (Longitude) zorunludur")]
        public double Longitude { get; set; }
    }
}