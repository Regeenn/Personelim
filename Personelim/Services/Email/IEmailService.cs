namespace Personelim.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName);
        Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message);
        Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword);
    } 
}
