using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Leave;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;

namespace Personelim.Services.Leave
{
    public class LeaveService : ILeaveService
    {
        private readonly AppDbContext _context;

        public LeaveService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<LeaveResponse>> CreateLeaveRequestAsync(Guid userId, CreateLeaveRequest request)
        {
            
            if (request.StartDate > request.EndDate)
                return ServiceResponse<LeaveResponse>.ErrorResult("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

           
            var member = await _context.BusinessMembers
                .Include(bm => bm.User)
                .FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == request.BusinessId && bm.IsActive);

            if (member == null)
                return ServiceResponse<LeaveResponse>.ErrorResult("İşletme üyeliği bulunamadı.");

            var leave = new MemberLeave
            {
                BusinessMemberId = member.Id,
                Title = request.Title,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = LeaveStatus.Pending
            };

            _context.MemberLeaves.Add(leave);
            await _context.SaveChangesAsync();

            return ServiceResponse<LeaveResponse>.SuccessResult(MapToResponse(leave, member.User.FirstName + " " + member.User.LastName), "İzin talebi oluşturuldu.");
        }

        public async Task<ServiceResponse<List<LeaveResponse>>> GetMyLeavesAsync(Guid userId, Guid businessId)
        {
            var member = await _context.BusinessMembers
                .FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.IsActive);

            if (member == null) return ServiceResponse<List<LeaveResponse>>.ErrorResult("Üyelik bulunamadı.");

            var leaves = await _context.MemberLeaves
                .Where(l => l.BusinessMemberId == member.Id)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // İsim bilgisi için user çekmemize gerek yok, kendi izinleri zaten
            // Ancak genel map fonksiyonu için isim gerekirse boş geçebiliriz veya user çekebilirdik.
            var list = leaves.Select(l => MapToResponse(l, "Siz")).ToList();
            
            return ServiceResponse<List<LeaveResponse>>.SuccessResult(list);
        }

        public async Task<ServiceResponse<List<LeaveResponse>>> GetBusinessLeavesAsync(Guid userId, Guid businessId)
        {
            // Yetki Kontrolü: Sadece Owner (veya Manager) görebilir
            var isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.Role == UserRole.Owner && bm.IsActive);

            if (!isOwner) return ServiceResponse<List<LeaveResponse>>.ErrorResult("Yetkiniz yok.");

            var leaves = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .ThenInclude(bm => bm.User)
                .Where(l => l.BusinessMember.BusinessId == businessId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var list = leaves.Select(l => MapToResponse(l, l.BusinessMember.User.FirstName + " " + l.BusinessMember.User.LastName)).ToList();

            return ServiceResponse<List<LeaveResponse>>.SuccessResult(list);
        }

        public async Task<ServiceResponse<LeaveResponse>> UpdateLeaveStatusAsync(Guid userId, Guid leaveId, UpdateLeaveStatusRequest request)
        {
            var leave = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .ThenInclude(bm => bm.User)
                .FirstOrDefaultAsync(l => l.Id == leaveId);

            if (leave == null) return ServiceResponse<LeaveResponse>.ErrorResult("İzin talebi bulunamadı.");

            // İşlemi yapan kişi o işletmenin sahibi mi?
            var isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == leave.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);

            if (!isOwner) return ServiceResponse<LeaveResponse>.ErrorResult("Bu işlemi yapmaya yetkiniz yok.");

            leave.Status = request.Status;
            if (request.Status == LeaveStatus.Rejected)
            {
                leave.RejectionReason = request.RejectionReason;
            }
            leave.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResponse<LeaveResponse>.SuccessResult(
                MapToResponse(leave, leave.BusinessMember.User.FirstName + " " + leave.BusinessMember.User.LastName), 
                $"İzin durumu {request.Status} olarak güncellendi.");
        }

        public async Task<ServiceResponse<bool>> DeleteLeaveAsync(Guid userId, Guid leaveId)
        {
            var leave = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .FirstOrDefaultAsync(l => l.Id == leaveId);

            if (leave == null) return ServiceResponse<bool>.ErrorResult("Talep bulunamadı.");

            // Silme Kuralı:
            // 1. Sahibi silebilir (Eğer henüz onaylanmadıysa - Pending)
            // 2. İşletme sahibi silebilir.
            
            bool isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == leave.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
            
            bool isSelf = leave.BusinessMember.UserId == userId;

            if (!isOwner && !isSelf) return ServiceResponse<bool>.ErrorResult("Yetkiniz yok.");

            if (isSelf && leave.Status != LeaveStatus.Pending)
            {
                return ServiceResponse<bool>.ErrorResult("Onaylanmış veya reddedilmiş izinleri silemezsiniz. Yöneticinizle görüşün.");
            }

            _context.MemberLeaves.Remove(leave);
            await _context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResult(true, "İzin talebi silindi.");
        }
        
        private LeaveResponse MapToResponse(MemberLeave leave, string memberName)
        {
            var dayDiff = (leave.EndDate - leave.StartDate).Days;
            if (dayDiff == 0) dayDiff = 1; 

            return new LeaveResponse
            {
                Id = leave.Id,
                MemberName = memberName,
                Title = leave.Title,
                Description = leave.Description,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                DayCount = dayDiff,
                Status = leave.Status.ToString(),
                RejectionReason = leave.RejectionReason,
                CreatedAt = leave.CreatedAt
            };
        }
    }
}