namespace Personelim.DTOs.Business
{
    public class CreateBusinessRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        
        
        public int ProvinceId { get; set; }  
        public int DistrictId { get; set; }  
    }
}