using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;

        public BusinessService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(Guid userId, CreateBusinessRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        Console.WriteLine("========== DEBUG START ==========");
        Console.WriteLine($"UserId: {userId}");
        Console.WriteLine($"Request Name: '{request.Name}'");
        
        var business = new Models.Business
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            OwnerId = userId
        };
        
        Console.WriteLine($"Business ID: {business.Id}");
        _context.Businesses.Add(business);
        
        Console.WriteLine("Business kaydediliyor...");
        await _context.SaveChangesAsync();
        Console.WriteLine("✅ Business kaydedildi");
        
        var ownerMembership = new BusinessMember
        {
            UserId = userId,
            BusinessId = business.Id,
            Role = UserRole.Owner
        };
        
        Console.WriteLine($"Role: {ownerMembership.Role} (Type: {ownerMembership.Role.GetType()})");
        _context.BusinessMembers.Add(ownerMembership);
        
        Console.WriteLine("Membership kaydediliyor...");
        await _context.SaveChangesAsync();
        Console.WriteLine("✅ Membership kaydedildi");
        
        await transaction.CommitAsync();
        Console.WriteLine("========== DEBUG END ==========");
        
        var response = new BusinessResponse
        {
            Id = business.Id,
            Name = business.Name,
            Description = business.Description,
            Address = business.Address,
            Role = "Owner",
            MemberCount = 1,
            CreatedAt = business.CreatedAt
        };
        
        return ServiceResponse<BusinessResponse>.SuccessResult(response, "İşletme başarıyla oluşturuldu");
    }
    catch (DbUpdateException dbEx)
    {
        await transaction.RollbackAsync();
        Console.WriteLine("❌❌❌ DbUpdateException ❌❌❌");
        Console.WriteLine($"Message: {dbEx.Message}");
        Console.WriteLine($"InnerException: {dbEx.InnerException?.Message}");
        Console.WriteLine($"StackTrace: {dbEx.StackTrace}");
        
        return ServiceResponse<BusinessResponse>.ErrorResult(
            "Database hatası", 
            dbEx.InnerException?.Message ?? dbEx.Message
        );
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine("❌❌❌ Exception ❌❌❌");
        Console.WriteLine($"Type: {ex.GetType().Name}");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"InnerException: {ex.InnerException?.Message}");
        
        return ServiceResponse<BusinessResponse>.ErrorResult(
            "İşletme oluşturulurken hata oluştu",
            ex.InnerException?.Message ?? ex.Message
        );
    }
}

        public async Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid userId)
        {
            try
            {
                var businesses = await _context.BusinessMembers
                    .Where(bm => bm.UserId == userId && bm.IsActive)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Members)
                    .Select(bm => new BusinessResponse
                    {
                        Id = bm.Business.Id,
                        Name = bm.Business.Name,
                        Description = bm.Business.Description,
                        Address = bm.Business.Address,
                        Role = bm.Role.ToString(),
                        MemberCount = bm.Business.Members.Count(m => m.IsActive),
                        CreatedAt = bm.Business.CreatedAt
                    })
                    .ToListAsync();

                return ServiceResponse<List<BusinessResponse>>.SuccessResult(businesses);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessResponse>>.ErrorResult("İşletmeler getirilirken hata oluştu", ex.Message);
            }
        }
    }
}