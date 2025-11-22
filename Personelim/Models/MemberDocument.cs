namespace Personelim.Models
{
    public class MemberDocument
    {
        public Guid Id { get; set; }
        public Guid BusinessMemberId { get; set; } 
        public string DocumentType { get; set; }   
        public string FileName { get; set; }       
        public string FilePath { get; set; }       
        public string FileExtension { get; set; }  
        public DateTime UploadedAt { get; set; }

        public BusinessMember BusinessMember { get; set; }

        public MemberDocument()
        {
            Id = Guid.NewGuid();
            UploadedAt = DateTime.UtcNow;
        }
    }
}