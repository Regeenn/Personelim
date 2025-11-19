using System.ComponentModel.DataAnnotations;

namespace Personelim.DTOs.Auth
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }
    }
    
    public class VerifyResetCodeRequest
    {
        [Required(ErrorMessage = "Email adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Doğrulama kodu zorunludur")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır")]
        public string Code { get; set; }
    }
    
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Doğrulama kodu zorunludur")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
        public string ConfirmPassword { get; set; }
    }
    
    public class ForgotPasswordResponse
    {
        public string Email { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInMinutes { get; set; }
    }
}