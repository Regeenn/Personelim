using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Leave;
using Personelim.Services.Leave;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeaveController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _leaveService.CreateLeaveRequestAsync(userId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

      
        [HttpGet("my-leaves/{businessId}")]
        public async Task<IActionResult> GetMyLeaves(Guid businessId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _leaveService.GetMyLeavesAsync(userId, businessId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        
        [HttpGet("business/{businessId}")]
        public async Task<IActionResult> GetBusinessLeaves(Guid businessId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _leaveService.GetBusinessLeavesAsync(userId, businessId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        
        [HttpPut("{leaveId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid leaveId, [FromBody] UpdateLeaveStatusRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _leaveService.UpdateLeaveStatusAsync(userId, leaveId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        
        [HttpDelete("{leaveId}")]
        public async Task<IActionResult> DeleteLeave(Guid leaveId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _leaveService.DeleteLeaveAsync(userId, leaveId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}