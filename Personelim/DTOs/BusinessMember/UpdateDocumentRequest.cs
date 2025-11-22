using Microsoft.AspNetCore.Http;

namespace Personelim.DTOs.BusinessMember
{
    public class UpdateDocumentRequest
    {
        public string DocumentType { get; set; } 
        
        public IFormFile? File { get; set; } 
    }
}