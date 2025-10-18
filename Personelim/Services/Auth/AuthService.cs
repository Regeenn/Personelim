using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Personelim.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Personelim.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ServiceResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Email kontrolü
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email.ToLower());
                if (emailExists)
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Bu email adresi zaten kayıtlı");
                }

                // Şifre hash
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Kullanıcı oluştur
                var user = new User
                {
                    Email = request.Email.ToLower(),
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Token oluştur
                var token = GenerateJwtToken(user);

                var response = new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.GetFullName(),
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt
                };

                return ServiceResponse<AuthResponse>.SuccessResult(response, "Kayıt başarılı");
            }
            catch (Exception ex)
            {
                return ServiceResponse<AuthResponse>.ErrorResult("Kayıt sırasında hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

                if (user == null)
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Email veya şifre hatalı");
                }

                // Şifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Email veya şifre hatalı");
                }

                if (!user.IsActive)
                {
                    return ServiceResponse<AuthResponse>.ErrorResult("Hesabınız aktif değil");
                }

                // Son giriş güncelle
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Token oluştur
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
    }
}