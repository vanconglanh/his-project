using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

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

    /// <summary>Top dich vu theo doanh thu (stub)</summary>
    [HttpGet("revenue/by-service")]
    [RequirePermission("report.read")]
    public IActionResult GetRevenueByService(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int top = 20)
    {
        // Stub — pattern giong by-doctor, extend sau
        return Ok(new { data = Array.Empty<object>() });
    }

    /// <summary>Doanh thu theo phuong thuc thanh toan (stub)</summary>
    [HttpGet("revenue/by-payment-method")]
    [RequirePermission("report.read")]
    public IActionResult GetRevenueByPaymentMethod(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        return Ok(new { data = Array.Empty<object>() });
    }

    /// <summary>Tong ket thu ngan cuoi ngay (stub)</summary>
    [HttpGet("cashier/daily-summary")]
    [RequirePermission("report.read")]
    public IActionResult GetCashierDailySummary(
        [FromQuery] DateOnly date,
        [FromQuery] Guid? cashier_id)
    {
        return Ok(new
        {
            data = new
            {
                date = date.ToString("yyyy-MM-dd"),
                total_revenue = 0m,
                total_invoices = 0,
                total_refunds = 0m,
                by_payment_method = Array.Empty<object>(),
                opening_balance = 0m,
                closing_balance = 0m
            }
        });
    }

    /// <summary>Cong no qua han (stub)</summary>
    [HttpGet("debts/aging")]
    [RequirePermission("report.read")]
    public IActionResult GetDebtsAging([FromQuery] DateOnly? as_of)
    {
        return Ok(new
        {
            data = new
            {
                bucket_0_30 = 0m,
                bucket_30_60 = 0m,
                bucket_60_90 = 0m,
                bucket_over_90 = 0m,
                total = 0m,
                details = Array.Empty<object>()
            }
        });
    }

    /// <summary>Tom tat BHYT (stub)</summary>
    [HttpGet("bhyt/summary")]
    [RequirePermission("report.read")]
    public IActionResult GetBhytSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        return Ok(new
        {
            data = new
            {
                total_cards = 0,
                total_amount_claimed = 0m,
                total_amount_paid = 0m,
                total_amount_rejected = 0m,
                rejection_rate = 0m,
                pending_count = 0
            }
        });
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

    /// <summary>Bao cao lam sang - luot kham (stub)</summary>
    [HttpGet("clinical/visits")]
    [RequirePermission("report.read")]
    public IActionResult GetClinicalVisits(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        return Ok(new { data = Array.Empty<object>(), meta = new { page = 1, page_size = 20, total = 0, total_pages = 0 } });
    }

    /// <summary>Bao cao lam sang - phan bo ICD-10 (stub)</summary>
    [HttpGet("clinical/icd10")]
    [RequirePermission("report.read")]
    public IActionResult GetClinicalIcd10(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int top = 20)
    {
        return Ok(new { data = Array.Empty<object>(), meta = new { page = 1, page_size = top, total = 0, total_pages = 0 } });
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

    /// <summary>Gia tri ton kho (stub)</summary>
    [HttpGet("pharmacy/inventory-value")]
    [RequirePermission("report.read")]
    public IActionResult GetInventoryValue()
    {
        return Ok(new { data = new { total_value = 0m, items = Array.Empty<object>() } });
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
}
