using Personelim.DTOs.Admin;
using Personelim.Helpers; 

namespace Personelim.Services.Admin
{
    public interface IAdminService
    {
        Task<ServiceResponse<Guid>> CreateOwnerUserAsync(CreateOwnerUserRequest request);
    }
}