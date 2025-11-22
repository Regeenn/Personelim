namespace Personelim.DTOs.BusinessMember
{
    public class DocumentDownloadResponse
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}