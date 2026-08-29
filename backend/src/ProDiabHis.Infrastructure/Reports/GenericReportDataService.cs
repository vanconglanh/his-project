using System.Data;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Thuc thi ReportDescriptor.BuildQuery voi tenant_id + filters, group theo GroupByKey (neu co),
/// tinh subtotal/grand-total/KPI o tang application (khong lap SQL rieng cho tung bao cao).
/// </summary>
public class GenericReportDataService : IGenericReportDataService
{
    private readonly IReportRegistry _registry;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;
    private readonly IBranchProvider _branch;
    private readonly IPermissionChecker _permissions;

    // P1-04: gia tri thay the khi mask cot PII cho user khong duoc phep xem plaintext.
    private const string PiiMask = "••••••";

    public GenericReportDataService(IReportRegistry registry, IDapperConnectionFactory db,
        ITenantProvider tenant, IAuditService audit, IBranchProvider branch, IPermissionChecker permissions)
    {
        _registry = registry;
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _branch = branch;
        _permissions = permissions;
    }

    public async Task<ReportDataResult> GetDataAsync(
        string reportCode,
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<string, string?> filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var descriptor = _registry.GetByCode(reportCode)
            ?? throw new ReportValidationException("REPORT_NOT_FOUND", $"Không tìm thấy báo cáo '{reportCode}'");

        if (from > to)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

        if (to.DayNumber - from.DayNumber > 366)
            throw new ReportValidationException("REPORT_INVALID_DATE_RANGE", "Khoảng thời gian báo cáo không được vượt quá 366 ngày");

        page = Math.Max(1, page);
        // Tran tren 5000 — khop voi LIMIT trong descriptor.BuildQuery (an toan bo nho khi export toan bo).
        pageSize = pageSize <= 0 ? 100 : Math.Min(pageSize, 5000);

        var ctx = new ReportQueryContext(_tenant.TenantId, from, to, filters, _branch.BranchId, _branch.IgnoreBranchFilter);
        var (sql, parameters) = descriptor.BuildQuery(ctx);

        using var conn = (IDbConnection)_db.CreateConnection();
        var rawRows = await conn.QueryAsync(sql, parameters);

        // P1-04: mac dinh MASK cot PII (*_enc). Chi giai ma plaintext khi descriptor cho phep
        // (AllowPiiPlaintext) VA user co quyen 'report.pii_plaintext' (super admin bypass).
        var revealPii = descriptor.AllowPiiPlaintext && _permissions.HasPermission("report.pii_plaintext");
        var maskedAny = false;

        var rows = rawRows
            .Select(r =>
            {
                var src = (IDictionary<string, object>)r;
                var dict = new Dictionary<string, object?>(src.Count);
                foreach (var kv in src)
                {
                    if (kv.Value is string sv && PiiCrypto.Current?.IsProtected(sv) == true)
                    {
                        if (revealPii)
                        {
                            dict[kv.Key] = PiiCrypto.Unprotect(sv);
                        }
                        else
                        {
                            dict[kv.Key] = PiiMask;
                            maskedAny = true;
                        }
                    }
                    else
                    {
                        // Gia tri khong ma hoa: giu nguyen (Unprotect la pass-through cho chuoi thuong).
                        dict[kv.Key] = kv.Value;
                    }
                }
                return (IDictionary<string, object?>)dict;
            })
            .ToList();

        // Audit: chi ghi khi THUC SU giai ma PII plaintext (co rui ro lo lot). Mask thi khong can.
        if (revealPii && rawRows.Cast<IDictionary<string, object>>()
                   .Any(r => r.Values.Any(v => v is string sv && PiiCrypto.Current?.IsProtected(sv) == true)))
        {
            await _audit.LogAsync("PII_PLAINTEXT_REVEAL", "Report", reportCode,
                AuditSeverity.WARN, false, null,
                new { tenantId = _tenant.TenantId, rowCount = rows.Count }, ct);
        }
        _ = maskedAny;

        var subtotalKeys = descriptor.Columns.Where(c => c.IsGroupSubtotal).Select(c => c.Key).ToList();

        decimal SumColumn(IEnumerable<IDictionary<string, object?>> src, string key)
            => src.Sum(row => ReportValueConverter.ToDecimal(row.TryGetValue(key, out var v) ? v : null));

        List<ReportGroupResult>? groups = null;
        List<IDictionary<string, object?>>? flatRows = null;

        if (!string.IsNullOrWhiteSpace(descriptor.GroupByKey))
        {
            groups = rows
                .GroupBy(r => r.TryGetValue(descriptor.GroupByKey, out var gv) && gv != null
                    ? gv.ToString() ?? "—"
                    : "—")
                .Select(g =>
                {
                    var subtotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(g, k));
                    return new ReportGroupResult(g.Key, g.Key, g.Count(), g.ToList(), subtotals);
                })
                .OrderBy(g => g.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            // Bao cao khong group: phan trang tren tap dong (grand total van tinh tren toan bo tap ket qua).
            flatRows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        var grandTotals = subtotalKeys.ToDictionary(k => k, k => SumColumn(rows, k));

        var kpiResults = descriptor.Kpis
            .Select(k => new ReportKpiResult(k.Label, k.Tint, k.Compute(rows), k.IsMoney))
            .ToList();

        return new ReportDataResult(descriptor.Columns, groups, flatRows, grandTotals, kpiResults, rows.Count);
    }
}
