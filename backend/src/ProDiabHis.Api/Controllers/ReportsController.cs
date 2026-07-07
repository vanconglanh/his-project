using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Bao cao tai chinh, lam sang, duoc pham — Sprint 11 EPIC 9.
/// Route: /api/v1/reports/*
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportCodeGenerator _codeGen;
    private readonly ITenantProvider _tenant;

    public ReportsController(IMediator mediator, IReportCodeGenerator codeGen, ITenantProvider tenant)
    {
        _mediator = mediator;
        _codeGen  = codeGen;
        _tenant   = tenant;
    }

    // ======== FINANCIAL ======== //

    /// <summary>Bao cao doanh thu theo ky</summary>
    [HttpGet("revenue")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] string period,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? clinic_id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRevenueReportQuery(period, from, to, clinic_id), ct);
        return Ok(new { data = result });
    }

    /// <summary>KPI bac si — doanh thu, luot kham, don thuoc</summary>
    [HttpGet("revenue/by-doctor")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetRevenueByDoctor(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDoctorKpiQuery(from, to, top), ct);
        return Ok(new { data = result });
    }

    /// <summary>Top dich vu theo doanh thu</summary>
    [HttpGet("revenue/by-service")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetRevenueByService(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRevenueByServiceQuery(from, to, top), ct);
        return Ok(new { data = result });
    }

    /// <summary>Doanh thu theo phuong thuc thanh toan</summary>
    [HttpGet("revenue/by-payment-method")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetRevenueByPaymentMethod(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRevenueByPaymentMethodQuery(from, to), ct);
        return Ok(new { data = result });
    }

    /// <summary>Tong ket thu ngan cuoi ngay</summary>
    [HttpGet("cashier/daily-summary")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetCashierDailySummary(
        [FromQuery] DateOnly date,
        [FromQuery] Guid? cashier_id,
        CancellationToken ct = default)
    {
        var r = await _mediator.Send(new GetCashierDailySummaryQuery(date, cashier_id), ct);
        return Ok(new
        {
            data = new
            {
                date = r.Date.ToString("yyyy-MM-dd"),
                total_revenue = r.TotalRevenue,
                total_invoices = r.TotalInvoices,
                total_refunds = r.TotalRefunds,
                by_payment_method = r.ByPaymentMethod,
                opening_balance = r.OpeningBalance,
                closing_balance = r.ClosingBalance
            }
        });
    }

    /// <summary>Cong no qua han</summary>
    [HttpGet("debts/aging")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDebtsAging([FromQuery] DateOnly? as_of, CancellationToken ct = default)
    {
        var asOf = as_of ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await _mediator.Send(new GetDebtsAgingQuery(asOf), ct);
        return Ok(new { data = result });
    }

    /// <summary>Tom tat BHYT</summary>
    [HttpGet("bhyt/summary")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetBhytSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBhytSummaryQuery(from, to), ct);
        return Ok(new { data = result });
    }

    // ======== CLINICAL ======== //

    /// <summary>Phan tich cohort dai thao duong / HbA1c</summary>
    [HttpGet("clinical/diabetes-cohort")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDiabetesCohort(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDiabetesCohortQuery(from, to), ct);
        return Ok(new { data = result });
    }

    /// <summary>Cohort DTD chi tiet — theo loai DM, phan bo HbA1c, bien chung (snake_case khop FE)</summary>
    [HttpGet("diabetes/cohort")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDiabetesCohortDetailed(
        [FromServices] IReportingService svc,
        [FromServices] ProDiabHis.Application.Common.ITenantProvider tenant,
        [FromQuery] DateOnly? as_of,
        [FromQuery] string? dm_type,
        CancellationToken ct = default)
    {
        var asOf = as_of ?? DateOnly.FromDateTime(DateTime.Today);
        var r = await svc.GetDiabetesCohortDetailedAsync(tenant.TenantId, asOf, dm_type, ct);

        return Ok(new
        {
            data = new
            {
                as_of = r.AsOf.ToString("yyyy-MM-dd"),
                total_patients = r.TotalPatients,
                by_type = new { t1 = r.ByType.T1, t2 = r.ByType.T2, gdm = r.ByType.Gdm },
                hba1c_distribution = new
                {
                    lt_7        = r.Hba1cDistribution.Lt7,
                    between_7_8 = r.Hba1cDistribution.Between7And8,
                    between_8_9 = r.Hba1cDistribution.Between8And9,
                    gt_9        = r.Hba1cDistribution.Gt9
                },
                complications = new
                {
                    retinopathy = r.Complications.Retinopathy,
                    neuropathy  = r.Complications.Neuropathy,
                    nephropathy = r.Complications.Nephropathy,
                    cad         = r.Complications.Cad,
                    pad         = r.Complications.Pad
                }
            }
        });
    }

    // ======== ENCOUNTERS / DIAGNOSES ======== //

    /// <summary>Dem luot kham theo ky (DAY/WEEK/MONTH)</summary>
    [HttpGet("encounters/count")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetEncountersCount(
        [FromQuery] string period = "DAY",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetEncountersCountQuery(period, from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29)), to ?? DateOnly.FromDateTime(DateTime.Today)),
            ct);
        return Ok(new { data = result });
    }

    /// <summary>Top chan doan ICD-10</summary>
    [HttpGet("diagnoses/top")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetTopDiagnoses(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTopDiagnosesQuery(from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29)), to ?? DateOnly.FromDateTime(DateTime.Today), top),
            ct);
        return Ok(new { data = result });
    }

    /// <summary>Bao cao lam sang - luot kham</summary>
    [HttpGet("clinical/visits")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetClinicalVisits(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var paged = await _mediator.Send(new GetClinicalVisitsQuery(fromDate, toDate, page, Math.Min(page_size, 100)), ct);
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    /// <summary>Bao cao lam sang - phan bo ICD-10</summary>
    [HttpGet("clinical/icd10")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetClinicalIcd10(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int top = 20,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var paged = await _mediator.Send(new GetClinicalIcd10Query(fromDate, toDate, page, Math.Min(top, 100)), ct);
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // ======== PHARMACY ======== //

    /// <summary>Top thuoc su dung nhieu nhat</summary>
    [HttpGet("pharmacy/top-drugs")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetTopDrugs(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTopDrugsQuery(from, to, top), ct);
        return Ok(new { data = result });
    }

    /// <summary>Gia tri ton kho</summary>
    [HttpGet("pharmacy/inventory-value")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetInventoryValue(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetInventoryValueQuery(), ct);
        return Ok(new { data = result });
    }

    // ======== EXPORT ======== //

    // ======== PDF A4 (Financial / Clinical / Pharmacy) ======== //

    /// <summary>Dat truoc ma bao cao (dung chung cho preview HTML va PDF download)</summary>
    /// <remarks>
    /// Goi 1 lan truoc khi render preview. FE luu ma nay, truyen vao GET /{type}/pdf?reportCode=...
    /// de PDF dung dung ma da hien thi trong preview, khong sinh ma moi.
    /// type: financial | clinical | pharmacy
    /// </remarks>
    [HttpPost("{type}/code")]
    [RequirePermission("report.export")]
    public async Task<IActionResult> ReserveReportCode(string type, CancellationToken ct)
    {
        if (!TryParseReportType(type, out var reportType))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REPORT_INVALID_TYPE",
                    message = $"Loại báo cáo '{type}' không hợp lệ. Chấp nhận: financial, clinical, pharmacy"
                }
            });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var code  = await _codeGen.NextAsync(_tenant.TenantId, reportType, today, ct);
        return Ok(new { data = new { reportCode = code } });
    }

    /// <summary>Xuat bao cao PDF kho A4 doc (Financial / Clinical / Pharmacy)</summary>
    /// <remarks>
    /// Tra ve file PDF binary. Neu truyen reportCode (da dat truoc qua POST /{type}/code)
    /// thi dung ma do (khong sinh them), dam bao khop voi ma hien thi o preview HTML.
    /// Neu khong truyen reportCode thi tu dong sinh ma moi qua Redis INCR.
    /// type: financial | clinical | pharmacy
    /// </remarks>
    [HttpGet("{type}/pdf")]
    [RequirePermission("report.export")]
    public async Task<IActionResult> GetPdf(
        string type,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int? clinicId,
        [FromQuery] string? reportCode,
        CancellationToken ct)
    {
        if (!TryParseReportType(type, out var reportType))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REPORT_INVALID_TYPE",
                    message = $"Loại báo cáo '{type}' không hợp lệ. Chấp nhận: financial, clinical, pharmacy"
                }
            });
        }

        var query = new GetReportPdfQuery(reportType, from, to, clinicId, reportCode);
        var (bytes, fileName, code) = await _mediator.Send(query, ct);

        Response.Headers.Append("X-Report-Code", code);
        return File(bytes, "application/pdf", fileName);
    }

    private static bool TryParseReportType(string raw, out Application.Reports.ReportType result)
    {
        result = raw.ToLowerInvariant() switch
        {
            "financial" => Application.Reports.ReportType.Financial,
            "clinical"  => Application.Reports.ReportType.Clinical,
            "pharmacy"  => Application.Reports.ReportType.Pharmacy,
            _           => (Application.Reports.ReportType)(-1)
        };
        return (int)result >= 0;
    }

    /// <summary>Xuat bao cao sang Excel hoac PDF</summary>
    [HttpPost("export")]
    [RequirePermission("report.export")]
    public async Task<IActionResult> ExportReport(
        [FromBody] ExportReportRequest request,
        CancellationToken ct)
    {
        var (content, contentType, fileName) = await _mediator.Send(new ExportReportCommand(request), ct);
        return File(content, contentType, fileName);
    }

    // ======== REPORT ENGINE (config-driven, 23 bao cao — docs/prd/reports-catalog-prd.md) ======== //

    private static readonly string[] ReservedQueryKeys =
        { "from", "to", "page", "page_size", "pageSize", "format" };

    private IReadOnlyDictionary<string, string?> ExtractFilters()
        => Request.Query
            .Where(kv => !ReservedQueryKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Count > 0 ? (string?)kv.Value[0] : null);

    /// <summary>Danh muc bao cao (dung cho FE render menu + man hinh xuat bao cao generic).</summary>
    [HttpGet("catalog")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var descriptors = await _mediator.Send(new GetReportCatalogQuery(), ct);

        var data = descriptors
            .OrderBy(d => d.Group).ThenBy(d => d.GroupOrder)
            .Select(d => new
            {
                code = d.Code,
                title = d.Title,
                group = d.Group.ToString(),
                group_order = d.GroupOrder,
                icon = d.Icon,
                orientation = d.Orientation.ToString(),
                group_by_key = d.GroupByKey,
                filters = d.Filters.Select(f => new
                {
                    key = f.Key,
                    label = f.Label,
                    type = f.Type.ToString(),
                    options_source = f.OptionsSource,
                    required = f.Required
                })
            });

        return Ok(new { data });
    }

    /// <summary>Du lieu luoi cua 1 bao cao (Report Engine) — theo code + khoang ngay + filter rieng.</summary>
    [HttpGet("{code}/data")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetReportData(
        string code,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 100,
        CancellationToken ct = default)
    {
        var filters = ExtractFilters();
        var result = await _mediator.Send(new GetReportDataQuery(code, from, to, filters, page, page_size), ct);

        return Ok(new
        {
            data = new
            {
                columns = result.Columns.Select(c => new
                {
                    key = c.Key,
                    label = c.Label,
                    type = c.Type.ToString(),
                    align = c.Align.ToString(),
                    width = c.Width,
                    is_group_subtotal = c.IsGroupSubtotal
                }),
                groups = result.Groups?.Select(g => new
                {
                    key = g.Key,
                    label = g.Label,
                    count = g.Count,
                    rows = g.Rows,
                    subtotals = g.Subtotals
                }),
                rows = result.Rows,
                totals = result.Totals,
                kpis = result.Kpis.Select(k => new { label = k.Label, tint = k.Tint, value = k.Value, is_money = k.IsMoney })
            },
            meta = new { page, page_size, total = result.TotalRows }
        });
    }

    /// <summary>Xuat 1 bao cao (Report Engine) ra PDF hoac Excel.</summary>
    [HttpGet("{code}/export")]
    [RequirePermission("report.export")]
    public async Task<IActionResult> ExportGenericReport(
        string code,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var filters = ExtractFilters();
        var (bytes, contentType, fileName) = await _mediator.Send(
            new ExportGenericReportQuery(code, from, to, filters, format), ct);
        return File(bytes, contentType, fileName);
    }

    /// <summary>Danh sach tuy chon cho filter dropdown (collectors/counters/doctors/clinics/patients).</summary>
    [HttpGet("options/{source}")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetReportOptions(string source, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReportOptionsQuery(source), ct);
        return Ok(new { data = result.Select(o => new { value = o.Value, label = o.Label }) });
    }
}
