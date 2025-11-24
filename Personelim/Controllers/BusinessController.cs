using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Business;
using Personelim.Services.Business;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        #region Ana İşletme Yönetimi

        /// <summary>
        /// Yeni bir Ana İşletme (Merkez) oluşturur.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            
            // ParentId null giderse Ana İşletme ve Merkez Lokasyonu oluşur
            var result = await _businessService.CreateBusinessAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }

        /// <summary>
        /// Kullanıcının sahip olduğu işletmeleri (Merkezleri) getirir.
        /// </summary>
        [HttpGet("my-businesses")]
        public async Task<IActionResult> GetMyBusinesses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetUserBusinessesAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// İşletme detayını getirir.
        /// </summary>
        [HttpGet("{businessId}")]
        public async Task<IActionResult> GetBusinessById(Guid businessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetBusinessByIdAsync(userId, businessId);
            
            if (!result.Success)
                return NotFound(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Ana İşletme bilgilerini (Merkez Lokasyon Dahil) günceller.
        /// </summary>
        [HttpPut("{businessId}")]
        public async Task<IActionResult> UpdateBusiness(Guid businessId, [FromBody] UpdateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.UpdateBusinessAsync(userId, businessId, request);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// İşletmeyi tamamen siler (Tüm lokasyonlar dahil).
        /// </summary>
        [HttpDelete("{businessId}")]
        public async Task<IActionResult> DeleteBusiness(Guid businessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.DeleteBusinessAsync(userId, businessId);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        #endregion

        #region Lokasyon (Şube) Yönetimi

        /// <summary>
        /// Ana işletmeye bağlı lokasyonları listeler.
        /// </summary>
        [HttpGet("{parentBusinessId}/locations")]
        public async Task<IActionResult> GetLocations(Guid parentBusinessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetSubBusinessesAsync(userId, parentBusinessId);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Belirli bir lokasyonun detayını getirir.
        /// </summary>
        [HttpGet("{parentBusinessId}/locations/{locationId}")]
        public async Task<IActionResult> GetLocationById(Guid parentBusinessId, Guid locationId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            
            // Servis isimlendirmesi GetSubBusinessByIdAsync olsa da içerik lokasyondur
            var result = await _businessService.GetSubBusinessByIdAsync(userId, parentBusinessId, locationId);

            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Ana işletmeye yeni bir lokasyon ekler.
        /// (Sadece İsim, Enlem, Boylam alır)
        /// </summary>
        [HttpPost("{parentBusinessId}/locations")]
        public async Task<IActionResult> CreateLocation(Guid parentBusinessId, [FromBody] CreateLocationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            
            // Yeni oluşturduğumuz CreateLocationAsync metodunu çağırıyoruz
            var result = await _businessService.CreateLocationAsync(userId, parentBusinessId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetLocationById), new { parentBusinessId = parentBusinessId, locationId = result.Data.Id }, result);
        }

        /// <summary>
        /// Lokasyon bilgilerini (Sadece İsim ve Koordinat) günceller.
        /// </summary>
        [HttpPut("{parentBusinessId}/locations/{locationId}")]
        public async Task<IActionResult> UpdateLocation(Guid parentBusinessId, Guid locationId, [FromBody] UpdateLocationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            
            // UpdateLocationRequest kullanan yeni servis metodunu çağırıyoruz
            var result = await _businessService.UpdateSubBusinessAsync(userId, parentBusinessId, locationId, request);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Lokasyonu siler.
        /// </summary>
        [HttpDelete("{parentBusinessId}/locations/{locationId}")]
        public async Task<IActionResult> DeleteLocation(Guid parentBusinessId, Guid locationId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.DeleteSubBusinessAsync(userId, parentBusinessId, locationId);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        #endregion
    }
}