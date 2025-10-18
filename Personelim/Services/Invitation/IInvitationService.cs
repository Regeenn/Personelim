using Personelim.DTOs.Invitation;
using Personelim.Helpers;

namespace Personelim.Services.Invitation
{
    public interface IInvitationService
    {
        Task<ServiceResponse<InvitationResponse>> SendInvitationAsync(Guid userId, SendInvitationRequest request);
        Task<ServiceResponse<string>> AcceptInvitationAsync(Guid userId, string invitationCode);
        Task<ServiceResponse<List<InvitationResponse>>> GetUserInvitationsAsync(string email);
    }
}