namespace Personelim.Models
{
    public class Business
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public Guid OwnerId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Province? Province { get; set; }  
        public District? District { get; set; }

        
        public User? Owner { get; set; }
        public ICollection<BusinessMember> Members { get; set; }
        public ICollection<Invitation> Invitations { get; set; }
 

        public Business()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            Members = new List<BusinessMember>();
            Invitations = new List<Invitation>();
        }
    }
}