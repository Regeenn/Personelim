using System;

namespace Personelim.DTOs.BusinessMember
{
    public class BusinessMemberResponse
    {
        public Guid Id { get; set; }         
        public Guid UserId { get; set; }     
        public string FullName { get; set; }  
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }      
        public string? Position { get; set; } 
        public decimal? Salary { get; set; }
        public string? TCIdentityNumber { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        
        public List<MemberDocumentResponse> Documents { get; set; }
        
        public class MemberDocumentResponse
        {
            public Guid Id { get; set; }
            public string DocumentType { get; set; }
            public string FileName { get; set; }
            public string FileUrl { get; set; } // İndirme linki için
            public DateTime UploadedAt { get; set; }
        }
    }
}