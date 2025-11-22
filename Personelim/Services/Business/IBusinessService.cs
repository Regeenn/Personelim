using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        // Opsiyonel parametre ekleyin
        Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(
            Guid userId, 
            CreateBusinessRequest request, 
            Guid? parentBusinessId = null);
        
        Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid userId);
        Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid userId, Guid businessId);
        Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request);
        Task<ServiceResponse<bool>> DeleteBusinessAsync(Guid userId, Guid businessId);
        Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid userId, Guid parentBusinessId);
        Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request);
        Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId);
        Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid userId, Guid parentBusinessId, Guid subBusinessId);
       
    }
}
