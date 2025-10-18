using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public interface IBusinessService
    {
        Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(Guid userId, CreateBusinessRequest request);
        Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid userId);
    }
}