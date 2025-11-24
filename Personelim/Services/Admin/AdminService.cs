using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Admin;
using Personelim.Models;
using Personelim.Services.Email;
using Personelim.Helpers;
using BCrypt.Net;

namespace Personelim.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AdminService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ServiceResponse<Guid>> CreateOwnerUserAsync(CreateOwnerUserRequest request)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
                {
                    return ServiceResponse<Guid>.ErrorResult("Bu e-posta adresi zaten sistemde kayıtlı.");
                }
                
                string plainPassword = GenerateRandomPassword();
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                
                var newUser = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PasswordHash = passwordHash,
                    UpdatedAt = DateTime.UtcNow
                   
                };
                
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                
                var emailSent = await _emailService.SendAccountCreatedEmailAsync(request.Email, request.FirstName, plainPassword);

                string message = emailSent 
                    ? "Kullanıcı başarıyla oluşturuldu ve bilgiler e-posta ile gönderildi." 
                    : $"Kullanıcı oluşturuldu ANCAK mail gönderilemedi. Geçici Şifre: {plainPassword}";

                return ServiceResponse<Guid>.SuccessResult(newUser.Id, message);
            }
            catch (Exception ex)
            {
                return ServiceResponse<Guid>.ErrorResult("Kullanıcı oluşturulurken bir hata oluştu", ex.Message);
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}