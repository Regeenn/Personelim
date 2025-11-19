namespace Personelim.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName);
    }
}