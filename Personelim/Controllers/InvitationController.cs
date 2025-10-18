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
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _invitationService.SendInvitationAsync(userId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("accept/{invitationCode}")]
        public async Task<IActionResult> AcceptInvitation(string invitationCode)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _invitationService.AcceptInvitationAsync(userId, invitationCode);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-invitations")]
        public async Task<IActionResult> GetMyInvitations()
        {
            var email = User.FindFirst(ClaimTypes.Email).Value;
            var result = await _invitationService.GetUserInvitationsAsync(email);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}