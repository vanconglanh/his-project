using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using System.Text.Json;

namespace ProDiabHis.Application.Billing;

public record OpenShiftCommand(OpenShiftRequest Request) : IRequest<Result<CashierClosingResponse>>;
public record CloseShiftCommand(CloseShiftRequest Request) : IRequest<Result<CashierClosingResponse>>;
public record GetTodayReportQuery(Guid? CashierUserId, DateOnly? Date) : IRequest<Result<CashierClosingResponse>>;
public record ListShiftsQuery(
    Guid? CashierUserId, DateOnly? FromDate, DateOnly? ToDate,
    string? Status, int Page, int PageSize)
    : IRequest<Result<PagedResult<CashierClosingResponse>>>;
public record ExportShiftPdfQuery(Guid Id) : IRequest<Result<byte[]>>;
public record GetDebtsQuery(string? Q, decimal MinBalance, int? OlderThanDays, int Page, int PageSize)
    : IRequest<Result<(List<DebtResponse> Items, int Total, decimal TotalDebt)>>;
// GetCurrentShiftQuery defined at bottom of file

internal static class ShiftMapper
{
    public static CashierClosingResponse ToDto(CashierShift s, string? cashierName = null)
    {
        List<BreakdownByMethodDto> breakdown = [];
        if (!string.IsNullOrEmpty(s.BreakdownJson))
        {
            try
            {
                breakdown = JsonSerializer.Deserialize<List<BreakdownByMethodDto>>(s.BreakdownJson) ?? [];
            }
            catch { }
        }

        var gross = s.TotalCash + s.TotalCard + s.TotalTransfer + s.TotalQr + s.TotalOther;
        var net = gross - s.TotalRefund - s.TotalVoid;

        var summary = new ShiftSummaryDto(
            s.TotalCash, s.TotalCard, s.TotalTransfer, s.TotalQr, s.TotalOther,
            s.TotalRefund, s.TotalVoid, s.CountTransactions, gross, net, breakdown);

        return new CashierClosingResponse(
            s.Id, s.TenantId, s.CashierUserId, cashierName,
            s.ShiftDate, s.ShiftStart, s.ShiftEnd,
            summary, s.OpeningBalance, s.ClosingBalance,
            s.ExpectedCash, s.ActualCash, s.Difference,
            s.Note, s.Status, s.ClosedBy, s.CreatedAt);
    }
}

public class OpenShiftHandler : IRequestHandler<OpenShiftCommand, Result<CashierClosingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public OpenShiftHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<CashierClosingResponse>> Handle(OpenShiftCommand cmd, CancellationToken ct)
    {
        if (!_user.UserId.HasValue)
            return Result<CashierClosingResponse>.Failure("AUTH_REQUIRED", "Can dang nhap");

        var userId = _user.UserId.Value;
        var tenantId = _tenant.TenantId;

        // Check no open shift for user
        var hasOpen = await _db.CashierShifts
            .AnyAsync(s => s.CashierUserId == userId && s.TenantId == tenantId && s.Status == "OPEN", ct);
        if (hasOpen)
            return Result<CashierClosingResponse>.Failure("CASHIER_SHIFT_NOT_OPEN",
                "User da co ca dang mo. Vui long dong ca truoc.");

        // Opening balance from last closed shift
        var lastShift = await _db.CashierShifts
            .Where(s => s.CashierUserId == userId && s.TenantId == tenantId && s.Status == "CLOSED")
            .OrderByDescending(s => s.ShiftEnd)
            .FirstOrDefaultAsync(ct);

        var openingBalance = cmd.Request.OpeningBalance > 0
            ? cmd.Request.OpeningBalance
            : (lastShift?.ClosingBalance ?? 0);

        var shift = new CashierShift
        {
            TenantId = tenantId,
            CashierUserId = userId,
            ShiftDate = DateOnly.FromDateTime(DateTime.Today),
            ShiftStart = DateTime.UtcNow,
            OpeningBalance = openingBalance,
            Status = "OPEN",
            Note = cmd.Request.Note
        };
        _db.CashierShifts.Add(shift);
        await _db.SaveChangesAsync(ct);
        return Result<CashierClosingResponse>.Success(ShiftMapper.ToDto(shift));
    }
}

public class CloseShiftHandler : IRequestHandler<CloseShiftCommand, Result<CashierClosingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly ICashierShiftService _shiftSvc;

    public CloseShiftHandler(
        IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, ICashierShiftService shiftSvc)
    {
        _db = db; _tenant = tenant; _user = user; _shiftSvc = shiftSvc;
    }

    public async Task<Result<CashierClosingResponse>> Handle(CloseShiftCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        var tenantId = _tenant.TenantId;

        CashierShift? shift;
        if (req.ShiftId.HasValue)
        {
            shift = await _db.CashierShifts.FirstOrDefaultAsync(
                s => s.Id == req.ShiftId.Value && s.TenantId == tenantId, ct);
        }
        else
        {
            shift = _user.UserId.HasValue
                ? await _shiftSvc.GetOpenShiftAsync(_user.UserId.Value, tenantId, ct)
                : null;
        }

        if (shift == null) return Result<CashierClosingResponse>.Failure("SHIFT_NOT_FOUND", "Khong tim thay ca thu ngan");
        if (shift.Status == "CLOSED")
            return Result<CashierClosingResponse>.Failure("CASHIER_SHIFT_ALREADY_CLOSED", "Ca da dong");

        // Calculate summary from payments
        shift = await _shiftSvc.CalculateShiftSummaryAsync(shift, ct);

        shift.ActualCash = req.ActualCash;
        shift.ExpectedCash = shift.OpeningBalance + shift.TotalCash - shift.TotalRefund;
        shift.Difference = req.ActualCash - shift.ExpectedCash;
        shift.ClosingBalance = req.ActualCash + shift.TotalCard + shift.TotalTransfer + shift.TotalQr;
        shift.ShiftEnd = DateTime.UtcNow;
        shift.Status = "CLOSED";
        shift.Note = req.Note ?? shift.Note;
        shift.ClosedBy = _user.UserId;

        if (shift.Difference != 0 && !req.AcceptDifference)
            return Result<CashierClosingResponse>.Failure("CASHIER_CASH_DIFFERENCE",
                $"Chenh lech tien mat: {shift.Difference:N0} VND. Set accept_difference=true de chap nhan.");

        await _db.SaveChangesAsync(ct);
        return Result<CashierClosingResponse>.Success(ShiftMapper.ToDto(shift));
    }
}

public class GetTodayReportHandler : IRequestHandler<GetTodayReportQuery, Result<CashierClosingResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public GetTodayReportHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<CashierClosingResponse>> Handle(GetTodayReportQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var date = query.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var userId = query.CashierUserId ?? _user.UserId;

        var where = "WHERE s.tenant_id = @tenantId AND s.shift_date = @date";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        p.Add("date", date.ToString("yyyy-MM-dd"));
        if (userId.HasValue) { where += " AND s.cashier_user_id = @userId"; p.Add("userId", userId.ToString()); }

        var shift = await conn.QueryFirstOrDefaultAsync<dynamic>(
            $"SELECT s.*, u.full_name as cashier_name FROM diab_his_bil_cashier_shifts s " +
            $"LEFT JOIN sec_users u ON u.id = CONVERT(s.cashier_user_id USING utf8mb4) COLLATE utf8mb4_0900_ai_ci {where} ORDER BY s.shift_start DESC LIMIT 1", p);

        if (shift == null)
        {
            // Return empty report for today
            var empty = new CashierShift
            {
                TenantId = tenantId,
                ShiftDate = date,
                ShiftStart = DateTime.UtcNow
            };
            return Result<CashierClosingResponse>.Success(ShiftMapper.ToDto(empty));
        }

        var s = MapShift(shift);
        return Result<CashierClosingResponse>.Success(ShiftMapper.ToDto(s, (string?)shift.cashier_name));
    }

    public static CashierShift MapShift(dynamic r) => new()
    {
        Id = Guid.Parse((string)r.id),
        TenantId = (int)r.tenant_id,
        CashierUserId = Guid.Parse((string)r.cashier_user_id),
        ShiftDate = DateOnly.FromDateTime((DateTime)r.shift_date),
        ShiftStart = (DateTime)r.shift_start,
        ShiftEnd = r.shift_end == null ? null : (DateTime?)r.shift_end,
        OpeningBalance = (decimal)r.opening_balance,
        ClosingBalance = r.closing_balance == null ? null : (decimal?)r.closing_balance,
        ExpectedCash = r.expected_cash == null ? null : (decimal?)r.expected_cash,
        ActualCash = r.actual_cash == null ? null : (decimal?)r.actual_cash,
        Difference = r.difference == null ? null : (decimal?)r.difference,
        TotalCash = (decimal)r.total_cash,
        TotalCard = (decimal)r.total_card,
        TotalTransfer = (decimal)r.total_transfer,
        TotalQr = (decimal)r.total_qr,
        TotalOther = (decimal)r.total_other,
        TotalRefund = (decimal)r.total_refund,
        TotalVoid = (decimal)r.total_void,
        CountTransactions = (int)r.count_transactions,
        BreakdownJson = (string?)r.breakdown_json,
        Status = (string)r.status,
        Note = (string?)r.note
    };
}

public class ListShiftsHandler : IRequestHandler<ListShiftsQuery, Result<PagedResult<CashierClosingResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListShiftsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PagedResult<CashierClosingResponse>>> Handle(ListShiftsQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE s.tenant_id = @tenantId";
        var p = new DynamicParameters();
        p.Add("tenantId", _tenant.TenantId);

        if (query.CashierUserId.HasValue) { where += " AND s.cashier_user_id = @uid"; p.Add("uid", query.CashierUserId.ToString()); }
        if (query.FromDate.HasValue) { where += " AND s.shift_date >= @from"; p.Add("from", query.FromDate.Value.ToString("yyyy-MM-dd")); }
        if (query.ToDate.HasValue) { where += " AND s.shift_date <= @to"; p.Add("to", query.ToDate.Value.ToString("yyyy-MM-dd")); }
        if (!string.IsNullOrEmpty(query.Status)) { where += " AND s.status = @status"; p.Add("status", query.Status); }

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_bil_cashier_shifts s {where}", p);

        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT s.*, u.full_name as cashier_name
               FROM diab_his_bil_cashier_shifts s
               LEFT JOIN sec_users u ON u.id = CONVERT(s.cashier_user_id USING utf8mb4) COLLATE utf8mb4_0900_ai_ci
               {where} ORDER BY s.shift_start DESC LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r =>
        {
            var shift = GetTodayReportHandler.MapShift((object)r);
            return ShiftMapper.ToDto(shift, (string?)r.cashier_name);
        }).Cast<CashierClosingResponse>().ToList();

        return Result<PagedResult<CashierClosingResponse>>.Success(
            new PagedResult<CashierClosingResponse>(items, query.Page, query.PageSize, total));
    }
}

public class ExportShiftPdfHandler : IRequestHandler<ExportShiftPdfQuery, Result<byte[]>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ITenantProvider _tenant;
    private readonly ICashierShiftReportPdfBuilder _builder;

    public ExportShiftPdfHandler(IApplicationDbContext db, IDapperConnectionFactory dapper, ITenantProvider tenant, ICashierShiftReportPdfBuilder builder)
    {
        _db = db; _dapper = dapper; _tenant = tenant; _builder = builder;
    }

    public async Task<Result<byte[]>> Handle(ExportShiftPdfQuery query, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        var shift = await _db.CashierShifts
            .FirstOrDefaultAsync(s => s.Id == query.Id && s.TenantId == tenantId, ct);
        if (shift == null) return Result<byte[]>.Failure("SHIFT_NOT_FOUND", "Khong tim thay ca thu ngan");

        string? cashierName = null;
        Reports.LetterheadDto? lh;
        using (var conn = _dapper.CreateConnection())
        {
            cashierName = await conn.QueryFirstOrDefaultAsync<string?>(
                "SELECT full_name FROM diab_his_sec_users WHERE id = @id",
                new { id = shift.CashierUserId.ToString() });

            lh = await conn.QueryFirstOrDefaultAsync<Reports.LetterheadDto>(
                @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                         phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                         slogan AS Slogan, website AS Website
                  FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });
        }
        lh ??= new Reports.LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var data = new CashierShiftReportData(
            lh, shift.Id, shift.ShiftDate, cashierName, shift.ShiftStart, shift.ShiftEnd, shift.Status,
            shift.OpeningBalance, shift.ClosingBalance ?? 0,
            shift.TotalCash, shift.TotalCard, shift.TotalTransfer, shift.TotalQr, shift.TotalOther,
            shift.TotalRefund, shift.TotalVoid, shift.CountTransactions,
            shift.ExpectedCash, shift.ActualCash, shift.Difference, shift.Note);

        var pdf = _builder.Build(data);
        return Result<byte[]>.Success(pdf);
    }
}

public class GetDebtsHandler : IRequestHandler<GetDebtsQuery, Result<(List<DebtResponse> Items, int Total, decimal TotalDebt)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetDebtsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<(List<DebtResponse> Items, int Total, decimal TotalDebt)>> Handle(
        GetDebtsQuery query, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var having = "HAVING SUM(b.balance) >= @minBalance";
        var where = "WHERE b.tenant_id = @tenantId AND b.status IN ('FINALIZED','PARTIAL_PAID')";
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        p.Add("minBalance", query.MinBalance);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where += " AND (p.full_name LIKE @q OR p.code LIKE @q OR p.phone LIKE @q)";
            p.Add("q", $"%{query.Q}%");
        }
        if (query.OlderThanDays.HasValue)
        {
            where += " AND b.created_at <= DATE_SUB(NOW(), INTERVAL @days DAY)";
            p.Add("days", query.OlderThanDays.Value);
        }

        var baseSql = $@"
            FROM diab_his_bil_billing b
            JOIN pat_patients p ON p.id = b.patient_id
            {where}
            GROUP BY b.patient_id, p.code, p.full_name, p.phone
            {having}";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM (SELECT b.patient_id {baseSql}) t", p);
        var totalDebt = await conn.ExecuteScalarAsync<decimal?>($"SELECT SUM(t.balance) FROM (SELECT SUM(b.balance) AS balance {baseSql}) t", p) ?? 0;

        var offset = (query.Page - 1) * query.PageSize;
        p.Add("limit", query.PageSize); p.Add("offset", offset);

        var rows = await conn.QueryAsync<dynamic>($@"
            SELECT
                b.patient_id,
                p.code as patient_code,
                p.full_name as patient_name,
                p.phone,
                SUM(b.subtotal + b.vat_total - b.discount_amount) as total_billed,
                SUM(b.paid_amount) as total_paid,
                SUM(b.balance) as balance,
                COUNT(*) as unpaid_bills_count,
                MAX(b.updated_at) as last_payment_at,
                MIN(b.created_at) as oldest_unpaid_at
            {baseSql}
            ORDER BY balance DESC
            LIMIT @limit OFFSET @offset", p);

        var items = rows.Select(r =>
        {
            var oldest = r.oldest_unpaid_at == null ? (DateTime?)null : (DateTime)r.oldest_unpaid_at;
            int? days = oldest.HasValue ? (int)(DateTime.UtcNow - oldest.Value).TotalDays : null;
            return new DebtResponse(
                Guid.Parse((string)r.patient_id),
                (string?)r.patient_code,
                (string)r.patient_name,
                (string?)r.phone,
                (decimal)r.total_billed,
                (decimal)r.total_paid,
                (decimal)r.balance,
                (int)r.unpaid_bills_count,
                r.last_payment_at == null ? null : (DateTime?)r.last_payment_at,
                oldest,
                days);
        }).ToList();

        return Result<(List<DebtResponse>, int, decimal)>.Success((items, totalCount, totalDebt));
    }
}

public record GetCurrentShiftQuery() : IRequest<Result<CurrentShiftResponse>>;

public record CurrentShiftResponse(
    Guid? ShiftId,
    bool IsOpen,
    DateTime? OpenedAt,
    decimal ExpectedCash,
    decimal OpeningBalance,
    string Status);

public class GetCurrentShiftHandler : IRequestHandler<GetCurrentShiftQuery, Result<CurrentShiftResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public GetCurrentShiftHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<CurrentShiftResponse>> Handle(GetCurrentShiftQuery request, CancellationToken ct)
    {
        if (!_user.UserId.HasValue)
            return Result<CurrentShiftResponse>.Failure("AUTH_REQUIRED", "Can dang nhap");

        var userId = _user.UserId.Value;
        var tenantId = _tenant.TenantId;

        var shift = await _db.CashierShifts
            .AsNoTracking()
            .Where(s => s.CashierUserId == userId && s.TenantId == tenantId && s.Status == "OPEN")
            .OrderByDescending(s => s.ShiftStart)
            .FirstOrDefaultAsync(ct);

        if (shift == null)
            return Result<CurrentShiftResponse>.Success(
                new CurrentShiftResponse(null, false, null, 0, 0, "NONE"));

        var expected = shift.ExpectedCash ?? (shift.OpeningBalance + shift.TotalCash + shift.TotalCard + shift.TotalTransfer + shift.TotalQr + shift.TotalOther - shift.TotalRefund - shift.TotalVoid);

        return Result<CurrentShiftResponse>.Success(
            new CurrentShiftResponse(shift.Id, true, shift.ShiftStart, expected, shift.OpeningBalance, "OPEN"));
    }
}
