using Personelim.Models.Enums; 

namespace Personelim.DTOs.BusinessMember
{
    public class UpdateBusinessMemberRequest
    {
        public UserRole Role { get; set; }    
        public string? Position { get; set; }
        public decimal? Salary { get; set; }
        public string? TCIdentityNumber { get; set; }
    }
}