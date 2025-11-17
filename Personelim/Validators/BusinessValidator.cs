using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Validators
{
    public interface IBusinessValidator
    {
        Task<ServiceResponse<bool>> ValidateCreateBusinessAsync(CreateBusinessRequest request);
    }

    public class BusinessValidator : IBusinessValidator
    {
        private readonly AppDbContext _context;

        public BusinessValidator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<bool>> ValidateCreateBusinessAsync(CreateBusinessRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ServiceResponse<bool>.ErrorResult("İşletme adı zorunludur");
            }

            if (request.Name.Length < 2)
            {
                return ServiceResponse<bool>.ErrorResult("İşletme adı en az 2 karakter olmalıdır");
            }

            if (request.Name.Length > 200)
            {
                return ServiceResponse<bool>.ErrorResult("İşletme adı en fazla 200 karakter olabilir");
            }
            
            var existingBusinessByName = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Name.ToLower() == request.Name.Trim().ToLower() && b.IsActive);

            if (existingBusinessByName != null)
            {
                return ServiceResponse<bool>.ErrorResult("Bu isimde bir işletme zaten kayıtlı");
            }
            
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return ServiceResponse<bool>.ErrorResult("Adres boş bırakılamaz");
            }

            if (request.Address.Length < 10)
            {
                return ServiceResponse<bool>.ErrorResult("Adres en az 10 karakter olmalıdır");
            }

            if (request.Address.Length > 500)
            {
                return ServiceResponse<bool>.ErrorResult("Adres en fazla 500 karakter olabilir");
            }
            
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return ServiceResponse<bool>.ErrorResult("Telefon numarası boş bırakılamaz");
            }
            
            var cleanPhoneNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

            if (cleanPhoneNumber.Length < 10 || cleanPhoneNumber.Length > 11)
            {
                return ServiceResponse<bool>.ErrorResult("Geçerli bir telefon numarası giriniz (10-11 rakam)");
            }
            
            var existingBusinesses = await _context.Businesses
                .Where(b => b.IsActive)
                .Select(b => b.PhoneNumber)
                .ToListAsync();

            foreach (var phone in existingBusinesses)
            {
                var dbCleanPhone = new string(phone.Where(char.IsDigit).ToArray());
                if (dbCleanPhone == cleanPhoneNumber)
                {
                    return ServiceResponse<bool>.ErrorResult(
                        "Bu telefon numarası başka bir işletme tarafından kullanılıyor"
                    );
                }
            }
            
            var provinceExists = await _context.Provinces
                .AnyAsync(p => p.Id == request.ProvinceId);

            if (!provinceExists)
            {
                return ServiceResponse<bool>.ErrorResult("Geçersiz şehir seçimi");
            }
            
            var districtExists = await _context.Districts
                .AnyAsync(d => d.Id == request.DistrictId && d.ProvinceId == request.ProvinceId);

            if (!districtExists)
            {
                return ServiceResponse<bool>.ErrorResult(
                    "Geçersiz ilçe seçimi veya seçilen ilçe bu şehre ait değil"
                );
            }
            
            return ServiceResponse<bool>.SuccessResult(true);
        }
    }
}