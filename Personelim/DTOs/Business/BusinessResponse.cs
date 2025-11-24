namespace Personelim.DTOs.Business
{
    public class BusinessResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        
        // Yeni Alanlar
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        
        public string Role { get; set; }
        public int MemberCount { get; set; }
        
        public Guid? ParentBusinessId { get; set; }
        public string? ParentBusinessName { get; set; }
        public bool IsSubBusiness { get; set; }
        public int SubBusinessCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}