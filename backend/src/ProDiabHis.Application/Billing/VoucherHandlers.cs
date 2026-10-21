using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Billing;

// ---- Commands + Queries ----

public record ListVouchersQuery(bool? IsActive, int Page, int PageSize) : IRequest<Result<PagedResult<VoucherResponse>>>;
public record GetVoucherQuery(Guid Id) : IRequest<Result<VoucherResponse>>;
public record CreateVoucherCommand(CreateVoucherRequest Request) : IRequest<Result<VoucherResponse>>;
public record UpdateVoucherCommand(Guid Id, UpdateVoucherRequest Request) : IRequest<Result<VoucherResponse>>;
public record DeleteVoucherCommand(Guid Id) : IRequest<Result>;
public record ApplyVoucherCommand(Guid BillingId, ApplyVoucherRequest Request) : IRequest<Result<BillingResponse>>;

// ---- Logic thuan (de unit test) ----

/// <summary>Tinh toan / kiem tra voucher - ham thuan, khong phu thuoc DB.</summary>
public static class VoucherLogic
{
    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    /// <summary>Quy doi dong hoa don sang nhom voucher (EXAM/LAB/IMAGING/DRUG) hoac null neu khong thuoc nhom nao.
    /// ItemType LAB/RAD/DRUG map truc tiep; SERVICE map theo category cua dich vu danh muc.</summary>
    public static string? GroupOf(string itemType, string? serviceCategory)
    {
        switch (itemType?.ToUpperInvariant())
        {
            case "LAB": return VoucherScope.Lab;
            case "RAD": return VoucherScope.Imaging;
            case "DRUG": return VoucherScope.Drug;
        }
        return serviceCategory?.ToUpperInvariant() switch
        {
            "EXAM" or "CONSULTATION" => VoucherScope.Exam,
            "LAB" => VoucherScope.Lab,
            "RAD" or "IMAGING" => VoucherScope.Imaging,
            "PHARMACY" or "DRUG" => VoucherScope.Drug,
            _ => null
        };
    }

    /// <summary>Kiem tra voucher dung duoc khong. Tra ve (code, message) neu loi, null neu hop le.</summary>
    public static (string Code, string Message)? CheckUsable(Voucher v, DateOnly today)
    {
        if (!v.IsActive) return ("VOUCHER_INACTIVE", "Mã voucher đang bị vô hiệu hóa");
        if ((v.ValidFrom.HasValue && today < v.ValidFrom) || (v.ValidTo.HasValue && today > v.ValidTo))
            return ("VOUCHER_EXPIRED", "Mã voucher đã hết hạn");
        if (v.MaxUses.HasValue && v.UsedCount >= v.MaxUses)
            return ("VOUCHER_USAGE_EXCEEDED", "Mã voucher đã hết lượt sử dụng");
        return null;
    }

    /// <summary>Giam = % x tong line_total cac dong thuoc nhom. ALL = moi dong. Lam tron 2 chu so.
    /// <paramref name="categoryOf"/> tra ve category dich vu cho dong SERVICE (co the null).</summary>
    public static decimal CalculateDiscount(Voucher v, IEnumerable<BillingItem> items, Func<BillingItem, string?> categoryOf)
    {
        var eligible = items
            .Where(i => v.AppliesTo == VoucherScope.All || GroupOf(i.ItemType, categoryOf(i)) == v.AppliesTo)
            .Sum(i => i.LineTotal);
        return Math.Round(eligible * v.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
    }
}

internal static class VoucherMapper
{
    public static VoucherResponse ToDto(Voucher v) => new(
        v.Id, v.Code, v.Description, v.DiscountPercent, v.AppliesTo,
        v.ValidFrom, v.ValidTo, v.MaxUses, v.UsedCount, v.IsActive, v.CreatedAt);
}

// ---- Handlers CRUD ----

public class ListVouchersHandler : IRequestHandler<ListVouchersQuery, Result<PagedResult<VoucherResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    public ListVouchersHandler(IApplicationDbContext db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<PagedResult<VoucherResponse>>> Handle(ListVouchersQuery q, CancellationToken ct)
    {
        var page = Math.Max(q.Page, 1);
        var size = Math.Clamp(q.PageSize, 1, 100);
        var query = _db.Vouchers.Where(v => v.TenantId == _tenant.TenantId);
        if (q.IsActive.HasValue) query = query.Where(v => v.IsActive == q.IsActive.Value);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Result<PagedResult<VoucherResponse>>.Success(
            new PagedResult<VoucherResponse>(rows.Select(VoucherMapper.ToDto), page, size, total));
    }
}

public class GetVoucherHandler : IRequestHandler<GetVoucherQuery, Result<VoucherResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    public GetVoucherHandler(IApplicationDbContext db, ITenantProvider tenant) { _db = db; _tenant = tenant; }

    public async Task<Result<VoucherResponse>> Handle(GetVoucherQuery q, CancellationToken ct)
    {
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == q.Id && x.TenantId == _tenant.TenantId, ct);
        return v == null
            ? Result<VoucherResponse>.Failure("VOUCHER_NOT_FOUND", "Không tìm thấy mã voucher")
            : Result<VoucherResponse>.Success(VoucherMapper.ToDto(v));
    }
}

public class CreateVoucherHandler : IRequestHandler<CreateVoucherCommand, Result<VoucherResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    public CreateVoucherHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<VoucherResponse>> Handle(CreateVoucherCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var code = VoucherLogic.NormalizeCode(r.Code);
        var tenantId = _tenant.TenantId;

        // Ke ca voucher da soft-delete van chiem unique (tenant_id, code) o DB -> kiem tra bo qua query filter.
        var dup = await _db.Vouchers.IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && v.Code == code, ct);
        if (dup)
            return Result<VoucherResponse>.Failure("VOUCHER_CODE_DUPLICATE", "Mã voucher đã tồn tại");

        var v = new Voucher
        {
            TenantId = tenantId,
            Code = code,
            Description = r.Description,
            DiscountPercent = r.DiscountPercent,
            AppliesTo = r.AppliesTo,
            ValidFrom = r.ValidFrom,
            ValidTo = r.ValidTo,
            MaxUses = r.MaxUses,
            IsActive = r.IsActive,
            CreatedBy = _user.UserId
        };
        _db.Vouchers.Add(v);
        await _db.SaveChangesAsync(ct);
        return Result<VoucherResponse>.Success(VoucherMapper.ToDto(v));
    }
}

public class UpdateVoucherHandler : IRequestHandler<UpdateVoucherCommand, Result<VoucherResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    public UpdateVoucherHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<VoucherResponse>> Handle(UpdateVoucherCommand cmd, CancellationToken ct)
    {
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (v == null) return Result<VoucherResponse>.Failure("VOUCHER_NOT_FOUND", "Không tìm thấy mã voucher");

        var r = cmd.Request;
        if (r.MaxUses.HasValue && r.MaxUses.Value < v.UsedCount)
            return Result<VoucherResponse>.Failure("VOUCHER_MAX_USES_INVALID",
                "Số lượt dùng tối đa không được nhỏ hơn số lượt đã dùng");

        // Khong cho doi code (da in/phat hanh) - chi sua cac thuoc tinh con lai.
        v.Description = r.Description;
        v.DiscountPercent = r.DiscountPercent;
        v.AppliesTo = r.AppliesTo;
        v.ValidFrom = r.ValidFrom;
        v.ValidTo = r.ValidTo;
        v.MaxUses = r.MaxUses;
        v.IsActive = r.IsActive;
        v.UpdatedAt = DateTime.UtcNow;
        v.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Result<VoucherResponse>.Success(VoucherMapper.ToDto(v));
    }
}

public class DeleteVoucherHandler : IRequestHandler<DeleteVoucherCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    public DeleteVoucherHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result> Handle(DeleteVoucherCommand cmd, CancellationToken ct)
    {
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (v == null) return Result.Failure("VOUCHER_NOT_FOUND", "Không tìm thấy mã voucher");
        v.DeletedAt = DateTime.UtcNow;
        v.IsActive = false;
        v.UpdatedAt = DateTime.UtcNow;
        v.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ---- Ap voucher vao hoa don ----

public class ApplyVoucherHandler : IRequestHandler<ApplyVoucherCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    public ApplyVoucherHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<BillingResponse>> Handle(ApplyVoucherCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.BillingId && x.TenantId == tenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Không tìm thấy hóa đơn");
        if (b.Status != BillingStatus.Draft)
            return Result<BillingResponse>.Failure("BILLING_ALREADY_FINALIZED", "Chỉ áp voucher cho hóa đơn nháp");

        var code = VoucherLogic.NormalizeCode(cmd.Request.Code);
        var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (v == null) return Result<BillingResponse>.Failure("VOUCHER_NOT_FOUND", "Không tìm thấy mã voucher");

        var err = VoucherLogic.CheckUsable(v, DateOnly.FromDateTime(DateTime.Now));
        if (err != null) return Result<BillingResponse>.Failure(err.Value.Code, err.Value.Message);

        if (await _db.VoucherUsages.AnyAsync(u => u.TenantId == tenantId && u.BillingId == b.Id && u.VoucherId == v.Id, ct))
            return Result<BillingResponse>.Failure("VOUCHER_ALREADY_APPLIED", "Mã voucher đã được áp cho hóa đơn này");

        // Category dich vu cho cac dong SERVICE (BillingItem khong luu category -> join theo RefId).
        var refIds = b.Items.Where(i => i.RefId.HasValue && i.ItemType == "SERVICE").Select(i => i.RefId!.Value).Distinct().ToList();
        var cats = refIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.BillingServices.Where(s => s.TenantId == tenantId && refIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Category, ct);

        var discount = VoucherLogic.CalculateDiscount(v, b.Items,
            i => i.RefId.HasValue && cats.TryGetValue(i.RefId.Value, out var c) ? c : null);
        if (discount <= 0)
            return Result<BillingResponse>.Failure("VOUCHER_NOT_APPLICABLE",
                "Hóa đơn không có dịch vụ thuộc nhóm áp dụng của voucher");

        // Khong cho tong giam vuot tong tien hang (Subtotal).
        discount = Math.Min(discount, Math.Max(0, b.Subtotal - b.DiscountAmount));

        b.DiscountAmount += discount;
        BillingMapper.Recalculate(b);
        v.UsedCount += 1;
        v.UpdatedAt = DateTime.UtcNow;
        _db.VoucherUsages.Add(new VoucherUsage
        {
            TenantId = tenantId,
            VoucherId = v.Id,
            BillingId = b.Id,
            DiscountAmount = discount,
            UsedBy = _user.UserId
        });
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}
