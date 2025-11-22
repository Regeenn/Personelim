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

        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(
    Guid userId, 
    CreateBusinessRequest request, 
    Guid? parentBusinessId = null) 
{
    var validationResult = await _validator.ValidateCreateBusinessAsync(request);
    if (!validationResult.Success)
    {
        return ServiceResponse<BusinessResponse>.ErrorResult(validationResult.Message);
    }
    
    if (parentBusinessId.HasValue)
    {
        var parentBusiness = await _context.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == parentBusinessId.Value && b.IsActive);

        if (parentBusiness == null)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Ana işletme bulunamadı");
        }

        var hasPermission = await _context.BusinessMembers
            .AnyAsync(bm => bm.BusinessId == parentBusinessId.Value
                         && bm.UserId == userId
                         && bm.IsActive
                         && bm.Role == UserRole.Owner);

        if (!hasPermission)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Ana işletmede alt işletme oluşturma yetkiniz yok. Sadece işletme sahipleri alt işletme oluşturabilir.");
        }
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
            OwnerId = userId,
            ParentBusinessId = parentBusinessId 
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

        var parentBusinessName = parentBusinessId.HasValue
            ? await _context.Businesses
                .Where(b => b.Id == parentBusinessId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync()
            : null;

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
            CreatedAt = business.CreatedAt,
            ParentBusinessId = business.ParentBusinessId,
            ParentBusinessName = parentBusinessName,
            IsSubBusiness = parentBusinessId.HasValue,
            SubBusinessCount = 0
        };

        return ServiceResponse<BusinessResponse>.SuccessResult(
            response,
            parentBusinessId.HasValue
                ? "Alt işletme başarıyla oluşturuldu"
                : "İşletme başarıyla oluşturuldu"
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
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.ParentBusiness)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.SubBusinesses)
                    .Where(bm => bm.Business.IsActive)
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
                        CreatedAt = bm.Business.CreatedAt,
                        ParentBusinessId = bm.Business.ParentBusinessId,
                        ParentBusinessName = bm.Business.ParentBusiness != null ? bm.Business.ParentBusiness.Name : null,
                        IsSubBusiness = bm.Business.ParentBusinessId.HasValue,
                        SubBusinessCount = bm.Business.SubBusinesses.Count(sb => sb.IsActive)
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

        public async Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid userId, Guid businessId)
        {
            try
            {
                var businessMember = await _context.BusinessMembers
                    .Where(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.IsActive)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Members)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.Province)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.District)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.ParentBusiness)
                    .Include(bm => bm.Business)
                        .ThenInclude(b => b.SubBusinesses)
                    .FirstOrDefaultAsync();

                if (businessMember == null || !businessMember.Business.IsActive)
                {
                    return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı");
                }

                var response = new BusinessResponse
                {
                    Id = businessMember.Business.Id,
                    Name = businessMember.Business.Name,
                    Description = businessMember.Business.Description,
                    Address = businessMember.Business.Address,
                    PhoneNumber = businessMember.Business.PhoneNumber,
                    ProvinceId = businessMember.Business.ProvinceId,
                    ProvinceName = businessMember.Business.Province.Name,
                    DistrictId = businessMember.Business.DistrictId,
                    DistrictName = businessMember.Business.District.Name,
                    Role = businessMember.Role.ToString(),
                    MemberCount = businessMember.Business.Members.Count(m => m.IsActive),
                    CreatedAt = businessMember.Business.CreatedAt,
                    ParentBusinessId = businessMember.Business.ParentBusinessId,
                    ParentBusinessName = businessMember.Business.ParentBusiness?.Name,
                    IsSubBusiness = businessMember.Business.ParentBusinessId.HasValue,
                    SubBusinessCount = businessMember.Business.SubBusinesses.Count(sb => sb.IsActive)
                };

                return ServiceResponse<BusinessResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult(
                    "İşletme getirilirken hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request)
        {
            try
            {
                // Sadece Owner güncelleyebilir
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
                    return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı veya yetkiniz yok. Sadece işletme sahipleri güncelleme yapabilir.");
                }

                var business = businessMember.Business;

                // İsim güncellemesi
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var nameExists = await _context.Businesses
                        .AnyAsync(b => b.Name.ToLower() == request.Name.Trim().ToLower()
                                    && b.Id != businessId
                                    && b.IsActive);

                    if (nameExists)
                    {
                        return ServiceResponse<BusinessResponse>.ErrorResult("Bu isimde bir işletme zaten kayıtlı");
                    }

                    business.Name = request.Name.Trim();
                }

                // Açıklama güncellemesi
                if (request.Description != null)
                {
                    business.Description = request.Description.Trim();
                }

                // Adres güncellemesi
                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    if (request.Address.Length < 10)
                    {
                        return ServiceResponse<BusinessResponse>.ErrorResult("Adres en az 10 karakter olmalıdır");
                    }
                    business.Address = request.Address.Trim();
                }

                // Telefon güncellemesi
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var cleanPhoneNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

                    if (cleanPhoneNumber.Length != 10 || !cleanPhoneNumber.StartsWith("5"))
                    {
                        return ServiceResponse<BusinessResponse>.ErrorResult("Geçerli bir telefon numarası giriniz");
                    }

                    business.PhoneNumber = request.PhoneNumber.Trim();
                }

                // Şehir ve İlçe güncellemesi
                if (request.ProvinceId.HasValue && request.DistrictId.HasValue)
                {
                    var districtExists = await _context.Districts
                        .AnyAsync(d => d.Id == request.DistrictId.Value && d.ProvinceId == request.ProvinceId.Value);

                    if (!districtExists)
                    {
                        return ServiceResponse<BusinessResponse>.ErrorResult("Geçersiz ilçe seçimi");
                    }

                    business.ProvinceId = request.ProvinceId.Value;
                    business.DistrictId = request.DistrictId.Value;

                    // Province ve District'i yeniden yükle
                    await _context.Entry(business).Reference(b => b.Province).LoadAsync();
                    await _context.Entry(business).Reference(b => b.District).LoadAsync();
                }

                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = new BusinessResponse
                {
                    Id = business.Id,
                    Name = business.Name,
                    Description = business.Description,
                    Address = business.Address,
                    PhoneNumber = business.PhoneNumber,
                    ProvinceId = business.ProvinceId,
                    ProvinceName = business.Province.Name,
                    DistrictId = business.DistrictId,
                    DistrictName = business.District.Name,
                    Role = businessMember.Role.ToString(),
                    MemberCount = await _context.BusinessMembers.CountAsync(bm => bm.BusinessId == businessId && bm.IsActive),
                    CreatedAt = business.CreatedAt,
                    ParentBusinessId = business.ParentBusinessId,
                    IsSubBusiness = business.ParentBusinessId.HasValue,
                    SubBusinessCount = await _context.Businesses.CountAsync(b => b.ParentBusinessId == businessId && b.IsActive)
                };

                return ServiceResponse<BusinessResponse>.SuccessResult(response, "İşletme başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult(
                    "İşletme güncellenirken hata oluştu",
                    ex.Message
                );
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

                // Alt işletmeleri de pasif yap
                if (business.SubBusinesses != null && business.SubBusinesses.Any())
                {
                    foreach (var subBusiness in business.SubBusinesses.Where(sb => sb.IsActive))
                    {
                        subBusiness.IsActive = false;
                        subBusiness.UpdatedAt = DateTime.UtcNow;

                        // Alt işletmenin üyelerini de pasif yap
                        var subMembers = await _context.BusinessMembers
                            .Where(bm => bm.BusinessId == subBusiness.Id && bm.IsActive)
                            .ToListAsync();

                        foreach (var member in subMembers)
                        {
                            member.IsActive = false;
                            member.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Ana işletmenin üyelerini pasif yap
                foreach (var member in business.Members.Where(m => m.IsActive))
                {
                    member.IsActive = false;
                    member.UpdatedAt = DateTime.UtcNow;
                }

                // İşletmeyi pasif yap (soft delete)
                business.IsActive = false;
                business.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResponse<bool>.SuccessResult(true, "İşletme başarıyla silindi");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<bool>.ErrorResult(
                    "İşletme silinirken hata oluştu",
                    ex.Message
                );
            }
        }

        public async Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid userId, Guid parentBusinessId)
        {
            try
            {
                // Kullanıcının ana işletmede üyesi olup olmadığını kontrol et
                var hasAccess = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive);

                if (!hasAccess)
                {
                    return ServiceResponse<List<BusinessResponse>>.ErrorResult("Bu işletmeye erişim yetkiniz yok");
                }

                var subBusinesses = await _context.Businesses
                    .Where(b => b.ParentBusinessId == parentBusinessId && b.IsActive)
                    .Include(b => b.Province)
                    .Include(b => b.District)
                    .Include(b => b.Members)
                    .Select(b => new BusinessResponse
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Description = b.Description,
                        Address = b.Address,
                        PhoneNumber = b.PhoneNumber,
                        ProvinceId = b.ProvinceId,
                        ProvinceName = b.Province.Name,
                        DistrictId = b.DistrictId,
                        DistrictName = b.District.Name,
                        MemberCount = b.Members.Count(m => m.IsActive),
                        CreatedAt = b.CreatedAt,
                        ParentBusinessId = b.ParentBusinessId,
                        IsSubBusiness = true,
                        SubBusinessCount = 0
                    })
                    .ToListAsync();

                return ServiceResponse<List<BusinessResponse>>.SuccessResult(subBusinesses);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessResponse>>.ErrorResult(
                    "Alt işletmeler getirilirken hata oluştu",
                    ex.Message
                );
            }
        }
        
        public async Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId)
{
    try
    {
        // 1. İstenen alt işletmeyi ve ilişkili verileri çek
        var subBusiness = await _context.Businesses
            .Include(b => b.Province)
            .Include(b => b.District)
            .Include(b => b.Members)
            .Include(b => b.ParentBusiness)
            .FirstOrDefaultAsync(b => b.Id == subBusinessId 
                                   && b.ParentBusinessId == parentBusinessId 
                                   && b.IsActive);

        if (subBusiness == null)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Alt işletme bulunamadı veya belirtilen ana işletmeye ait değil.");
        }

        // 2. Yetki Kontrolü:
        // Kullanıcı Ana İşletmenin üyesi mi?
        var hasParentAccess = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId && bm.IsActive);
        
        // VEYA Kullanıcı direkt bu Alt İşletmenin bir üyesi mi?
        var subMemberInfo = subBusiness.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        var hasSubAccess = subMemberInfo != null;

        if (!hasParentAccess && !hasSubAccess)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Bu alt işletmeyi görüntüleme yetkiniz yok.");
        }
        
        string userRole = "Viewer";
        if (hasSubAccess)
        {
            userRole = subMemberInfo.Role.ToString();
        }
        else if (hasParentAccess)
        {
            // Ana işletmedeki rolünü bulalım
            var parentRole = await _context.BusinessMembers
                .Where(bm => bm.UserId == userId && bm.BusinessId == parentBusinessId)
                .Select(bm => bm.Role)
                .FirstOrDefaultAsync();
            userRole = parentRole.ToString() + " (Ana İşletme)";
        }

        
        var response = new BusinessResponse
        {
            Id = subBusiness.Id,
            Name = subBusiness.Name,
            Description = subBusiness.Description,
            Address = subBusiness.Address,
            PhoneNumber = subBusiness.PhoneNumber,
            ProvinceId = subBusiness.ProvinceId,
            ProvinceName = subBusiness.Province.Name,
            DistrictId = subBusiness.DistrictId,
            DistrictName = subBusiness.District.Name,
            Role = userRole,
            MemberCount = subBusiness.Members.Count(m => m.IsActive),
            CreatedAt = subBusiness.CreatedAt,
            ParentBusinessId = subBusiness.ParentBusinessId,
            ParentBusinessName = subBusiness.ParentBusiness?.Name,
            IsSubBusiness = true,
            SubBusinessCount = 0
        };

        return ServiceResponse<BusinessResponse>.SuccessResult(response);
    }
    catch (Exception ex)
    {
        return ServiceResponse<BusinessResponse>.ErrorResult(
            "Alt işletme detayı getirilirken hata oluştu",
            ex.Message
        );
    }
}
        public async Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request)
{
    try
    {
        var subBusiness = await _context.Businesses
            .Include(b => b.Province)
            .Include(b => b.District)
            .Include(b => b.ParentBusiness)
            .FirstOrDefaultAsync(b => b.Id == subBusinessId 
                                   && b.ParentBusinessId == parentBusinessId 
                                   && b.IsActive);

        if (subBusiness == null)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Alt işletme bulunamadı veya bu ana işletmeye ait değil");
        }
        
        var isParentOwner = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == userId 
                         && bm.BusinessId == parentBusinessId 
                         && bm.IsActive 
                         && bm.Role == UserRole.Owner);
        
        var isSubBusinessOwner = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == userId 
                         && bm.BusinessId == subBusinessId 
                         && bm.IsActive 
                         && bm.Role == UserRole.Owner);

        if (!isParentOwner && !isSubBusinessOwner)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult(
                "Alt işletmeyi güncelleme yetkiniz yok. Ana işletme sahibi veya alt işletme sahibi olmanız gerekir.");
        }
        
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nameExists = await _context.Businesses
                .AnyAsync(b => b.Name.ToLower() == request.Name.Trim().ToLower()
                            && b.Id != subBusinessId
                            && b.IsActive);

            if (nameExists)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Bu isimde bir işletme zaten kayıtlı");
            }

            subBusiness.Name = request.Name.Trim();
        }
        
        if (request.Description != null)
        {
            subBusiness.Description = request.Description.Trim();
        }
        
        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            if (request.Address.Length < 10)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Adres en az 10 karakter olmalıdır");
            }
            subBusiness.Address = request.Address.Trim();
        }
        
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var cleanPhoneNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

            if (cleanPhoneNumber.Length != 10 || !cleanPhoneNumber.StartsWith("5"))
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Geçerli bir telefon numarası giriniz");
            }

            subBusiness.PhoneNumber = request.PhoneNumber.Trim();
        }
        
        if (request.ProvinceId.HasValue && request.DistrictId.HasValue)
        {
            var districtExists = await _context.Districts
                .AnyAsync(d => d.Id == request.DistrictId.Value && d.ProvinceId == request.ProvinceId.Value);

            if (!districtExists)
            {
                return ServiceResponse<BusinessResponse>.ErrorResult("Geçersiz ilçe seçimi");
            }

            subBusiness.ProvinceId = request.ProvinceId.Value;
            subBusiness.DistrictId = request.DistrictId.Value;
            
            await _context.Entry(subBusiness).Reference(b => b.Province).LoadAsync();
            await _context.Entry(subBusiness).Reference(b => b.District).LoadAsync();
        }

        subBusiness.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var userRole = isSubBusinessOwner ? "Owner" : "Owner (Ana İşletme)";

        var response = new BusinessResponse
        {
            Id = subBusiness.Id,
            Name = subBusiness.Name,
            Description = subBusiness.Description,
            Address = subBusiness.Address,
            PhoneNumber = subBusiness.PhoneNumber,
            ProvinceId = subBusiness.ProvinceId,
            ProvinceName = subBusiness.Province.Name,
            DistrictId = subBusiness.DistrictId,
            DistrictName = subBusiness.District.Name,
            Role = userRole,
            MemberCount = await _context.BusinessMembers.CountAsync(bm => bm.BusinessId == subBusinessId && bm.IsActive),
            CreatedAt = subBusiness.CreatedAt,
            ParentBusinessId = subBusiness.ParentBusinessId,
            ParentBusinessName = subBusiness.ParentBusiness?.Name,
            IsSubBusiness = true,
            SubBusinessCount = 0
        };

        return ServiceResponse<BusinessResponse>.SuccessResult(response, "Alt işletme başarıyla güncellendi");
    }
    catch (Exception ex)
    {
        return ServiceResponse<BusinessResponse>.ErrorResult(
            "Alt işletme güncellenirken hata oluştu",
            ex.Message
        );
    }
}

public async Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var subBusiness = await _context.Businesses
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == subBusinessId 
                                   && b.ParentBusinessId == parentBusinessId 
                                   && b.IsActive);

        if (subBusiness == null)
        {
            return ServiceResponse<bool>.ErrorResult("Alt işletme bulunamadı veya bu ana işletmeye ait değil");
        }
        
        var isParentOwner = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == userId 
                         && bm.BusinessId == parentBusinessId 
                         && bm.IsActive 
                         && bm.Role == UserRole.Owner);
        
        var isSubBusinessOwner = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == userId 
                         && bm.BusinessId == subBusinessId 
                         && bm.IsActive 
                         && bm.Role == UserRole.Owner);

        if (!isParentOwner && !isSubBusinessOwner)
        {
            return ServiceResponse<bool>.ErrorResult(
                "Alt işletmeyi silme yetkiniz yok. Ana işletme sahibi veya alt işletme sahibi olmanız gerekir.");
        }
        
        foreach (var member in subBusiness.Members.Where(m => m.IsActive))
        {
            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;
        }
        
        subBusiness.IsActive = false;
        subBusiness.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResponse<bool>.SuccessResult(true, "Alt işletme başarıyla silindi");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return ServiceResponse<bool>.ErrorResult(
            "Alt işletme silinirken hata oluştu",
            ex.Message
        );
    }
}
    }
}