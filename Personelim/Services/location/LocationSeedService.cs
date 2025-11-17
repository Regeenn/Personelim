using Personelim.Data;
using Personelim.Models;
using System.Text.Json;

namespace Personelim.Services.Location
{
    public class LocationSeedService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public LocationSeedService(AppDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        public async Task SeedLocationsAsync()
        {
            if (_context.Provinces.Any())
            {
                Console.WriteLine("✅ İl ve ilçe verileri zaten mevcut");
                return;
            }

            Console.WriteLine("⏳ Türkiye İl-İlçe API'sinden veriler çekiliyor...");

            try
            {
                // İlleri çek
                var provincesResponse = await _httpClient.GetStringAsync("https://turkiyeapi.dev/api/v1/provinces");
                var provincesData = JsonSerializer.Deserialize<ProvinceApiResponse>(provincesResponse);

                if (provincesData?.Data == null)
                {
                    Console.WriteLine("❌ İl verileri çekilemedi");
                    return;
                }

                var provinceId = 1;
                var districtId = 1;

                foreach (var provinceApi in provincesData.Data)
                {
                    // İl ekle
                    var province = new Province
                    {
                        Id = provinceId,
                        Name = provinceApi.Name
                    };
                    _context.Provinces.Add(province);

                    // İlçeleri ekle
                    if (provinceApi.Districts != null)
                    {
                        foreach (var districtApi in provinceApi.Districts)
                        {
                            var district = new District
                            {
                                Id = districtId++,
                                Name = districtApi.Name,
                                ProvinceId = provinceId
                            };
                            _context.Districts.Add(district);
                        }
                    }

                    provinceId++;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ {provincesData.Data.Count} il ve ilçeleri başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ İl-İlçe verileri eklenirken hata: {ex.Message}");
            }
        }

        // API Response Models
        private class ProvinceApiResponse
        {
            public string Status { get; set; }
            public List<ProvinceApiData> Data { get; set; }
        }

        private class ProvinceApiData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<DistrictApiData> Districts { get; set; }
        }

        private class DistrictApiData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}