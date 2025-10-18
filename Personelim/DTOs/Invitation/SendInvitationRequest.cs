namespace Personelim.DTOs.Invitation
{
    public class SendInvitationRequest
    {
        public Guid BusinessId { get; set; }
        public string Email { get; set; }
        public string? Message { get; set; }
    }
}