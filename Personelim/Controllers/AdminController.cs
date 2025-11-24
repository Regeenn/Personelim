using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Admin;
using Personelim.Helpers;
using Personelim.Services.Admin;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Yeni bir işletme sahibi (User) oluşturur.
        /// Sistem otomatik şifre üretir ve mail atar.
        /// Herhangi bir yetki kontrolü yoktur (Public Endpoint).
        /// </summary>
        [HttpPost("create-owner")]
        public async Task<ActionResult<ServiceResponse<Guid>>> CreateOwner([FromBody] CreateOwnerUserRequest request)
        {
            var result = await _adminService.CreateOwnerUserAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            // 200 OK ve oluşturulan kullanıcının ID'si döner
            return Ok(result);
        }
    }
}