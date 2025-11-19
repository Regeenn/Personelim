using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.Models;
using System.Text.Json;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public SeedController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("locations")]
        public async Task<IActionResult> SeedLocations()
        {
            try
            {
                if (_context.Provinces.Any())
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"Veriler zaten mevcut ({_context.Provinces.Count()} il, {_context.Districts.Count()} ilçe)" 
                    });
                }

                var httpClient = _httpClientFactory.CreateClient();
                
                Console.WriteLine("📡 API isteği gönderiliyor...");
                var response = await httpClient.GetStringAsync("https://turkiyeapi.dev/api/v1/provinces");
                
                Console.WriteLine($"📥 API yanıtı alındı: {response.Length} karakter");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var apiResponse = JsonSerializer.Deserialize<TurkeyApiResponse>(response, options);
                
                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    return BadRequest(new { success = false, message = "API'den veri alınamadı" });
                }

                var provinceId = 1;
                var districtId = 1;
                var addedProvinces = 0;
                var addedDistricts = 0;

                foreach (var provinceData in apiResponse.Data)
                {
                    var province = new Province 
                    { 
                        Id = provinceId,
                        Name = provinceData.Name 
                    };
                    _context.Provinces.Add(province);
                    addedProvinces++;
                    
                    if (provinceData.Districts != null)
                    {
                        foreach (var districtData in provinceData.Districts)
                        {
                            _context.Districts.Add(new District
                            {
                                Id = districtId++,
                                Name = districtData.Name,
                                ProvinceId = provinceId
                            });
                            addedDistricts++;
                        }
                    }
                    
                    provinceId++;
                }
                
                await _context.SaveChangesAsync();
                
                return Ok(new 
                { 
                    success = true, 
                    message = $"İl-İlçe verileri başarıyla eklendi!",
                    data = new 
                    {
                        provinces = addedProvinces,
                        districts = addedDistricts
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hata: {ex.Message}");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Seed işlemi başarısız", 
                    error = ex.Message 
                });
            }
        }

        [HttpDelete("locations")]
        public async Task<IActionResult> ClearLocations()
        {
            try
            {
                var districtCount = await _context.Districts.CountAsync();
                var provinceCount = await _context.Provinces.CountAsync();

                _context.Districts.RemoveRange(_context.Districts);
                _context.Provinces.RemoveRange(_context.Provinces);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = "Veriler temizlendi",
                    deleted = new 
                    {
                        provinces = provinceCount,
                        districts = districtCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Temizleme başarısız", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("locations/status")]
        public IActionResult GetLocationStatus()
        {
            var provinceCount = _context.Provinces.Count();
            var districtCount = _context.Districts.Count();

            return Ok(new 
            { 
                success = true,
                data = new 
                {
                    provinces = provinceCount,
                    districts = districtCount,
                    isEmpty = provinceCount == 0
                }
            });
        }
    }
    
    public class TurkeyApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public List<TurkeyProvinceData> Data { get; set; }
    }

    public class TurkeyProvinceData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("districts")]
        public List<TurkeyDistrictData> Districts { get; set; }
    }

    public class TurkeyDistrictData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
    }
}