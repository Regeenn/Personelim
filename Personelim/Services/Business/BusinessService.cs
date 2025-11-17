using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Validators;

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessValidator _validator;

        public BusinessService(AppDbContext context, IBusinessValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(Guid userId, CreateBusinessRequest request)
        {
            var validationResult = await _validator.ValidateCreateBusinessAsync(request);
            if (!validationResult.Success)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult(validationResult.Message);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var province = await _context.Provinces.FindAsync(request.ProvinceId);
                var district = await _context.Districts.FindAsync(request.DistrictId);
                
                var business = new Models.Business
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Address = request.Address.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    ProvinceId = request.ProvinceId,
                    DistrictId = request.DistrictId,
                    OwnerId = userId
                };

                _context.Businesses.Add(business);
                await _context.SaveChangesAsync();
                
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
                    PhoneNumber = business.PhoneNumber,
                    ProvinceId = business.ProvinceId,
                    ProvinceName = province!.Name,
                    DistrictId = business.DistrictId,
                    DistrictName = district!.Name,
                    Role = "Owner",
                    MemberCount = 1,
                    CreatedAt = business.CreatedAt
                };

                return ServiceResponse<BusinessResponse>.SuccessResult(
                    response,
                    "İşletme başarıyla oluşturuldu"
                );
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();

                return ServiceResponse<BusinessResponse>.ErrorResult(
                    "Database hatası",
                    dbEx.InnerException?.Message ?? dbEx.Message
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

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
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Province)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.District)
                    .Select(bm => new BusinessResponse
                    {
                        Id = bm.Business.Id,
                        Name = bm.Business.Name,
                        Description = bm.Business.Description,
                        Address = bm.Business.Address,
                        PhoneNumber = bm.Business.PhoneNumber,
                        ProvinceId = bm.Business.ProvinceId,
                        ProvinceName = bm.Business.Province.Name,
                        DistrictId = bm.Business.DistrictId,
                        DistrictName = bm.Business.District.Name,
                        Role = bm.Role.ToString(),
                        MemberCount = bm.Business.Members.Count(m => m.IsActive),
                        CreatedAt = bm.Business.CreatedAt
                    })
                    .ToListAsync();

                return ServiceResponse<List<BusinessResponse>>.SuccessResult(businesses);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessResponse>>.ErrorResult(
                    "İşletmeler getirilirken hata oluştu",
                    ex.Message
                );
            }
        }
    }
}