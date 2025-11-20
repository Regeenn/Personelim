namespace Personelim.DTOs.Business
{
    public class BusinessResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        
        
        public Guid? ParentBusinessId { get; set; }
        public string? ParentBusinessName { get; set; }
        public int SubBusinessCount { get; set; }
        public bool IsSubBusiness { get; set; }
    }
}