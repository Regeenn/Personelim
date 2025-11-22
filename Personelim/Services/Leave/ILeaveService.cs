using Personelim.DTOs.Leave;
using Personelim.Helpers;

namespace Personelim.Services.Leave
{
    public interface ILeaveService
    {
        Task<ServiceResponse<LeaveResponse>> CreateLeaveRequestAsync(Guid userId, CreateLeaveRequest request);
        
        Task<ServiceResponse<List<LeaveResponse>>> GetMyLeavesAsync(Guid userId, Guid businessId);
        
        Task<ServiceResponse<List<LeaveResponse>>> GetBusinessLeavesAsync(Guid userId, Guid businessId);
        
        Task<ServiceResponse<LeaveResponse>> UpdateLeaveStatusAsync(Guid userId, Guid leaveId, UpdateLeaveStatusRequest request);
        
        Task<ServiceResponse<bool>> DeleteLeaveAsync(Guid userId, Guid leaveId);
    }
}