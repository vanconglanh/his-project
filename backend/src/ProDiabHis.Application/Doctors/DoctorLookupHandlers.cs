using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Doctors;

// ═══════════════════════════════════════════════════════════════════════════════
// P2-07: Danh ba bac si RUT GON de dat lich / tao luot kham.
// Chi tra id + ho ten + chuyen khoa (neu co) - KHONG tra email/phone/trang thai
// tai khoan/vai tro/chi nhanh nhu GET /users. Muc tieu: sau khi FE chuyen sang
// endpoint nay se bo quyen user.read khoi le_tan/bac_si (xem phan B5 cua 9141).
// ═══════════════════════════════════════════════════════════════════════════════

public record DoctorLookupItem(Guid Id, string FullName, string? Specialty);

public record DoctorLookupQuery(string? Q) : IRequest<IReadOnlyList<DoctorLookupItem>>;

public class DoctorLookupQueryHandler : IRequestHandler<DoctorLookupQuery, IReadOnlyList<DoctorLookupItem>>
{
    private readonly IApplicationDbContext _db;

    public DoctorLookupQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DoctorLookupItem>> Handle(DoctorLookupQuery request, CancellationToken ct)
    {
        var query = _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null
                && u.Status == UserStatus.Active
                && u.UserRoles.Any(ur => ur.Role != null && ur.Role.Code == "bac_si"));

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(u => u.FullName.Contains(request.Q));

        var users = await query
            .OrderBy(u => u.FullName)
            .Take(200)
            .ToListAsync(ct);

        // GHI CHU: he thong hien chua co truong "chuyen khoa" rieng cho bac si
        // (khong ton tai cot Specialty tren entity User) -> tra null. Neu sau nay
        // bo sung bang/cot chuyen khoa, cap nhat mapping tai day.
        return users.Select(u => new DoctorLookupItem(u.Id, u.FullName, null)).ToList();
    }
}
