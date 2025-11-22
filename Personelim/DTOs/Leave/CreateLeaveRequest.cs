namespace Personelim.DTOs.Leave
{
    public class CreateLeaveRequest
    {
        public Guid BusinessId { get; set; } 
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}