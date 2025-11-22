using Personelim.Models.Enums;

namespace Personelim.Models
{
    public class MemberLeave
    {
        public Guid Id { get; set; }
        public Guid BusinessMemberId { get; set; }
        
        public string Title { get; set; }       
        public string? Description { get; set; } 
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public LeaveStatus Status { get; set; } 
        public string? RejectionReason { get; set; } 
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public BusinessMember BusinessMember { get; set; }

        public MemberLeave()
        {
            Id = Guid.NewGuid();
            Status = LeaveStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }
    }
}