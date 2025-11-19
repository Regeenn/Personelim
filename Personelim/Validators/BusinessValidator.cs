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
        
        // Türkiye'de kullanılan operatör kodları (Güncel 2024)
        private readonly HashSet<string> _validOperatorCodes = new()
        {
            // Turkcell
            "500", "501", "505", "506", "507",
            
            // Vodafone
            "530", "531", "532", "533", "534", "535", "536", "537", "538", "539",
            
            // Türk Telekom
            "540", "541", "542", "543", "544", "545", "546", "547", "548", "549",
            "550", "551", "552", "553", "554", "555", "556", "557", "558", "559",
            
            // Avea (Türk Telekom ile birleşti ama hala kullanılıyor)
            "501", "505", "506", "507",
            
            // Diğer sanal operatörler
            "551", "552", "553", "554", "555", "556", "559", // Türk Telekom sanal
            "510", "511", "512", "513", "514", "515", "516", "517", "518", "519", // Sanal operatörler
            "520", "521", "522", "523", "524", "525", "526", "527", "528", "529", // Sanal operatörler
            "560", "561", "562", "563", "564", "565", "566", "567", "568", "569", // Sanal operatörler
        };

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

            // Telefon numarası validasyonu
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return ServiceResponse<bool>.ErrorResult("Telefon numarası boş bırakılamaz");
            }

            var cleanPhoneNumber = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());

            // 10 haneli olmalı
            if (cleanPhoneNumber.Length != 10)
            {
                return ServiceResponse<bool>.ErrorResult("Telefon numarası 10 haneli olmalıdır");
            }

            // İlk rakam 5 ile başlamalı
            if (!cleanPhoneNumber.StartsWith("5"))
            {
                return ServiceResponse<bool>.ErrorResult("Telefon numarası 5 ile başlamalıdır");
            }

            // İlk 3 hane geçerli operatör kodu olmalı
            var operatorCode = cleanPhoneNumber.Substring(0, 3);
            if (!_validOperatorCodes.Contains(operatorCode))
            {
                return ServiceResponse<bool>.ErrorResult(
                    "Geçersiz operatör kodu. Lütfen geçerli bir Türkiye telefon numarası giriniz"
                );
            }

            // Telefon numarası benzersizliği kontrolü
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