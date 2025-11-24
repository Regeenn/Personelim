using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Personelim.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Personelim.Services.Email;

namespace Personelim.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly  IEmailService _emailService;

        public AuthService(
            AppDbContext context, 
            IConfiguration configuration,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ServiceResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

                if (user == null || !user.IsActive)
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Email veya şifre hatalı");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Email veya şifre hatalı");
                }
                
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                var response = new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.GetFullName(),
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt
                };

                return ServiceResponse<AuthResponse>.SuccessResult(response, "Giriş başarılı");
            }
            catch (Exception ex)
            {
                return ServiceResponse<AuthResponse>.ErrorResult("Giriş sırasında hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<UserProfileResponse>> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.BusinessMemberships)
                    .Include(u => u.OwnedBusinesses)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return ServiceResponse<UserProfileResponse>.ErrorResult("Kullanıcı bulunamadı");
                }

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.GetFullName(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    BusinessCount = user.BusinessMemberships.Count(bm => bm.IsActive),
                    OwnedBusinessCount = user.OwnedBusinesses.Count(b => b.IsActive)
                };

                return ServiceResponse<UserProfileResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<UserProfileResponse>.ErrorResult(
                    "Profil bilgisi getirilirken hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return ServiceResponse<UserProfileResponse>.ErrorResult("Kullanıcı bulunamadı");
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.ToLower() != user.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == request.Email.ToLower() && u.Id != userId);

                    if (emailExists)
                    {
                        return ServiceResponse<UserProfileResponse>.ErrorResult("Bu email adresi zaten kullanılıyor");
                    }

                    user.Email = request.Email.ToLower();
                }

                if (!string.IsNullOrWhiteSpace(request.FirstName))
                {
                    user.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName))
                {
                    user.LastName = request.LastName;
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.GetFullName(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return ServiceResponse<UserProfileResponse>.SuccessResult(response, "Profil başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                return ServiceResponse<UserProfileResponse>.ErrorResult(
                    "Profil güncellenirken hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Kullanıcı bulunamadı");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return ServiceResponse<bool>.ErrorResult("Mevcut şifre hatalı");
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return ServiceResponse<bool>.ErrorResult("Yeni şifreler eşleşmiyor");
                }

                if (request.NewPassword.Length < 6)
                {
                    return ServiceResponse<bool>.ErrorResult("Şifre en az 6 karakter olmalıdır");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true, "Şifre başarıyla değiştirildi");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(
                    "Şifre değiştirilirken hata oluştu",
                    ex.Message
                );
            }
        }
        
        public async Task<ServiceResponse<bool>> LogoutAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Kullanıcı bulunamadı");
                }

                return ServiceResponse<bool>.SuccessResult(true, "Çıkış başarılı");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(
                    "Çıkış yapılırken hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<bool>> DeleteUserAsync(Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.OwnedBusinesses)
                    .Include(u => u.BusinessMemberships)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Kullanıcı bulunamadı");
                }

                int affectedRecords = 0;

                // Kullanıcının sahip olduğu şirketleri pasif yap
                var ownedBusinesses = await _context.Businesses
                    .Where(b => b.OwnerId == userId && b.IsActive)
                    .ToListAsync();

                foreach (var business in ownedBusinesses)
                {
                    business.IsActive = false;
                    business.UpdatedAt = DateTime.UtcNow;
                    affectedRecords++;
                }

                // Kullanıcının üye olduğu şirket üyeliklerini pasif yap
                var memberships = await _context.BusinessMembers
                    .Where(bm => bm.UserId == userId && bm.IsActive)
                    .ToListAsync();

                foreach (var membership in memberships)
                {
                    membership.IsActive = false;
                    membership.UpdatedAt = DateTime.UtcNow;
                    affectedRecords++;
                }
                
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                affectedRecords++;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResponse<bool>.SuccessResult(
                    true, 
                    $"Hesabınız başarıyla silindi. {affectedRecords} kayıt güncellendi."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<bool>.ErrorResult(
                    "Hesap silinirken hata oluştu",
                    ex.Message
                );
            }
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.GetFullName())
                }),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiresAt);
        }
         public async Task<ServiceResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

                if (user == null)
                {
                    return ServiceResponse<ForgotPasswordResponse>.SuccessResult(
                        new ForgotPasswordResponse 
                        { 
                            Email = request.Email,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                            ExpiresInMinutes = 15
                        },
                        "Eğer bu email kayıtlıysa, şifre sıfırlama kodu gönderildi"
                    );
                }
                
                var oldTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in oldTokens)
                {
                    token.IsUsed = true;
                    token.UsedAt = DateTime.UtcNow;
                }
                
                var code = GenerateRandomCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(15);

                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Code = code,
                    ExpiresAt = expiresAt
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();
                
                var emailSent = await _emailService.SendPasswordResetCodeAsync(
                    user.Email, 
                    code, 
                    user.GetFullName()
                );

                if (!emailSent)
                {
                    return ServiceResponse<ForgotPasswordResponse>.ErrorResult(
                        "Email gönderilemedi, lütfen daha sonra tekrar deneyiniz"
                    );
                }

                var response = new ForgotPasswordResponse
                {
                    Email = user.Email,
                    ExpiresAt = expiresAt,
                    ExpiresInMinutes = 15
                };

                return ServiceResponse<ForgotPasswordResponse>.SuccessResult(
                    response,
                    "Şifre sıfırlama kodu email adresinize gönderildi"
                );
            }
            catch (Exception ex)
            {
                return ServiceResponse<ForgotPasswordResponse>.ErrorResult(
                    "İşlem sırasında hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<bool>> VerifyResetCodeAsync(VerifyResetCodeRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

                if (user == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Geçersiz kod");
                }

                var token = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => 
                        t.UserId == user.Id && 
                        t.Code == request.Code && 
                        !t.IsUsed && 
                        t.ExpiresAt > DateTime.UtcNow
                    );

                if (token == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Geçersiz veya süresi dolmuş kod");
                }

                return ServiceResponse<bool>.SuccessResult(true, "Kod doğrulandı");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(
                    "Kod doğrulama sırasında hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

                if (user == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Geçersiz işlem");
                }

                var token = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => 
                        t.UserId == user.Id && 
                        t.Code == request.Code && 
                        !t.IsUsed && 
                        t.ExpiresAt > DateTime.UtcNow
                    );

                if (token == null)
                {
                    return ServiceResponse<bool>.ErrorResult("Geçersiz veya süresi dolmuş kod");
                }
                
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResponse<bool>.SuccessResult(
                    true,
                    "Şifreniz başarıyla değiştirildi"
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<bool>.ErrorResult(
                    "Şifre sıfırlama sırasında hata oluştu",
                    ex.Message
                );
            }
        }

        private string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}