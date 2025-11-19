using Personelim.Models.Enums;

namespace Personelim.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public ICollection<BusinessMember> BusinessMemberships { get; set; }
        public ICollection<Business> OwnedBusinesses { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            BusinessMemberships = new List<BusinessMember>();
            OwnedBusinesses = new List<Business>();
        }

        public string GetFullName() => $"{FirstName} {LastName}";
    }
}