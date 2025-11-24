using Microsoft.AspNetCore.Hosting; // Dosya işlemleri için gerekli
using Microsoft.AspNetCore.Http;    // IFormFile için gerekli
using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.BusinessMember;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;

namespace Personelim.Services.BusinessMember
{
    public class BusinessMemberService : IBusinessMemberService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; 
        
        public BusinessMemberService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        
        public async Task<ServiceResponse<List<BusinessMemberResponse>>> GetMembersByBusinessIdAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                var isMember = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == businessId && bm.IsActive);

                if (!isMember)
                    return ServiceResponse<List<BusinessMemberResponse>>.ErrorResult("Bu işletmenin personelini görüntüleme yetkiniz yok.");

                var members = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .Where(bm => bm.BusinessId == businessId && bm.IsActive)
                    .Select(bm => new BusinessMemberResponse
                    {
                        Id = bm.Id,
                        UserId = bm.UserId,
                        FullName = bm.User.FirstName + " " + bm.User.LastName,
                        Email = bm.User.Email,
                        Role = bm.Role.ToString(),
                        Position = bm.Position,
                        Salary = bm.Salary,                 
                        TCIdentityNumber = bm.TCIdentityNumber, 
                        JoinedAt = bm.JoinedAt,
                        IsActive = bm.IsActive
                       
                    })
                    .ToListAsync();

                return ServiceResponse<List<BusinessMemberResponse>>.SuccessResult(members);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessMemberResponse>>.ErrorResult("Personel listesi alınırken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponse>> GetMemberByIdAsync(Guid currentUserId, Guid memberId)
        {
            try
            {
                var member = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .Include(bm => bm.Documents) // Dokümanları da çekiyoruz
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);

                if (member == null)
                    return ServiceResponse<BusinessMemberResponse>.ErrorResult("Personel bulunamadı.");
                
                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == member.BusinessId && bm.IsActive);

                if (requester == null)
                    return ServiceResponse<BusinessMemberResponse>.ErrorResult("Bu personeli görüntüleme yetkiniz yok.");

                var response = new BusinessMemberResponse
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    FullName = member.User.FirstName + " " + member.User.LastName,
                    Email = member.User.Email,
                    Role = member.Role.ToString(),
                    Position = member.Position,
                    Salary = member.Salary,                
                    TCIdentityNumber = member.TCIdentityNumber, 
                    JoinedAt = member.JoinedAt,
                    IsActive = member.IsActive,
                    Documents = member.Documents.Select(d => new BusinessMemberResponse.MemberDocumentResponse
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        FileUrl = d.FilePath,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                };

                return ServiceResponse<BusinessMemberResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponse>.ErrorResult("Personel detayı alınırken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponse>> UpdateMemberAsync(Guid currentUserId, Guid memberId, UpdateBusinessMemberRequest request)
        {
            try
            {
                var targetMember = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);

                if (targetMember == null)
                    return ServiceResponse<BusinessMemberResponse>.ErrorResult("Düzenlenecek personel bulunamadı.");
                
                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == targetMember.BusinessId && bm.IsActive);

                if (requester == null || requester.Role != UserRole.Owner)
                {
                    return ServiceResponse<BusinessMemberResponse>.ErrorResult("Personel bilgilerini güncelleme yetkiniz yok. Sadece işletme sahibi yapabilir.");
                }
                
                if (!string.IsNullOrEmpty(request.TCIdentityNumber))
                {
                    if (request.TCIdentityNumber.Length != 11 || !request.TCIdentityNumber.All(char.IsDigit))
                    {
                        return ServiceResponse<BusinessMemberResponse>.ErrorResult("TC Kimlik Numarası 11 haneli rakamlardan oluşmalıdır.");
                    }
                }
                
                targetMember.Role = request.Role;
                targetMember.Position = request.Position;
                targetMember.Salary = request.Salary;                 
                targetMember.TCIdentityNumber = request.TCIdentityNumber; 
                targetMember.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new BusinessMemberResponse
                {
                    Id = targetMember.Id,
                    UserId = targetMember.UserId,
                    FullName = targetMember.User.FirstName + " " + targetMember.User.LastName,
                    Email = targetMember.User.Email,
                    Role = targetMember.Role.ToString(),
                    Position = targetMember.Position,
                    Salary = targetMember.Salary,
                    TCIdentityNumber = targetMember.TCIdentityNumber,
                    JoinedAt = targetMember.JoinedAt,
                    IsActive = targetMember.IsActive
                };

                return ServiceResponse<BusinessMemberResponse>.SuccessResult(response, "Personel bilgileri güncellendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponse>.ErrorResult("Güncelleme sırasında hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<bool>> RemoveMemberAsync(Guid currentUserId, Guid memberId)
        {
            try
            {
                var targetMember = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);

                if (targetMember == null)
                    return ServiceResponse<bool>.ErrorResult("Silinecek personel bulunamadı.");

                if (targetMember.UserId == currentUserId)
                {
                    return ServiceResponse<bool>.ErrorResult("Kendinizi bu ekrandan çıkartamazsınız.");
                }

                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == targetMember.BusinessId && bm.IsActive);

                if (requester == null || requester.Role != UserRole.Owner)
                {
                     return ServiceResponse<bool>.ErrorResult("Personel çıkartma yetkiniz yok.");
                }
                
                targetMember.IsActive = false;
                targetMember.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, "Personel işletmeden çıkartıldı.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult("Personel çıkartılırken hata oluştu.", ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>> UploadDocumentAsync(Guid currentUserId, Guid memberId, UploadDocumentRequest request)
        {
            try
            {
                var member = await _context.BusinessMembers.FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (member == null) return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Personel bulunamadı.");
                
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == member.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
                
                if (!isOwner) return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Belge yükleme yetkiniz yok.");
                
                if (request.File == null || request.File.Length == 0)
                    return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Dosya seçilmedi.");

                var ext = Path.GetExtension(request.File.FileName).ToLower();
                if (ext != ".pdf")
                    return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Sadece PDF dosyaları yüklenebilir.");

               
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "documents", member.BusinessId.ToString(), member.Id.ToString());
                
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                
                string uniqueFileName = Guid.NewGuid().ToString() + ext;
                string fullPath = Path.Combine(uploadFolder, uniqueFileName);

                
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                
                string dbFilePath = Path.Combine("uploads", "documents", member.BusinessId.ToString(), member.Id.ToString(), uniqueFileName).Replace("\\", "/");

                var document = new MemberDocument
                {
                    BusinessMemberId = memberId,
                    DocumentType = request.DocumentType,
                    FileName = request.File.FileName, 
                    FilePath = dbFilePath,
                    FileExtension = ext,
                    UploadedAt = DateTime.UtcNow
                };

                _context.MemberDocuments.Add(document);
                await _context.SaveChangesAsync();

                return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.SuccessResult(new BusinessMemberResponse.MemberDocumentResponse
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType,
                    FileName = document.FileName,
                    FileUrl = document.FilePath,
                    UploadedAt = document.UploadedAt
                }, "Belge başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Dosya yüklenirken hata oluştu: " + ex.Message);
            }
        }
        
public async Task<ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>> UpdateDocumentAsync(Guid currentUserId, Guid documentId, UpdateDocumentRequest request)
{
    try
    {
        var doc = await _context.MemberDocuments
            .Include(d => d.BusinessMember)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null) return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Belge bulunamadı.");
        
        var isOwner = await _context.BusinessMembers
            .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);

        if (!isOwner) return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Belge güncelleme yetkiniz yok.");
        
        if (!string.IsNullOrWhiteSpace(request.DocumentType))
        {
            doc.DocumentType = request.DocumentType;
        }
        
        if (request.File != null && request.File.Length > 0)
        {
            var ext = Path.GetExtension(request.File.FileName).ToLower();
            if (ext != ".pdf")
                return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Sadece PDF dosyaları yüklenebilir.");
            
            string oldFullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Delete(oldFullPath);
            }
            
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "documents", doc.BusinessMember.BusinessId.ToString(), doc.BusinessMember.Id.ToString());
            
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            
            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string newFullPath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(newFullPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }
            
            string dbFilePath = Path.Combine("uploads", "documents", doc.BusinessMember.BusinessId.ToString(), doc.BusinessMember.Id.ToString(), uniqueFileName).Replace("\\", "/");
            
            doc.FileName = request.File.FileName; 
            doc.FilePath = dbFilePath;
            doc.FileExtension = ext;
            doc.UploadedAt = DateTime.UtcNow; 
        }
        
        await _context.SaveChangesAsync();

        return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.SuccessResult(new BusinessMemberResponse.MemberDocumentResponse
        {
            Id = doc.Id,
            DocumentType = doc.DocumentType,
            FileName = doc.FileName,
            FileUrl = doc.FilePath,
            UploadedAt = doc.UploadedAt
        }, "Belge başarıyla güncellendi.");
    }
    catch (Exception ex)
    {
        return ServiceResponse<BusinessMemberResponse.MemberDocumentResponse>.ErrorResult("Belge güncellenirken hata oluştu: " + ex.Message);
    }
}

// Interface'e eklemeyi unutmayın: 
// Task<ServiceResponse<DocumentDownloadResponse>> GetDocumentFileAsync(Guid currentUserId, Guid documentId);

public async Task<ServiceResponse<DocumentDownloadResponse>> GetDocumentFileAsync(Guid currentUserId, Guid documentId)
{
    var doc = await _context.MemberDocuments
        .Include(d => d.BusinessMember)
        .FirstOrDefaultAsync(d => d.Id == documentId);

    if (doc == null) return ServiceResponse<DocumentDownloadResponse>.ErrorResult("Belge bulunamadı.");

    // YETKİ KONTROLÜ
    // 1. Personelin kendisi mi?
    bool isSelf = doc.BusinessMember.UserId == currentUserId;

    // 2. İşletme sahibi mi?
    bool isOwner = await _context.BusinessMembers
        .AnyAsync(bm => bm.UserId == currentUserId && 
                        bm.BusinessId == doc.BusinessMember.BusinessId && 
                        bm.Role == UserRole.Owner && 
                        bm.IsActive);

    if (!isSelf && !isOwner)
    {
        return ServiceResponse<DocumentDownloadResponse>.ErrorResult("Bu belgeyi görüntüleme yetkiniz yok.");
    }

    // Dosyayı Diskten Oku
    string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
    
    if (!System.IO.File.Exists(fullPath))
        return ServiceResponse<DocumentDownloadResponse>.ErrorResult("Dosya sunucuda bulunamadı (Silinmiş olabilir).");

    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

    return ServiceResponse<DocumentDownloadResponse>.SuccessResult(new DocumentDownloadResponse
    {
        FileBytes = fileBytes,
        FileName = doc.FileName,
        ContentType = "application/pdf" // Sadece PDF kabul ettiğimiz için sabit
    });
}
       
        public async Task<ServiceResponse<bool>> DeleteDocumentAsync(Guid currentUserId, Guid documentId)
        {
            try
            {
                var doc = await _context.MemberDocuments
                    .Include(d => d.BusinessMember)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (doc == null) return ServiceResponse<bool>.ErrorResult("Belge bulunamadı.");

                
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);

                if (!isOwner) return ServiceResponse<bool>.ErrorResult("Belge silme yetkiniz yok.");
               
                string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
                
                _context.MemberDocuments.Remove(doc);
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true, "Belge başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult("Belge silinirken hata oluştu: " + ex.Message);
            }
        }
    }
}