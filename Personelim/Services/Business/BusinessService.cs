using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
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

        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(Guid userId, CreateBusinessRequest request, Guid? parentBusinessId = null)
        {
            var validationResult = await _validator.ValidateCreateBusinessAsync(request);
            if (!validationResult.Success)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult(validationResult.Message);
            }

            if (parentBusinessId.HasValue)
            {
                var parentBusiness = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == parentBusinessId.Value && b.IsActive);

                if (parentBusiness == null) return ServiceResponse<BusinessResponse>.ErrorResult("Ana işletme bulunamadı");

                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.BusinessId == parentBusinessId.Value && bm.UserId == userId && bm.IsActive && bm.Role == UserRole.Owner);

                if (!isOwner) return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon ekleme yetkiniz yok.");

                try
                {
                    var newLocation = new Models.Business
                    {
                        ParentBusinessId = parentBusinessId,
                        OwnerId = parentBusiness.OwnerId,
                        LocationName = request.LocationName.Trim(),
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Name = parentBusiness.Name,
                        Description = $"{parentBusiness.Name} - {request.LocationName}",
                        Address = parentBusiness.Address,
                        PhoneNumber = parentBusiness.PhoneNumber,
                        ProvinceId = parentBusiness.ProvinceId,
                        DistrictId = parentBusiness.DistrictId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Businesses.Add(newLocation);
                    await _context.SaveChangesAsync();

                    var provinceName = (await _context.Provinces.FindAsync(parentBusiness.ProvinceId))?.Name;
                    var districtName = (await _context.Districts.FindAsync(parentBusiness.DistrictId))?.Name;

                    var response = MapToResponse(newLocation, provinceName ?? "", districtName ?? "", "Owner", 0, 0);
                    response.IsSubBusiness = true;
                    response.ParentBusinessId = parentBusinessId;

                    return ServiceResponse<BusinessResponse>.SuccessResult(response, "Yeni lokasyon eklendi.");
                }
                catch (Exception ex)
                {
                    return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon eklenirken hata oluştu", ex.Message);
                }
            }
            else
            {
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
                        OwnerId = userId,
                        ParentBusinessId = null,
                        LocationName = request.LocationName.Trim(),
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Businesses.Add(business);
                    await _context.SaveChangesAsync();

                    var ownerMembership = new Models.BusinessMember
                    {
                        UserId = userId,
                        BusinessId = business.Id,
                        Role = UserRole.Owner
                    };
                    _context.BusinessMembers.Add(ownerMembership);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    var response = MapToResponse(business, province?.Name ?? "", district?.Name ?? "", "Owner", 1, 0);
                    return ServiceResponse<BusinessResponse>.SuccessResult(response, "İşletme ve merkez lokasyonu başarıyla oluşturuldu");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return ServiceResponse<BusinessResponse>.ErrorResult("İşletme oluşturulurken hata oluştu", ex.Message);
                }
            }
        }

        public async Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request)
        {
            try
            {
                var businessMember = await _context.BusinessMembers
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Province)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.District)
                    .FirstOrDefaultAsync(bm => bm.UserId == userId
                                            && bm.BusinessId == businessId
                                            && bm.IsActive
                                            && bm.Role == UserRole.Owner);

                if (businessMember == null || !businessMember.Business.IsActive)
                {
                    return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı veya yetkiniz yok.");
                }

                var business = businessMember.Business;

                if (!string.IsNullOrWhiteSpace(request.Name)) business.Name = request.Name.Trim();
                if (request.Description != null) business.Description = request.Description.Trim();
                if (!string.IsNullOrWhiteSpace(request.Address)) business.Address = request.Address.Trim();
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) business.PhoneNumber = request.PhoneNumber.Trim();
                
                if (!string.IsNullOrWhiteSpace(request.LocationName)) business.LocationName = request.LocationName.Trim();
                if (request.Latitude != 0 && request.Latitude != null) business.Latitude = (double)request.Latitude;
                if (request.Longitude != 0 && request.Longitude != null) business.Longitude = (double)request.Longitude;

                if (request.ProvinceId.HasValue && request.DistrictId.HasValue)
                {
                    business.ProvinceId = request.ProvinceId.Value;
                    business.DistrictId = request.DistrictId.Value;
                    await _context.Entry(business).Reference(b => b.Province).LoadAsync();
                    await _context.Entry(business).Reference(b => b.District).LoadAsync();
                }

                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var subCount = await _context.Businesses.CountAsync(b => b.ParentBusinessId == businessId && b.IsActive);
                var memCount = await _context.BusinessMembers.CountAsync(bm => bm.BusinessId == businessId && bm.IsActive);

                var response = MapToResponse(business, business.Province.Name, business.District.Name, businessMember.Role.ToString(), memCount, subCount);
                return ServiceResponse<BusinessResponse>.SuccessResult(response, "İşletme başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Güncelleme hatası", ex.Message);
            }
        }

        public async Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid userId)
        {
            try
            {
                var businesses = await _context.BusinessMembers
                    .Where(bm => bm.UserId == userId && bm.IsActive)
                    .Include(bm => bm.Business).ThenInclude(b => b.Province)
                    .Include(bm => bm.Business).ThenInclude(b => b.District)
                    .Where(bm => bm.Business.IsActive)
                    .Select(bm => new BusinessResponse
                    {
                        Id = bm.Business.Id,
                        Name = bm.Business.Name,
                        LocationName = bm.Business.LocationName,
                        Latitude = bm.Business.Latitude,
                        Longitude = bm.Business.Longitude,
                        Role = bm.Role.ToString(),
                        Address = bm.Business.Address,
                        ProvinceName = bm.Business.Province.Name,
                        DistrictName = bm.Business.District.Name,
                        Description = bm.Business.Description,
                        PhoneNumber = bm.Business.PhoneNumber,
                        ProvinceId = bm.Business.ProvinceId,
                        DistrictId = bm.Business.DistrictId,
                        CreatedAt = bm.Business.CreatedAt
                    }).ToListAsync();

                return ServiceResponse<List<BusinessResponse>>.SuccessResult(businesses);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessResponse>>.ErrorResult("İşletmeler getirilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid userId, Guid businessId)
        {
            try
            {
                var bm = await _context.BusinessMembers
                   .Include(x => x.Business).ThenInclude(b => b.Province)
                   .Include(x => x.Business).ThenInclude(b => b.District)
                   .FirstOrDefaultAsync(x => x.UserId == userId && x.BusinessId == businessId && x.IsActive);

                if (bm == null) return ServiceResponse<BusinessResponse>.ErrorResult("Bulunamadı");

                var subCount = await _context.Businesses.CountAsync(b => b.ParentBusinessId == businessId && b.IsActive);
                var memCount = await _context.BusinessMembers.CountAsync(m => m.BusinessId == businessId && m.IsActive);

                var response = MapToResponse(bm.Business, bm.Business.Province.Name, bm.Business.District.Name, bm.Role.ToString(), memCount, subCount);
                return ServiceResponse<BusinessResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("İşletme getirilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteBusinessAsync(Guid userId, Guid businessId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var businessMember = await _context.BusinessMembers
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.SubBusinesses)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Members)
                    .FirstOrDefaultAsync(bm => bm.UserId == userId
                                            && bm.BusinessId == businessId
                                            && bm.IsActive
                                            && bm.Role == UserRole.Owner);

                if (businessMember == null || !businessMember.Business.IsActive)
                {
                    return ServiceResponse<bool>.ErrorResult("İşletme bulunamadı veya sadece işletme sahibi silebilir");
                }

                var business = businessMember.Business;

                if (business.SubBusinesses != null && business.SubBusinesses.Any())
                {
                    foreach (var subBusiness in business.SubBusinesses.Where(sb => sb.IsActive))
                    {
                        subBusiness.IsActive = false;
                        subBusiness.UpdatedAt = DateTime.UtcNow;
                    }
                }

                foreach (var member in business.Members.Where(m => m.IsActive))
                {
                    member.IsActive = false;
                    member.UpdatedAt = DateTime.UtcNow;
                }

                business.IsActive = false;
                business.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return ServiceResponse<bool>.SuccessResult(true, "İşletme başarıyla silindi");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<bool>.ErrorResult("İşletme silinirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid userId, Guid parentBusinessId)
        {
            try
            {
                var hasAccess = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive);

                if (!hasAccess) return ServiceResponse<List<BusinessResponse>>.ErrorResult("Yetkiniz yok");

                var locations = await _context.Businesses
                    .Where(b => b.ParentBusinessId == parentBusinessId && b.IsActive)
                    .Include(b => b.Province)
                    .Include(b => b.District)
                    .Select(b => new BusinessResponse
                    {
                        Id = b.Id,
                        Name = b.Name,
                        LocationName = b.LocationName,
                        Latitude = b.Latitude,
                        Longitude = b.Longitude,
                        Address = b.Address,
                        ProvinceName = b.Province.Name,
                        DistrictName = b.District.Name,
                        Description = b.Description,
                        IsSubBusiness = true,
                        ParentBusinessId = b.ParentBusinessId,
                        Role = "Branch",
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                return ServiceResponse<List<BusinessResponse>>.SuccessResult(locations);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessResponse>>.ErrorResult("Lokasyonlar getirilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId)
        {
            try
            {
                var hasAccess = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive);

                if (!hasAccess) return ServiceResponse<BusinessResponse>.ErrorResult("Bu lokasyonu görüntüleme yetkiniz yok.");

                var subBusiness = await _context.Businesses
                    .Include(b => b.Province)
                    .Include(b => b.District)
                    .FirstOrDefaultAsync(b => b.Id == subBusinessId && b.ParentBusinessId == parentBusinessId && b.IsActive);

                if (subBusiness == null) return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon bulunamadı.");

                var response = MapToResponse(subBusiness, subBusiness.Province?.Name ?? "", subBusiness.District?.Name ?? "", "Branch", 0, 0);
                response.IsSubBusiness = true;

                return ServiceResponse<BusinessResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon detayı getirilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId)
        {
            try
            {
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive && bm.Role == UserRole.Owner);

                if (!isOwner) return ServiceResponse<bool>.ErrorResult("Lokasyon silme yetkiniz yok.");

                var location = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == subBusinessId && b.ParentBusinessId == parentBusinessId && b.IsActive);

                if (location == null) return ServiceResponse<bool>.ErrorResult("Lokasyon bulunamadı.");

                location.IsActive = false;
                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true, "Lokasyon silindi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult("Silme hatası", ex.Message);
            }
        }

        public async Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request)
        {
            try
            {
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive && bm.Role == UserRole.Owner);

                if (!isOwner) return ServiceResponse<BusinessResponse>.ErrorResult("Yetkiniz yok.");

                var location = await _context.Businesses
                    .Include(b => b.Province).Include(b => b.District)
                    .FirstOrDefaultAsync(b => b.Id == subBusinessId && b.ParentBusinessId == parentBusinessId && b.IsActive);

                if (location == null) return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon bulunamadı.");

                if (!string.IsNullOrWhiteSpace(request.LocationName)) location.LocationName = request.LocationName.Trim();
                if (request.Latitude != 0 && request.Latitude != null) location.Latitude = (double)request.Latitude;
                if (request.Longitude != 0 && request.Longitude != null) location.Longitude = (double)request.Longitude;

                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = MapToResponse(location, location.Province?.Name ?? "", location.District?.Name ?? "", "Owner", 0, 0);
                response.IsSubBusiness = true;

                return ServiceResponse<BusinessResponse>.SuccessResult(response, "Lokasyon güncellendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon güncellenemedi", ex.Message);
            }
        }

        private static BusinessResponse MapToResponse(Models.Business b, string provName, string distName, string role, int memCount, int subCount)
        {
            return new BusinessResponse
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Address = b.Address,
                PhoneNumber = b.PhoneNumber,
                LocationName = b.LocationName,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                ProvinceId = b.ProvinceId,
                ProvinceName = provName,
                DistrictId = b.DistrictId,
                DistrictName = distName,
                Role = role,
                MemberCount = memCount,
                CreatedAt = b.CreatedAt,
                ParentBusinessId = b.ParentBusinessId,
                IsSubBusiness = b.ParentBusinessId.HasValue,
                SubBusinessCount = subCount
            };
        }
        // ... namespace ve class tanımları ...

        // 3. LOKASYON EKLEME (Yeni Metot)
        public async Task<ServiceResponse<BusinessResponse>> CreateLocationAsync(
             Guid userId, Guid parentBusinessId, CreateLocationRequest request) 
        {
            // Önce Ana İşletmeyi Bul
            var parentBusiness = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == parentBusinessId && b.IsActive);
            
            if (parentBusiness == null) return ServiceResponse<BusinessResponse>.ErrorResult("Ana işletme bulunamadı");

            // Yetki Kontrolü: SADECE OWNER
            var isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.BusinessId == parentBusinessId && bm.UserId == userId && bm.IsActive && bm.Role == UserRole.Owner);

            if (!isOwner) return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon ekleme yetkiniz yok.");

            try
            {
                var newLocation = new Models.Business
                {
                    ParentBusinessId = parentBusinessId,
                    OwnerId = parentBusiness.OwnerId,
                    
                    // --- Sadece Client'tan gelen veriler ---
                    LocationName = request.LocationName.Trim(),
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,

                    // --- Ana Firmadan MIRAS Alınan veriler ---
                    Name = parentBusiness.Name, 
                    Description = $"{parentBusiness.Name} - {request.LocationName}",
                    Address = parentBusiness.Address, 
                    PhoneNumber = parentBusiness.PhoneNumber,
                    ProvinceId = parentBusiness.ProvinceId,
                    DistrictId = parentBusiness.DistrictId,
                    
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Businesses.Add(newLocation);
                await _context.SaveChangesAsync();

                // Response hazırlama
                var provinceName = (await _context.Provinces.FindAsync(parentBusiness.ProvinceId))?.Name;
                var districtName = (await _context.Districts.FindAsync(parentBusiness.DistrictId))?.Name;

                var response = MapToResponse(newLocation, provinceName ?? "", districtName ?? "", "Owner", 0, 0);
                response.IsSubBusiness = true;
                response.ParentBusinessId = parentBusinessId;

                return ServiceResponse<BusinessResponse>.SuccessResult(response, "Yeni lokasyon eklendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon eklenirken hata oluştu", ex.Message);
            }
        }

        // 6. LOKASYON GÜNCELLEME (UpdateLocationRequest kullanacak şekilde revize)
        public async Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId, UpdateLocationRequest request)
        {
            try
            {
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive && bm.Role == UserRole.Owner);
                
                if (!isOwner) return ServiceResponse<BusinessResponse>.ErrorResult("Yetkiniz yok.");

                var location = await _context.Businesses
                    .Include(b=> b.Province).Include(b=>b.District)
                    .FirstOrDefaultAsync(b => b.Id == subBusinessId && b.ParentBusinessId == parentBusinessId && b.IsActive);

                if (location == null) return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon bulunamadı.");

                // Sadece izin verilen alanları güncelle
                if (!string.IsNullOrWhiteSpace(request.LocationName)) location.LocationName = request.LocationName.Trim();
                if (request.Latitude.HasValue && request.Latitude != 0) location.Latitude = request.Latitude.Value;
                if (request.Longitude.HasValue && request.Longitude != 0) location.Longitude = request.Longitude.Value;
                
                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = MapToResponse(location, location.Province?.Name ?? "", location.District?.Name ?? "", "Owner", 0, 0);
                response.IsSubBusiness = true;
                
                return ServiceResponse<BusinessResponse>.SuccessResult(response, "Lokasyon güncellendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Lokasyon güncellenemedi", ex.Message);
            }
        }
    }
}