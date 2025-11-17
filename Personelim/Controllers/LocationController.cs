// Controllers/LocationController.cs
using Microsoft.AspNetCore.Mvc;
using Personelim.Services.Location;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }
        
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _locationService.GetAllProvincesAsync();
            return Ok(provinces);
        }
        
        [HttpGet("provinces/{provinceId}/districts")]
        public async Task<IActionResult> GetDistrictsByProvince(int provinceId)
        {
            var districts = await _locationService.GetDistrictsByProvinceIdAsync(provinceId);
            return Ok(districts);
        }
    }
}