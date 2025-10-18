using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Invitation;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;

namespace Personelim.Services.Invitation
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;

        public InvitationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<InvitationResponse>> SendInvitationAsync(Guid userId, SendInvitationRequest request)
        {
            try
            {
                // Owner kontrolü
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == userId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isOwner)
                {
                    return ServiceResponse<InvitationResponse>.ErrorResult("Bu işletme için davetiye gönderme yetkiniz yok");
                }

                // Aktif davetiye kontrolü
                var existingInvitation = await _context.Invitations
                    .FirstOrDefaultAsync(i =>
                        i.BusinessId == request.BusinessId &&
                        i.Email == request.Email.ToLower() &&
                        i.Status == InvitationStatus.Pending &&
                        i.ExpiresAt > DateTime.UtcNow);

                if (existingInvitation != null)
                {
                    return ServiceResponse<InvitationResponse>.ErrorResult("Bu email için aktif bir davetiye zaten mevcut");
                }

                // Kullanıcı zaten üye mi?
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
                if (existingUser != null)
                {
                    var isMember = await _context.BusinessMembers.AnyAsync(bm =>
                        bm.UserId == existingUser.Id &&
                        bm.BusinessId == request.BusinessId &&
                        bm.IsActive);

                    if (isMember)
                    {
                        return ServiceResponse<InvitationResponse>.ErrorResult("Bu kullanıcı zaten işletme üyesi");
                    }
                }

                var business = await _context.Businesses.FindAsync(request.BusinessId);
                var inviter = await _context.Users.FindAsync(userId);

                var invitation = new Models.Invitation
                {
                    BusinessId = request.BusinessId,
                    Email = request.Email.ToLower(),
                    InvitedByUserId = userId,
                    Message = request.Message
                };

                _context.Invitations.Add(invitation);
                await _context.SaveChangesAsync();

                var response = new InvitationResponse
                {
                    Id = invitation.Id,
                    InvitationCode = invitation.InvitationCode,
                    Email = invitation.Email,
                    BusinessName = business.Name,
                    InvitedByName = inviter.GetFullName(),
                    Message = invitation.Message,
                    ExpiresAt = invitation.ExpiresAt,
                    CreatedAt = invitation.CreatedAt
                };

                return ServiceResponse<InvitationResponse>.SuccessResult(response, "Davetiye başarıyla gönderildi");
            }
            catch (Exception ex)
            {
                return ServiceResponse<InvitationResponse>.ErrorResult("Davetiye gönderilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<string>> AcceptInvitationAsync(Guid userId, string invitationCode)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Business)
                    .FirstOrDefaultAsync(i => i.InvitationCode == invitationCode);

                if (invitation == null)
                {
                    return ServiceResponse<string>.ErrorResult("Davetiye bulunamadı");
                }

                if (!invitation.IsValid())
                {
                    return ServiceResponse<string>.ErrorResult("Davetiye geçersiz veya süresi dolmuş");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user.Email.ToLower() != invitation.Email.ToLower())
                {
                    return ServiceResponse<string>.ErrorResult("Bu davetiye sizin email adresinize gönderilmemiş");
                }

                // Zaten üye mi?
                var isMember = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == userId &&
                    bm.BusinessId == invitation.BusinessId &&
                    bm.IsActive);

                if (isMember)
                {
                    return ServiceResponse<string>.ErrorResult("Zaten bu işletmenin üyesisiniz");
                }

                // Üyelik oluştur
                var membership = new BusinessMember
                {
                    UserId = userId,
                    BusinessId = invitation.BusinessId,
                    Role = UserRole.Employee
                };

                _context.BusinessMembers.Add(membership);

                // Davetiye durumunu güncelle
                invitation.Status = InvitationStatus.Accepted;
                invitation.AcceptedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResponse<string>.SuccessResult(
                    invitation.Business.Name,
                    "Davetiye başarıyla kabul edildi"
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<string>.ErrorResult("Davetiye kabul edilirken hata oluştu", ex.Message);
            }
        }

        public async Task<ServiceResponse<List<InvitationResponse>>> GetUserInvitationsAsync(string email)
        {
            try
            {
                var invitations = await _context.Invitations
                    .Include(i => i.Business)
                    .Include(i => i.InvitedBy)
                    .Where(i =>
                        i.Email == email.ToLower() &&
                        i.Status == InvitationStatus.Pending &&
                        i.ExpiresAt > DateTime.UtcNow)
                    .Select(i => new InvitationResponse
                    {
                        Id = i.Id,
                        InvitationCode = i.InvitationCode,
                        Email = i.Email,
                        BusinessName = i.Business.Name,
                        InvitedByName = i.InvitedBy.FirstName + " " + i.InvitedBy.LastName,
                        Message = i.Message,
                        ExpiresAt = i.ExpiresAt,
                        CreatedAt = i.CreatedAt
                    })
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return ServiceResponse<List<InvitationResponse>>.SuccessResult(invitations);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<InvitationResponse>>.ErrorResult("Davetiyeler getirilirken hata oluştu", ex.Message);
            }
        }
    }
}