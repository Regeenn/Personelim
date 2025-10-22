using Personelim.Models.Enums;

namespace Personelim.Models
{
    public class Invitation
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public string InvitationCode { get; set; }
        public Guid InvitedByUserId { get; set; }
        public InvitationStatus Status { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }

        // Navigation Properties
        public Business? Business { get; set; }
        public User? InvitedBy { get; set; }

        public Invitation()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddDays(7);
            Status = InvitationStatus.Pending;
            InvitationCode = GenerateCode();
        }

        private static string GenerateCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        public bool IsValid()
        {
            return Status == InvitationStatus.Pending && ExpiresAt > DateTime.UtcNow;
        }
    }
}