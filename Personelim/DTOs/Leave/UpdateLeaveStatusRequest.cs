using Personelim.Models.Enums;

namespace Personelim.DTOs.Leave
{
    public class UpdateLeaveStatusRequest
    {
        public LeaveStatus Status { get; set; } 
        public string? RejectionReason { get; set; } 
    }
}