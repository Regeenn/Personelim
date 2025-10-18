namespace Personelim.DTOs.Invitation
{
    public class InvitationResponse
    {
        public Guid Id { get; set; }
        public string InvitationCode { get; set; }
        public string Email { get; set; }
        public string BusinessName { get; set; }
        public string InvitedByName { get; set; }
        public string? Message { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}