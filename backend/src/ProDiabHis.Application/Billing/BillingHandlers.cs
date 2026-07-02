using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Application.Billing;

// ---- Commands + Queries ----

public record CreateBillingCommand(CreateBillingRequest Request) : IRequest<Result<BillingResponse>>;
public record GetBillingQuery(Guid Id) : IRequest<Result<BillingResponse>>;
public record ListBillingsQuery(
    string? Status, Guid? PatientId, Guid? EncounterId,
    DateOnly? FromDate, DateOnly? ToDate, string? Payer,
    int Page, int PageSize)
    : IRequest<Result<PagedResult<BillingResponse>>>;
public record UpdateBillingCommand(Guid Id, UpdateBillingRequest Request) : IRequest<Result<BillingResponse>>;
public record AddBillingItemCommand(Guid BillingId, BillingItemUpsertRequest Request) : IRequest<Result<BillingResponse>>;
public record DeleteBillingItemCommand(Guid ItemId) : IRequest<Result>;
public record FinalizeBillingCommand(Guid Id) : IRequest<Result<BillingResponse>>;
public record VoidBillingCommand(Guid Id, VoidBillingRequest Request) : IRequest<Result<BillingResponse>>;
public record ApplyBhytCommand(Guid Id, ApplyBhytRequest Request) : IRequest<Result<BillingResponse>>;
public record GetBillingByEncounterQuery(Guid EncounterId) : IRequest<Result<List<BillingResponse>>>;
public record ExportBillingPdfQuery(Guid Id) : IRequest<Result<byte[]>>;

// ---- Mapping helpers ----

internal static class BillingMapper
{
    public static BillingItemDto ToItemDto(BillingItem i) => new(
        i.Id, i.ItemType, i.RefId, i.Code, i.Name,
        i.Quantity, i.UnitPrice, i.VatRate, i.DiscountPercent,
        i.LineTotal, i.BhytApplicable, i.BhytAmount);

    public static BillingResponse ToDto(Domain.Entities.Billing b, PatientSummaryDto? patient = null)
    {
        var items = b.Items.Select(ToItemDto).ToList();
        return new BillingResponse(
            b.Id, b.TenantId, b.EncounterId, b.PatientId, patient,
            b.BillNo, items, b.Subtotal, b.VatTotal, b.DiscountAmount,
            b.BhytAmount, b.PatientPayable, b.PaidAmount, b.Balance,
            b.Status, b.PaymentDueDate, b.Payer, b.Note,
            b.CreatedAt, b.CreatedBy, b.FinalizedAt, b.VoidReason);
    }

    public static void Recalculate(Domain.Entities.Billing b)
    {
        b.Subtotal = b.Items.Sum(i => i.LineTotal);
        b.VatTotal = b.Items.Sum(i => i.LineTotal * i.VatRate / 100);
        b.PatientPayable = b.Subtotal + b.VatTotal - b.DiscountAmount - b.BhytAmount;
        b.Balance = b.PatientPayable - b.PaidAmount;
    }
}

// ---- Handlers ----

public class CreateBillingHandler : IRequestHandler<CreateBillingCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IBillingCalculator _calculator;
    private readonly IDapperConnectionFactory _dapper;

    public CreateBillingHandler(
        IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user,
        IBillingCalculator calculator, IDapperConnectionFactory dapper)
    {
        _db = db; _tenant = tenant; _user = user;
        _calculator = calculator; _dapper = dapper;
    }

    public async Task<Result<BillingResponse>> Handle(CreateBillingCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var tenantId = _tenant.TenantId;

        // Check existing DRAFT/FINALIZED for same encounter
        var existing = await _db.Billings
            .AnyAsync(b => b.EncounterId == req.EncounterId && b.TenantId == tenantId
                && b.Status != BillingStatus.Void, ct);
        if (existing)
            return Result<BillingResponse>.Failure("BILLING_ALREADY_EXISTS",
                "Da co hoa don cho luot kham nay");

        var items = await _calculator.BuildItemsFromEncounterAsync(
            req.EncounterId, tenantId, req.IncludeDispensing, ct);

        // Generate bill_no
        var billNo = $"HD-{DateTime.Now:yyyyMM}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";

        var billing = new Domain.Entities.Billing
        {
            TenantId = tenantId,
            EncounterId = req.EncounterId,
            PatientId = await GetPatientIdFromEncounterAsync(req.EncounterId),
            BillNo = billNo,
            Payer = req.Payer,
            Note = req.Note,
            Status = BillingStatus.Draft,
            CreatedBy = _user.UserId,
            Items = items
        };

        BillingMapper.Recalculate(billing);
        _db.Billings.Add(billing);
        await _db.SaveChangesAsync(ct);

        return Result<BillingResponse>.Success(BillingMapper.ToDto(billing));
    }

    private async Task<Guid> GetPatientIdFromEncounterAsync(Guid encounterId)
    {
        using var conn = _dapper.CreateConnection();
        var pid = await conn.ExecuteScalarAsync<string?>(
            "SELECT patient_id FROM diab_his_enc_encounters WHERE id = @id AND deleted_at IS NULL", new { id = encounterId.ToString() });
        return pid != null ? Guid.Parse(pid) : Guid.Empty;
    }
}

public class GetBillingHandler : IRequestHandler<GetBillingQuery, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IDapperConnectionFactory _dapper;

    public GetBillingHandler(IApplicationDbContext db, ITenantProvider tenant, IDapperConnectionFactory dapper)
    {
        _db = db; _tenant = tenant; _dapper = dapper;
    }

    public async Task<Result<BillingResponse>> Handle(GetBillingQuery query, CancellationToken ct)
    {
        var b = await _db.Billings
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");

        var patient = await GetPatientSummaryAsync(b.PatientId);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b, patient));
    }

    internal async Task<PatientSummaryDto?> GetPatientSummaryAsync(Guid patientId)
    {
        using var conn = _dapper.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT full_name, dob, gender, phone FROM diab_his_pat_patients WHERE id = @id AND deleted_at IS NULL",
            new { id = patientId.ToString() });
        if (row == null) return null;
        return new PatientSummaryDto(
            (string)row.full_name,
            row.dob == null ? null : DateOnly.FromDateTime((DateTime)row.dob),
            (string?)row.gender,
            (string?)row.phone,
            null);
    }
}

public class ListBillingsHandler : IRequestHandler<ListBillingsQuery, Result<PagedResult<BillingResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListBillingsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<BillingResponse>>> Handle(ListBillingsQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE b.tenant_id = @tenantId AND b.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("tenantId", _tenant.TenantId);

        if (!string.IsNullOrEmpty(query.Status)) { where += " AND b.status = @status"; p.Add("status", query.Status); }
        if (query.PatientId.HasValue) { where += " AND b.patient_id = @pid"; p.Add("pid", query.PatientId.ToString()); }
        if (query.EncounterId.HasValue) { where += " AND b.encounter_id = @eid"; p.Add("eid", query.EncounterId.ToString()); }
        if (query.FromDate.HasValue) { where += " AND DATE(b.created_at) >= @from"; p.Add("from", query.FromDate.Value.ToString("yyyy-MM-dd")); }
        if (query.ToDate.HasValue) { where += " AND DATE(b.created_at) <= @to"; p.Add("to", query.ToDate.Value.ToString("yyyy-MM-dd")); }
        if (!string.IsNullOrEmpty(query.Payer)) { where += " AND b.payer = @payer"; p.Add("payer", query.Payer); }

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_bil_billing b {where}", p);
        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT b.* FROM diab_his_bil_billing b {where}
               ORDER BY b.created_at DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r => new BillingResponse(
            Guid.TryParse((string?)r.id, out var gId) ? gId : Guid.Empty,
            (int)r.tenant_id,
            r.encounter_id == null ? null : (Guid.TryParse((string?)r.encounter_id, out var gEid) ? gEid : (Guid?)null),
            Guid.TryParse((string?)r.patient_id, out var gPid) ? gPid : Guid.Empty,
            null,
            (string?)r.bill_no, [],
            r.subtotal == null ? 0m : (decimal)r.subtotal,
            r.vat_total == null ? 0m : (decimal)r.vat_total,
            r.discount_amount == null ? 0m : (decimal)r.discount_amount,
            r.bhyt_amount == null ? 0m : (decimal)r.bhyt_amount,
            r.patient_payable == null ? 0m : (decimal)r.patient_payable,
            r.paid_amount == null ? 0m : (decimal)r.paid_amount,
            r.balance == null ? 0m : (decimal)r.balance,
            (string)r.status,
            r.payment_due_date == null ? null : DateOnly.FromDateTime((DateTime)r.payment_due_date),
            (string)r.payer, (string?)r.note,
            (DateTime)r.created_at,
            r.created_by == null ? null : (Guid.TryParse((string?)r.created_by, out var gCby) ? gCby : (Guid?)null),
            r.finalized_at == null ? null : (DateTime?)r.finalized_at,
            (string?)r.void_reason)).ToList();

        return Result<PagedResult<BillingResponse>>.Success(
            new PagedResult<BillingResponse>(items, query.Page, query.PageSize, total));
    }
}

public class UpdateBillingHandler : IRequestHandler<UpdateBillingCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public UpdateBillingHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BillingResponse>> Handle(UpdateBillingCommand cmd, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");
        if (b.Status != BillingStatus.Draft)
            return Result<BillingResponse>.Failure("BILLING_ALREADY_FINALIZED", "Hoa don da finalized");

        if (cmd.Request.Note != null) b.Note = cmd.Request.Note;
        if (cmd.Request.DiscountAmount.HasValue)
        {
            b.DiscountAmount = cmd.Request.DiscountAmount.Value;
            BillingMapper.Recalculate(b);
        }
        if (cmd.Request.PaymentDueDate.HasValue) b.PaymentDueDate = cmd.Request.PaymentDueDate;
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}

public class AddBillingItemHandler : IRequestHandler<AddBillingItemCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public AddBillingItemHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BillingResponse>> Handle(AddBillingItemCommand cmd, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.BillingId && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");
        if (b.Status != BillingStatus.Draft)
            return Result<BillingResponse>.Failure("BILLING_ALREADY_FINALIZED", "Hoa don da finalized");

        var req = cmd.Request;
        var lineTotal = req.Quantity * req.UnitPrice * (1 - req.DiscountPercent / 100);
        var item = new BillingItem
        {
            BillingId = b.Id,
            TenantId = b.TenantId,
            ItemType = req.Type,
            RefId = req.RefId,
            Code = req.Code,
            Name = req.Name,
            Quantity = req.Quantity,
            UnitPrice = req.UnitPrice,
            VatRate = req.VatRate,
            DiscountPercent = req.DiscountPercent,
            LineTotal = lineTotal,
            BhytApplicable = req.BhytApplicable
        };
        b.Items.Add(item);
        BillingMapper.Recalculate(b);
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}

public class DeleteBillingItemHandler : IRequestHandler<DeleteBillingItemCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public DeleteBillingItemHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result> Handle(DeleteBillingItemCommand cmd, CancellationToken ct)
    {
        var item = await _db.BillingItems.FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct);
        if (item == null) return Result.Failure("ITEM_NOT_FOUND", "Khong tim thay item");

        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == item.BillingId && x.TenantId == _tenant.TenantId, ct);
        if (b == null || b.Status != BillingStatus.Draft)
            return Result.Failure("BILLING_NOT_EDITABLE", "Khong the xoa item cua hoa don nay");

        _db.BillingItems.Remove(item);
        b.Items.Remove(item);
        BillingMapper.Recalculate(b);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class FinalizeBillingHandler : IRequestHandler<FinalizeBillingCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public FinalizeBillingHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BillingResponse>> Handle(FinalizeBillingCommand cmd, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");
        if (b.Status == BillingStatus.Finalized)
            return Result<BillingResponse>.Failure("BILLING_ALREADY_FINALIZED", "Hoa don da finalized");
        if (b.Status == BillingStatus.Void)
            return Result<BillingResponse>.Failure("BILLING_VOID", "Hoa don da huy");

        b.Status = BillingStatus.Finalized;
        b.FinalizedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}

public class VoidBillingHandler : IRequestHandler<VoidBillingCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public VoidBillingHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<BillingResponse>> Handle(VoidBillingCommand cmd, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");

        b.Status = BillingStatus.Void;
        b.VoidReason = cmd.Request.Reason;
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}

public class ApplyBhytHandler : IRequestHandler<ApplyBhytCommand, Result<BillingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IBhytCoPayCalculator _calculator;

    public ApplyBhytHandler(IApplicationDbContext db, ITenantProvider tenant, IBhytCoPayCalculator calculator)
    {
        _db = db; _tenant = tenant; _calculator = calculator;
    }

    public async Task<Result<BillingResponse>> Handle(ApplyBhytCommand cmd, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<BillingResponse>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");
        if (b.Status == BillingStatus.Void)
            return Result<BillingResponse>.Failure("BILLING_INVALID_BHYT", "Hoa don da huy");

        var req = cmd.Request;
        var result = _calculator.Calculate(new BhytCoPayInput(
            req.BhytCardNo, req.CopayRate, req.RightRoute, b.Items.ToList()));

        foreach (var r in result.ItemResults)
        {
            var item = b.Items.FirstOrDefault(i => i.Id == r.ItemId);
            if (item != null) item.BhytAmount = r.BhytAmount;
        }

        b.BhytAmount = result.BhytAmount;
        b.PatientPayable = result.PatientPayable;
        b.RightRoute = req.RightRoute;
        b.Balance = b.PatientPayable - b.PaidAmount;
        await _db.SaveChangesAsync(ct);
        return Result<BillingResponse>.Success(BillingMapper.ToDto(b));
    }
}

public class GetBillingByEncounterHandler : IRequestHandler<GetBillingByEncounterQuery, Result<List<BillingResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public GetBillingByEncounterHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<List<BillingResponse>>> Handle(GetBillingByEncounterQuery query, CancellationToken ct)
    {
        var billings = await _db.Billings
            .Include(b => b.Items)
            .Where(b => b.EncounterId == query.EncounterId && b.TenantId == _tenant.TenantId)
            .ToListAsync(ct);
        return Result<List<BillingResponse>>.Success(billings.Select(b => BillingMapper.ToDto(b)).ToList());
    }
}

public class ExportBillingPdfHandler : IRequestHandler<ExportBillingPdfQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public ExportBillingPdfHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<byte[]>> Handle(ExportBillingPdfQuery query, CancellationToken ct)
    {
        var b = await _db.Billings.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.TenantId == _tenant.TenantId, ct);
        if (b == null) return Result<byte[]>.Failure("BILLING_NOT_FOUND", "Khong tim thay hoa don");

        // Simple PDF placeholder via QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Item().Text($"HOA DON: {b.BillNo}").FontSize(18).Bold();
                    col.Item().Text($"Trang thai: {b.Status}");
                    col.Item().Text($"Tong tien: {b.PatientPayable:N0} VND");
                    foreach (var item in b.Items)
                        col.Item().Text($"- {item.Name}: {item.Quantity} x {item.UnitPrice:N0} = {item.LineTotal:N0}");
                });
            });
        }).GeneratePdf();

        return Result<byte[]>.Success(pdf);
    }
}
