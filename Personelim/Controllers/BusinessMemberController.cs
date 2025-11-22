using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.BusinessMember;
using Personelim.Services.BusinessMember;
using System.Security.Claims;
using Personelim.Helpers;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessMemberController : ControllerBase
    {
        private readonly IBusinessMemberService _memberService;

        public BusinessMemberController(IBusinessMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet("business/{businessId}")]
        public async Task<IActionResult> GetMembersByBusiness(Guid businessId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _memberService.GetMembersByBusinessIdAsync(userId, businessId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{memberId}")]
        public async Task<IActionResult> GetMemberById(Guid memberId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _memberService.GetMemberByIdAsync(userId, memberId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{memberId}")]
        public async Task<IActionResult> UpdateMember(Guid memberId, [FromBody] UpdateBusinessMemberRequest request)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _memberService.UpdateMemberAsync(userId, memberId, request);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{memberId}")]
        public async Task<IActionResult> RemoveMember(Guid memberId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var result = await _memberService.RemoveMemberAsync(userId, memberId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // DOSYA (DOKÜMAN) YÖNETİMİ ENDPOINTLERİ

        [HttpPost("{memberId}/documents")]
        public async Task<IActionResult> UploadDocument(Guid memberId, [FromForm] UploadDocumentRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(ServiceResponse<object>.ErrorResult("Lütfen bir dosya seçiniz."));
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = await _memberService.UploadDocumentAsync(userId, memberId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("documents/{documentId}")]
        public async Task<IActionResult> UpdateDocument(Guid documentId, [FromForm] UpdateDocumentRequest request)
        {
            // Validate: En azından bir şey gönderilmeli (İsim ya da Dosya)
            if (string.IsNullOrWhiteSpace(request.DocumentType) && request.File == null)
            {
                return BadRequest(
                    ServiceResponse<object>.ErrorResult(
                        "Güncellenecek bir bilgi (Belge Tipi veya Dosya) göndermelisiniz."));
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = await _memberService.UpdateDocumentAsync(userId, documentId, request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = await _memberService.DeleteDocumentAsync(userId, documentId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("documents/{documentId}/download")]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            // Token'dan User ID al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var currentUserId = Guid.Parse(userIdClaim.Value);

            // Tüm mantığı (Yetki, Dosya Bulma, Okuma) servise devret
            var result = await _memberService.GetDocumentFileAsync(currentUserId, documentId);

            if (!result.Success)
            {
                // Eğer belge bulunamadıysa NotFound, yetki yoksa Unauthorized dönebiliriz
                // Burada basitçe BadRequest dönüyoruz, mesaj içinde sebebi yazar.
                return BadRequest(result.Message);
            }

            var fileData = result.Data;

            // Dosyayı tarayıcıya stream olarak gönder
            // 1. Parametre: Byte Dizisi
            // 2. Parametre: Content Type (MIME Type)
            // 3. Parametre: İndirilecek dosya adı
            return File(fileData.FileBytes, fileData.ContentType, fileData.FileName);
        }
    }
}