// Services/Location/LocationService.cs
using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.Models;
using System.Text.Json;

namespace Personelim.Services.Location
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _context;
        

        public LocationService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            
        }
        
        public async Task<List<Province>> GetAllProvincesAsync()
        {
            return await _context.Provinces
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<District>> GetDistrictsByProvinceIdAsync(int provinceId)
        {
            return await _context.Districts
                .Where(d => d.ProvinceId == provinceId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}