using Personelim.Models.Enums;

namespace Personelim.Models
{
    public class BusinessMember
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public UserRole Role { get; set; }
        public string? Position { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }

        // Navigation Properties
        public User? User { get; set; }
        public Business? Business { get; set; }

        public BusinessMember()
        {
            Id = Guid.NewGuid();
            JoinedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }
}