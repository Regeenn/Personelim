using Personelim.Models;

namespace Personelim.Services.Location
{
    public interface ILocationService
    {
        Task<List<Province>> GetAllProvincesAsync();
        Task<List<District>> GetDistrictsByProvinceIdAsync(int provinceId);
    }
}