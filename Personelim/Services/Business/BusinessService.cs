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
                // İşletme oluştur
                var business = new Models.Business
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    PhoneNumber = request.PhoneNumber,
                    OwnerId = userId
                };

                _context.Businesses.Add(business);
                await _context.SaveChangesAsync();

                // Owner üyeliği oluştur
                var ownerMembership = new BusinessMember
                {
                    UserId = userId,
                    BusinessId = business.Id,
                    Role = UserRole.Owner
                };

                _context.BusinessMembers.Add(ownerMembership);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<BusinessResponse>.ErrorResult("İşletme oluşturulurken hata oluştu", ex.Message);
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