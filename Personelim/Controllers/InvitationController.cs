using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Invitation;
using Personelim.Services.Invitation;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvitationController : ControllerBase
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _invitationService.SendInvitationAsync(userId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("accept/{invitationCode}")]
        public async Task<IActionResult> AcceptInvitation(string invitationCode)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await _invitationService.AcceptInvitationAsync(userId, invitationCode);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-invitations")]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email);
            if (userEmailClaim == null)
                return Unauthorized("Token içinde User ID bulunamadı");

            var email = userEmailClaim.Value;
            var result = await _invitationService.GetUserInvitationsAsync(email);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}