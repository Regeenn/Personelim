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

        /// <summary>
        /// Yeni işletme oluşturur (Ana veya Alt işletme)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.CreateBusinessAsync(userId, request);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }

        /// <summary>
        /// Kullanıcının tüm işletmelerini getirir
        /// </summary>
        [HttpGet("my-businesses")]
        public async Task<IActionResult> GetMyBusinesses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetUserBusinessesAsync(userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Belirli bir işletmenin detaylarını getirir
        /// </summary>
        [HttpGet("{businessId}")]
        public async Task<IActionResult> GetBusinessById(Guid businessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetBusinessByIdAsync(userId, businessId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// İşletme bilgilerini günceller
        /// </summary>
        [HttpPut("{businessId}")]
        public async Task<IActionResult> UpdateBusiness(Guid businessId, [FromBody] UpdateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.UpdateBusinessAsync(userId, businessId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// İşletmeyi siler (Soft Delete)
        /// </summary>
        [HttpDelete("{businessId}")]
        public async Task<IActionResult> DeleteBusiness(Guid businessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.DeleteBusinessAsync(userId, businessId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        
        [HttpGet("{parentBusinessId}/sub-businesses")]
        public async Task<IActionResult> GetSubBusinesses(Guid parentBusinessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.GetSubBusinessesAsync(userId, parentBusinessId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        
        [HttpGet("{parentBusinessId}/sub-businesses/{subBusinessId}")]
        public async Task<IActionResult> GetSubBusinessById(Guid parentBusinessId, Guid subBusinessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) 
                return Unauthorized();
    
            var userId = Guid.Parse(userIdClaim.Value);

            var result = await _businessService.GetSubBusinessByIdAsync(userId, parentBusinessId, subBusinessId);
    
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        
        [HttpPost("{parentBusinessId}/sub-businesses")]
        public async Task<IActionResult> CreateSubBusiness(Guid parentBusinessId, [FromBody] CreateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            
            var result = await _businessService.CreateBusinessAsync(userId, request, parentBusinessId);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetBusinessById), new { businessId = result.Data.Id }, result);
        }
        [HttpPut("{parentBusinessId}/sub-businesses/{subBusinessId}")]
        public async Task<IActionResult> UpdateSubBusiness(Guid parentBusinessId, Guid subBusinessId, [FromBody] UpdateBusinessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.UpdateSubBusinessAsync(userId, parentBusinessId, subBusinessId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        
        [HttpDelete("{parentBusinessId}/sub-businesses/{subBusinessId}")]
        public async Task<IActionResult> DeleteSubBusiness(Guid parentBusinessId, Guid subBusinessId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _businessService.DeleteSubBusinessAsync(userId, parentBusinessId, subBusinessId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}