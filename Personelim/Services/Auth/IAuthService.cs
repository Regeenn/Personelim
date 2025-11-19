using Microsoft.AspNetCore.Identity.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using LoginRequest = Personelim.DTOs.Auth.LoginRequest;
using RegisterRequest = Personelim.DTOs.Auth.RegisterRequest;


namespace Personelim.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ServiceResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResponse<UserProfileResponse>> GetUserProfileAsync(Guid userId);
        Task<ServiceResponse<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
        Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<ServiceResponse<bool>> DeleteUserAsync(Guid userId);
        Task<ServiceResponse<bool>> LogoutAsync(Guid userId);
        Task<ServiceResponse<ForgotPasswordResponse>> ForgotPasswordAsync(Personelim.DTOs.Auth.ForgotPasswordRequest request);
        Task<ServiceResponse<bool>> VerifyResetCodeAsync(VerifyResetCodeRequest request);
        Task<ServiceResponse<bool>> ResetPasswordAsync(Personelim.DTOs.Auth.ResetPasswordRequest request);
    }
}