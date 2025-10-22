namespace Personelim.DTOs.Auth
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int BusinessCount { get; set; }  // Üye olduğu şirket sayısı
        public int OwnedBusinessCount { get; set; }  // Sahibi olduğu şirket sayısı
    }
}