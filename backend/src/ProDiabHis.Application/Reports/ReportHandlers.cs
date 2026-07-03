using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reports;

// ---- Revenue ---- //
public class GetRevenueReportHandler : IRequestHandler<GetRevenueReportQuery, RevenueReportResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetRevenueReportHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<RevenueReportResponse> Handle(GetRevenueReportQuery request, CancellationToken ct)
        => _svc.GetRevenueReportAsync(_tenant.TenantId, request.Period, request.From, request.To, request.ClinicId, ct);
}

// ---- Doctor KPI ---- //
public class GetDoctorKpiHandler : IRequestHandler<GetDoctorKpiQuery, IReadOnlyList<DoctorKpiResponse>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetDoctorKpiHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<DoctorKpiResponse>> Handle(GetDoctorKpiQuery request, CancellationToken ct)
        => _svc.GetDoctorKpiAsync(_tenant.TenantId, request.From, request.To, request.Top, ct);
}

// ---- Top Drugs ---- //
public class GetTopDrugsHandler : IRequestHandler<GetTopDrugsQuery, IReadOnlyList<TopDrugResponse>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetTopDrugsHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<TopDrugResponse>> Handle(GetTopDrugsQuery request, CancellationToken ct)
        => _svc.GetTopDrugsAsync(_tenant.TenantId, request.From, request.To, request.Top, ct);
}

// ---- Diabetes Cohort ---- //
public class GetDiabetesCohortHandler : IRequestHandler<GetDiabetesCohortQuery, DiabetesCohortResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetDiabetesCohortHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<DiabetesCohortResponse> Handle(GetDiabetesCohortQuery request, CancellationToken ct)
        => _svc.GetDiabetesCohortAsync(_tenant.TenantId, request.From, request.To, ct);
}

// ---- Dashboard Overview ---- //
public class GetDashboardOverviewHandler : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetDashboardOverviewHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<DashboardOverviewResponse> Handle(GetDashboardOverviewQuery request, CancellationToken ct)
        => _svc.GetDashboardOverviewAsync(_tenant.TenantId, ct);
}

// ---- Revenue Trend ---- //
public class GetRevenueTrendHandler : IRequestHandler<GetRevenueTrendChartQuery, IReadOnlyList<ChartDataPoint>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetRevenueTrendHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<ChartDataPoint>> Handle(GetRevenueTrendChartQuery request, CancellationToken ct)
    {
        var days = request.Range switch { "7d" => 7, "90d" => 90, _ => 30 };
        return _svc.GetRevenueTrendAsync(_tenant.TenantId, days, ct);
    }
}

// ---- Encounters Trend ---- //
public class GetEncountersTrendHandler : IRequestHandler<GetEncountersTrendChartQuery, IReadOnlyList<ChartDataPoint>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetEncountersTrendHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<ChartDataPoint>> Handle(GetEncountersTrendChartQuery request, CancellationToken ct)
    {
        var days = request.Range switch { "7d" => 7, "90d" => 90, _ => 30 };
        return _svc.GetEncountersTrendAsync(_tenant.TenantId, days, ct);
    }
}

// ---- Alerts ---- //
public class GetAlertsHandler : IRequestHandler<GetAlertsQuery, IReadOnlyList<AlertResponse>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetAlertsHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<AlertResponse>> Handle(GetAlertsQuery request, CancellationToken ct)
        => _svc.GetAlertsAsync(_tenant.TenantId, request.Severity, request.Type, ct);
}

// ---- GetReportPdf ---- //

public class GetReportPdfHandler : IRequestHandler<GetReportPdfQuery, (byte[] Bytes, string FileName, string ReportCode)>
{
    private readonly IPdfReportExporter _exporter;
    private readonly IReportCodeGenerator _codeGen;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public GetReportPdfHandler(
        IPdfReportExporter exporter,
        IReportCodeGenerator codeGen,
        IDapperConnectionFactory db,
        ITenantProvider tenant,
        ICurrentUser currentUser,
        IAuditService audit)
    {
        _exporter    = exporter;
        _codeGen     = codeGen;
        _db          = db;
        _tenant      = tenant;
        _currentUser = currentUser;
        _audit       = audit;
    }

    public async Task<(byte[] Bytes, string FileName, string ReportCode)> Handle(
        GetReportPdfQuery request, CancellationToken ct)
    {
        // Validate date range
        if (request.From > request.To)
            throw new ReportValidationException(
                "REPORT_INVALID_DATE_RANGE",
                "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc");

        if (request.To.DayNumber - request.From.DayNumber > 365)
            throw new ReportValidationException(
                "REPORT_INVALID_DATE_RANGE",
                "Khoảng thời gian báo cáo không được vượt quá 365 ngày");

        int tenantId  = _tenant.TenantId;
        Guid? userId  = _currentUser.UserId;

        using var conn = _db.CreateConnection();

        // Kiểm tra clinic (tenantId) hợp lệ — ClinicId nếu có phải thuộc tenant hiện tại
        if (request.ClinicId.HasValue && request.ClinicId.Value != tenantId)
        {
            var clinicExists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM diab_his_sys_tenants WHERE id = @id AND deleted_at IS NULL",
                new { id = request.ClinicId.Value }) > 0;
            if (!clinicExists)
                throw new ReportValidationException("REPORT_CLINIC_NOT_FOUND", "Phòng khám không tồn tại hoặc không thuộc tenant hiện tại");
        }

        // Lay letterhead tu diab_his_sys_tenants WHERE id=@tenantId
        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });

        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        // Dung ma da dat truoc (neu co) hoac sinh moi qua Redis
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reportCode = string.IsNullOrWhiteSpace(request.ReportCode)
            ? await _codeGen.NextAsync(tenantId, request.ReportType, today, ct)
            : request.ReportCode;

        // Resolve tên người xuất báo cáo qua bảng diab_his_sec_users (id là char(36) = Guid)
        string? exportedByFullName = null;
        if (userId.HasValue)
        {
            exportedByFullName = await conn.ExecuteScalarAsync<string?>(
                "SELECT full_name FROM diab_his_sec_users WHERE id = @id AND deleted_at IS NULL LIMIT 1",
                new { id = userId.Value.ToString() });
        }

        var pdfReq = new ReportPdfRequest(
            TenantId: tenantId,
            ReportType: request.ReportType,
            FromDate: request.From,
            ToDate: request.To,
            ClinicId: request.ClinicId,
            ExportedByUserId: userId,
            ReportCode: reportCode,
            ExportedByFullName: exportedByFullName);

        byte[] bytes;
        string typeCode;

        var fromDt = request.From.ToDateTime(TimeOnly.MinValue);
        var toDt   = request.To.ToDateTime(TimeOnly.MaxValue);

        switch (request.ReportType)
        {
            case ReportType.Financial:
            {
                typeCode = "FIN";
                // Query hóa đơn đã thanh toán trong khoảng thời gian, kèm dòng đầu tiên của items
                // Chi tinh hoa don da chot (FINALIZED) — dong nhat dinh nghia doanh thu voi
                // revenue-trend + revenue/by-service. Kem so dong dich vu de gan nhan da-dich-vu.
                var rawRows = await conn.QueryAsync<(string BillNo, string PatientId, string ItemName, int ItemCount, decimal Amount)>(
                    @"SELECT b.bill_no, b.patient_id,
                             COALESCE(i.name, 'Dịch vụ khám') AS item_name,
                             (SELECT COUNT(*) FROM diab_his_bil_billing_items ic WHERE ic.billing_id = b.id) AS item_count,
                             b.patient_payable
                      FROM diab_his_bil_billing b
                      LEFT JOIN diab_his_bil_billing_items i ON i.billing_id = b.id
                          AND i.id = (SELECT MIN(ii.id) FROM diab_his_bil_billing_items ii WHERE ii.billing_id = b.id)
                      WHERE b.tenant_id = @tenantId
                        AND b.deleted_at IS NULL
                        AND b.status = 'FINALIZED'
                        AND b.created_at >= @from AND b.created_at <= @to
                      ORDER BY b.created_at
                      LIMIT 500",
                    new { tenantId, from = fromDt, to = toDt });

                // Lấy tên bệnh nhân theo batch
                var patientIds = rawRows.Select(r => r.PatientId.ToString()).Distinct().ToList();
                var patientNames = new Dictionary<string, string>();
                if (patientIds.Any())
                {
                    var names = await conn.QueryAsync<(string Id, string FullName)>(
                        "SELECT id, full_name FROM diab_his_pat_patients WHERE id IN @ids AND tenant_id = @tenantId AND deleted_at IS NULL",
                        new { ids = patientIds, tenantId });
                    foreach (var (id, fn) in names) patientNames[id] = fn;
                }

                var rows = rawRows.Select((r, idx) => new FinancialRowDto(
                    idx + 1,
                    r.BillNo ?? $"HD-{idx + 1:D4}",
                    patientNames.TryGetValue(r.PatientId.ToString(), out var pn) ? pn : "—",
                    // Hoa don nhieu dich vu: hien ten dong dau + so dich vu con lai de tranh hieu nham
                    r.ItemCount > 1 ? $"{r.ItemName} (+{r.ItemCount - 1} DV)" : r.ItemName,
                    r.Amount)).ToList();

                bytes = await _exporter.ExportFinancialAsync(pdfReq, lh, rows, ct);
                break;
            }
            case ReportType.Clinical:
            {
                typeCode = "CLN";
                // Query lượt khám kèm chẩn đoán PRIMARY trong khoảng thời gian
                var rawRows = await conn.QueryAsync<(string EncounterId, string PatientId, string? DoctorId, string? Icd10Code, DateTime? StartedAt)>(
                    @"SELECT e.id AS encounter_id, e.patient_id, e.doctor_id,
                             d.icd10_code, e.started_at
                      FROM diab_his_enc_encounters e
                      LEFT JOIN diab_his_enc_diagnoses d ON d.encounter_id = e.id
                          AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
                          AND d.id = (SELECT MIN(dd.id) FROM diab_his_enc_diagnoses dd
                                      WHERE dd.encounter_id = e.id AND dd.type = 'PRIMARY' AND dd.deleted_at IS NULL)
                      WHERE e.tenant_id = @tenantId
                        AND e.deleted_at IS NULL
                        AND e.started_at >= @from AND e.started_at <= @to
                      ORDER BY e.started_at
                      LIMIT 500",
                    new { tenantId, from = fromDt, to = toDt });

                // Lấy tên bệnh nhân và bác sĩ
                var pIds = rawRows.Select(r => r.PatientId).Distinct().ToList();
                var dIds = rawRows.Where(r => r.DoctorId != null).Select(r => r.DoctorId!).Distinct().ToList();
                var allUserIds = dIds.ToList();

                var patientNames2 = new Dictionary<string, string>();
                if (pIds.Any())
                {
                    var names = await conn.QueryAsync<(string Id, string FullName)>(
                        "SELECT id, full_name FROM diab_his_pat_patients WHERE id IN @ids AND tenant_id = @tenantId AND deleted_at IS NULL",
                        new { ids = pIds, tenantId });
                    foreach (var (id, fn) in names) patientNames2[id] = fn;
                }

                var doctorNames = new Dictionary<string, string>();
                if (allUserIds.Any())
                {
                    var names = await conn.QueryAsync<(string Id, string FullName)>(
                        "SELECT id, full_name FROM diab_his_sec_users WHERE id IN @ids AND tenant_id = @tenantId AND deleted_at IS NULL",
                        new { ids = allUserIds, tenantId });
                    foreach (var (id, fn) in names) doctorNames[id] = fn;
                }

                var rows = rawRows.Select((r, idx) => new ClinicalRowDto(
                    idx + 1,
                    patientNames2.TryGetValue(r.PatientId, out var pn2) ? pn2 : "—",
                    r.DoctorId != null && doctorNames.TryGetValue(r.DoctorId, out var dn) ? $"BS. {dn}" : "—",
                    r.Icd10Code ?? "—",
                    r.StartedAt.HasValue ? DateOnly.FromDateTime(r.StartedAt.Value) : request.From)).ToList();

                bytes = await _exporter.ExportClinicalAsync(pdfReq, lh, rows, ct);
                break;
            }
            case ReportType.Pharmacy:
            {
                typeCode = "PHA";
                // Query tồn kho theo từng lô thuốc còn hạn
                var rawPharmacy = await conn.QueryAsync<dynamic>(
                    @"SELECT d.code AS DrugCode, d.name AS DrugName,
                             s.lot_number AS LotNumber, s.exp_date AS ExpiryDate,
                             s.quantity AS StockQuantity, d.unit AS Unit
                      FROM diab_his_pha_stock s
                      INNER JOIN diab_his_pha_drugs d ON d.id = s.drug_id
                      WHERE d.tenant_id = @tenantId
                        AND d.deleted_at IS NULL AND d.is_active = 1
                        AND s.quantity > 0
                        AND (s.exp_date IS NULL OR s.exp_date >= CURDATE())
                      ORDER BY d.name, s.exp_date
                      LIMIT 500",
                    new { tenantId });
                var rows = rawPharmacy.Select(r => new PharmacyRowDto(
                    (string?)r.DrugCode ?? "",
                    (string?)r.DrugName ?? "",
                    (string?)r.LotNumber,
                    r.ExpiryDate == null ? null : DateOnly.FromDateTime((DateTime)r.ExpiryDate),
                    (decimal)(r.StockQuantity ?? 0),
                    (string?)r.Unit ?? "")).ToList();

                bytes = await _exporter.ExportPharmacyAsync(pdfReq, lh, rows, ct);
                break;
            }
            default:
                throw new ReportValidationException("REPORT_INVALID_TYPE", "Loại báo cáo không hợp lệ");
        }

        var fileName = $"RPT-{typeCode}-{today:yyyyMMdd}.pdf";
        return (bytes, fileName, reportCode);
    }
}

// ---- Export ---- //
public class ExportReportHandler : IRequestHandler<ExportReportCommand, (byte[] Content, string ContentType, string FileName)>
{
    private readonly IReportingService _svc;
    private readonly IPdfReportExporter _pdf;
    private readonly IExcelExporter _excel;
    private readonly ITenantProvider _tenant;

    public ExportReportHandler(IReportingService svc, IPdfReportExporter pdf, IExcelExporter excel, ITenantProvider tenant)
    {
        _svc = svc;
        _pdf = pdf;
        _excel = excel;
        _tenant = tenant;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> Handle(ExportReportCommand command, CancellationToken ct)
    {
        var req = command.Request;
        int tenantId = _tenant.TenantId;

        if (req.ReportType == "doctor_kpi")
        {
            var rows = await _svc.GetDoctorKpiAsync(tenantId, req.From, req.To, 100, ct);
            if (req.Format == ExportFormat.Pdf)
            {
                var bytes = await _pdf.ExportDoctorKpiAsync(rows, req.From, req.To, ct);
                return (bytes, "application/pdf", $"bao-cao-bac-si-{req.From:yyyy-MM-dd}.pdf");
            }
            else
            {
                var bytes = _excel.Export(rows, "Bác sĩ KPI");
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"bao-cao-bac-si-{req.From:yyyy-MM-dd}.xlsx");
            }
        }
        else
        {
            // Default: revenue
            var report = await _svc.GetRevenueReportAsync(tenantId, "MONTH", req.From, req.To, req.ClinicId, ct);
            if (req.Format == ExportFormat.Pdf)
            {
                var bytes = await _pdf.ExportRevenueAsync(report, ct);
                return (bytes, "application/pdf", $"bao-cao-doanh-thu-{req.From:yyyy-MM-dd}.pdf");
            }
            else
            {
                var bytes = _excel.Export(report.Series, "Doanh thu");
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"bao-cao-doanh-thu-{req.From:yyyy-MM-dd}.xlsx");
            }
        }
    }
}

// ---- Encounters Count ---- //
public class GetEncountersCountHandler : IRequestHandler<GetEncountersCountQuery, IReadOnlyList<EncounterCountItem>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetEncountersCountHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<EncounterCountItem>> Handle(GetEncountersCountQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var groupExpr = q.Period.ToUpperInvariant() switch
        {
            "WEEK"  => "DATE_FORMAT(created_at, '%Y-%u')",
            "MONTH" => "DATE_FORMAT(created_at, '%Y-%m')",
            _       => "DATE_FORMAT(created_at, '%Y-%m-%d')"
        };

        var sql = $@"
            SELECT {groupExpr} AS period_label, COUNT(*) AS cnt
            FROM diab_his_enc_encounters
            WHERE tenant_id = @tenantId
              AND created_at >= @from
              AND created_at <= @to
            GROUP BY {groupExpr}
            ORDER BY {groupExpr}";

        var rows = await conn.QueryAsync<dynamic>(sql, new { tenantId, from = q.From.ToDateTime(TimeOnly.MinValue), to = q.To.ToDateTime(TimeOnly.MaxValue) });
        return rows.Select(r => new EncounterCountItem((string)r.period_label, (int)r.cnt)).ToList();
    }
}

// ---- Top Diagnoses ---- //
public class GetTopDiagnosesHandler : IRequestHandler<GetTopDiagnosesQuery, IReadOnlyList<TopDiagnosisResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetTopDiagnosesHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TopDiagnosisResponse>> Handle(GetTopDiagnosesQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        var sql = @"
            SELECT e.primary_icd10 AS icd10_code, i.name_vi AS icd10_name, COUNT(*) AS cnt
            FROM diab_his_enc_encounters e
            LEFT JOIN diab_his_ref_icd10 i ON i.code = e.primary_icd10
            WHERE e.tenant_id = @tenantId
              AND e.created_at >= @from
              AND e.created_at <= @to
              AND e.primary_icd10 IS NOT NULL AND e.primary_icd10 <> ''
            GROUP BY e.primary_icd10, i.name_vi
            ORDER BY cnt DESC
            LIMIT @top";

        var rows = await conn.QueryAsync<dynamic>(sql, new { tenantId, from = q.From.ToDateTime(TimeOnly.MinValue), to = q.To.ToDateTime(TimeOnly.MaxValue), top = q.Top });
        var list = rows.ToList();
        var totalCount = list.Sum(r => (int)r.cnt);
        return list.Select(r =>
        {
            var cnt = (int)r.cnt;
            var pct = totalCount > 0 ? Math.Round((decimal)cnt / totalCount * 100, 1) : 0m;
            return new TopDiagnosisResponse((string)r.icd10_code, (string?)r.icd10_name, cnt, pct);
        }).ToList();
    }
}

// ================= F7: Report endpoint con stub — bo sung handler that ================= //

// ---- Revenue by service ---- //
public class GetRevenueByServiceHandler : IRequestHandler<GetRevenueByServiceQuery, IReadOnlyList<ServiceRevenueResponse>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetRevenueByServiceHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<ServiceRevenueResponse>> Handle(GetRevenueByServiceQuery request, CancellationToken ct)
        => _svc.GetRevenueByServiceAsync(_tenant.TenantId, request.From, request.To, request.Top, ct);
}

// ---- Revenue by payment method ---- //
public class GetRevenueByPaymentMethodHandler : IRequestHandler<GetRevenueByPaymentMethodQuery, IReadOnlyList<PaymentMethodBreakdownResponse>>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetRevenueByPaymentMethodHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<PaymentMethodBreakdownResponse>> Handle(GetRevenueByPaymentMethodQuery request, CancellationToken ct)
        => _svc.GetRevenueByPaymentMethodAsync(_tenant.TenantId, request.From, request.To, ct);
}

// ---- Cashier daily summary ---- //
public class GetCashierDailySummaryHandler : IRequestHandler<GetCashierDailySummaryQuery, CashierDailySummaryResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetCashierDailySummaryHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<CashierDailySummaryResponse> Handle(GetCashierDailySummaryQuery request, CancellationToken ct)
        => _svc.GetCashierDailySummaryAsync(_tenant.TenantId, request.Date, request.CashierId, ct);
}

// ---- Debts aging ---- //
public class GetDebtsAgingHandler : IRequestHandler<GetDebtsAgingQuery, DebtsAgingResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetDebtsAgingHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<DebtsAgingResponse> Handle(GetDebtsAgingQuery request, CancellationToken ct)
        => _svc.GetDebtsAgingAsync(_tenant.TenantId, request.AsOf, ct);
}

// ---- BHYT summary ---- //
public class GetBhytSummaryHandler : IRequestHandler<GetBhytSummaryQuery, BhytSummaryResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetBhytSummaryHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<BhytSummaryResponse> Handle(GetBhytSummaryQuery request, CancellationToken ct)
        => _svc.GetBhytSummaryAsync(_tenant.TenantId, request.From, request.To, ct);
}

// ---- Pharmacy inventory value ---- //
public class GetInventoryValueHandler : IRequestHandler<GetInventoryValueQuery, InventoryValueResponse>
{
    private readonly IReportingService _svc;
    private readonly ITenantProvider _tenant;

    public GetInventoryValueHandler(IReportingService svc, ITenantProvider tenant)
    {
        _svc = svc;
        _tenant = tenant;
    }

    public Task<InventoryValueResponse> Handle(GetInventoryValueQuery request, CancellationToken ct)
        => _svc.GetInventoryValueAsync(_tenant.TenantId, ct);
}

// ---- Clinical visits (paged) ---- //
public class GetClinicalVisitsHandler : IRequestHandler<GetClinicalVisitsQuery, PagedResult<ClinicalVisitItem>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetClinicalVisitsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PagedResult<ClinicalVisitItem>> Handle(GetClinicalVisitsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var fromDt = q.From.ToDateTime(TimeOnly.MinValue);
        var toDt   = q.To.ToDateTime(TimeOnly.MaxValue);
        var offset = (q.Page - 1) * q.PageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_enc_encounters e
              WHERE e.tenant_id = @tenantId AND e.deleted_at IS NULL
                AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to",
            new { tenantId, from = fromDt, to = toDt });

        var sql = @"
            SELECT
                e.id             AS encounter_id,
                e.patient_id     AS patient_id,
                e.doctor_id      AS doctor_id,
                e.status         AS status,
                e.primary_icd10  AS icd10_code,
                i.name_vi        AS icd10_name,
                e.started_at     AS started_at
            FROM diab_his_enc_encounters e
            LEFT JOIN diab_his_ref_icd10 i ON i.code = e.primary_icd10
            WHERE e.tenant_id = @tenantId AND e.deleted_at IS NULL
              AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
            ORDER BY COALESCE(e.started_at, e.created_at) DESC
            LIMIT @limit OFFSET @offset";

        var rows = (await conn.QueryAsync<dynamic>(sql, new { tenantId, from = fromDt, to = toDt, limit = q.PageSize, offset })).ToList();

        var patientIds = rows.Select(r => (string)r.patient_id).Distinct().ToList();
        var doctorIds = rows.Where(r => r.doctor_id != null).Select(r => (string)r.doctor_id).Distinct().ToList();

        var patientNames = new Dictionary<string, string>();
        if (patientIds.Count > 0)
        {
            var names = await conn.QueryAsync<(string Id, string FullName)>(
                "SELECT id, full_name FROM diab_his_pat_patients WHERE tenant_id = @tenantId AND id IN @ids AND deleted_at IS NULL",
                new { tenantId, ids = patientIds });
            foreach (var (id, fn) in names) patientNames[id] = fn;
        }

        var doctorNames = new Dictionary<string, string>();
        if (doctorIds.Count > 0)
        {
            var names = await conn.QueryAsync<(string Id, string FullName)>(
                "SELECT id, full_name FROM diab_his_sec_users WHERE tenant_id = @tenantId AND id IN @ids AND deleted_at IS NULL",
                new { tenantId, ids = doctorIds });
            foreach (var (id, fn) in names) doctorNames[id] = fn;
        }

        var items = rows.Select(r =>
        {
            string patientId = (string)r.patient_id;
            string? doctorId = (string?)r.doctor_id;
            return new ClinicalVisitItem(
                Guid.Parse((string)r.encounter_id),
                patientNames.TryGetValue(patientId, out var pn) ? pn : "—",
                doctorId != null && doctorNames.TryGetValue(doctorId, out var dn) ? $"BS. {dn}" : null,
                (string)r.status,
                (string?)r.icd10_code,
                (string?)r.icd10_name,
                r.started_at == null ? null : (DateTime?)r.started_at);
        }).ToList();

        return new PagedResult<ClinicalVisitItem>(items, q.Page, q.PageSize, total);
    }
}

// ---- Clinical ICD-10 breakdown (paged) ---- //
public class GetClinicalIcd10Handler : IRequestHandler<GetClinicalIcd10Query, PagedResult<TopDiagnosisResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetClinicalIcd10Handler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PagedResult<TopDiagnosisResponse>> Handle(GetClinicalIcd10Query q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var fromDt = q.From.ToDateTime(TimeOnly.MinValue);
        var toDt   = q.To.ToDateTime(TimeOnly.MaxValue);
        var offset = (q.Page - 1) * q.PageSize;

        var fromClause = @"
            FROM diab_his_enc_encounters e
            LEFT JOIN diab_his_ref_icd10 i ON i.code = e.primary_icd10
            WHERE e.tenant_id = @tenantId
              AND e.deleted_at IS NULL
              AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
              AND e.primary_icd10 IS NOT NULL AND e.primary_icd10 <> ''";

        var totalGroups = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM (SELECT e.primary_icd10 {fromClause} GROUP BY e.primary_icd10) t",
            new { tenantId, from = fromDt, to = toDt });

        var totalDiagnosedEncounters = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) {fromClause}",
            new { tenantId, from = fromDt, to = toDt });

        var dataSql = $@"
            SELECT e.primary_icd10 AS icd10_code, i.name_vi AS icd10_name, COUNT(*) AS cnt
            {fromClause}
            GROUP BY e.primary_icd10, i.name_vi
            ORDER BY cnt DESC
            LIMIT @limit OFFSET @offset";

        var rows = await conn.QueryAsync<dynamic>(dataSql, new { tenantId, from = fromDt, to = toDt, limit = q.PageSize, offset });

        var items = rows.Select(r =>
        {
            var cnt = (int)r.cnt;
            var pct = totalDiagnosedEncounters > 0 ? Math.Round((decimal)cnt / totalDiagnosedEncounters * 100, 1) : 0m;
            return new TopDiagnosisResponse((string)r.icd10_code, (string?)r.icd10_name, cnt, pct);
        }).ToList();

        return new PagedResult<TopDiagnosisResponse>(items, q.Page, q.PageSize, totalGroups);
    }
}
