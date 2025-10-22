using Personelim.DTOs.Auth;
using Personelim.Helpers;

namespace Personelim.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ServiceResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResponse<UserProfileResponse>> GetUserProfileAsync(Guid userId);
    }
   
}