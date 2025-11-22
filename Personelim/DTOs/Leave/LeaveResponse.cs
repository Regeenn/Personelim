namespace Personelim.DTOs.Leave
{
    public class LeaveResponse
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DayCount { get; set; } 
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}