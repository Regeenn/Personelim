using Personelim.DTOs.BusinessMember;
using Personelim.Helpers;

namespace Personelim.Services.BusinessMember
{
    public interface IBusinessMemberService
    {
        Task<ServiceResponse<List<BusinessMemberResponse>>> GetMembersByBusinessIdAsync(Guid currentUserId, Guid businessId);
        
        Task<ServiceResponse<BusinessMemberResponse>> GetMemberByIdAsync(Guid currentUserId, Guid memberId);
        
        Task<ServiceResponse<BusinessMemberResponse>> UpdateMemberAsync(Guid currentUserId, Guid memberId, UpdateBusinessMemberRequest request);
        
        Task<ServiceResponse<bool>> RemoveMemberAsync(Guid currentUserId, Guid memberId);
        Task<ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>> UploadDocumentAsync(Guid currentUserId, Guid memberId, UploadDocumentRequest request);
        Task<ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>> UpdateDocumentAsync(Guid currentUserId, Guid documentId, UpdateDocumentRequest request);
        Task<ServiceResponse<bool>> DeleteDocumentAsync(Guid currentUserId, Guid documentId);
        Task<ServiceResponse<DocumentDownloadResponse>> GetDocumentFileAsync(Guid currentUserId, Guid documentId);
    }
}