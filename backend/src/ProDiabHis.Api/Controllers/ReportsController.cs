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
                view_type = d.ViewType.ToString().ToUpperInvariant(),
                chart = d.Chart is null ? null : new { type = d.Chart.Type, dims = d.Chart.Dims, measure = d.Chart.Measure },
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
                kpis = result.Kpis.Select(k => new
                {
                    label = k.Label,
                    tint = k.Tint,
                    tint_token = ReportTintTokens.FromHex(k.Tint),
                    value = k.Value,
                    is_money = k.IsMoney
                })
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

    // ======== REPORT BUILDER P1 (docs/prd/report-builder-prd.md) — tu tao bao cao qua UI, khong code ======== //

    /// <summary>Danh sach Dataset whitelist (nguon du lieu an toan) + truong duoc phep dung cho Report Builder.</summary>
    [HttpGet("datasets")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> GetDatasets(CancellationToken ct)
    {
        var datasets = await _mediator.Send(new GetReportDatasetsQuery(), ct);
        return Ok(new
        {
            data = datasets.Select(d => new
            {
                key = d.Key,
                label = d.Label,
                fields = d.Fields.Select(f => new
                {
                    key = f.Key,
                    label = f.Label,
                    role = f.Role.ToString().ToUpperInvariant(),
                    data_type = f.DataType.ToString(),
                    aggregations = f.AllowedAggregations.Select(ReportAggregationCodes.ToCode)
                })
            })
        });
    }

    /// <summary>Danh sach bao cao tu tao (cua tenant hien tai — TENANT hoac PRIVATE cua chinh minh).</summary>
    [HttpGet("definitions")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> GetDefinitions(CancellationToken ct)
    {
        var definitions = await _mediator.Send(new GetReportDefinitionsQuery(), ct);
        return Ok(new { data = definitions.Select(ToDefinitionResponse) });
    }

    /// <summary>Tao 1 bao cao tu tao moi tren Dataset whitelist.</summary>
    [HttpPost("definitions")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> CreateDefinition([FromBody] SaveReportDefinitionRequest request, CancellationToken ct)
    {
        var input = ReportBuilderRequestMapper.ToInput(request.Title, request.DatasetKey, request.Definition, request.Chart, request.ViewType, request.Visibility, request.SharedRoles);
        var created = await _mediator.Send(new CreateReportDefinitionCommand(input), ct);
        return Ok(new { data = ToDefinitionResponse(created) });
    }

    /// <summary>Sua 1 bao cao tu tao (chi chu so huu hoac admin).</summary>
    [HttpPut("definitions/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> UpdateDefinition(string id, [FromBody] SaveReportDefinitionRequest request, CancellationToken ct)
    {
        var input = ReportBuilderRequestMapper.ToInput(request.Title, request.DatasetKey, request.Definition, request.Chart, request.ViewType, request.Visibility, request.SharedRoles);
        var updated = await _mediator.Send(new UpdateReportDefinitionCommand(id, input), ct);
        return Ok(new { data = ToDefinitionResponse(updated) });
    }

    /// <summary>Xoa (mem) 1 bao cao tu tao (chi chu so huu hoac admin).</summary>
    [HttpDelete("definitions/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> DeleteDefinition(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReportDefinitionCommand(id), ct);
        return Ok(new { data = new { id, deleted = true } });
    }

    /// <summary>Chay thu 1 dinh nghia bao cao chua luu (LIMIT nho) — tra dung shape voi GET /{code}/data.</summary>
    [HttpPost("preview")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> PreviewDefinition([FromBody] PreviewReportDefinitionRequest request, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var input = ReportBuilderRequestMapper.ToInput("Xem trước", request.DatasetKey, request.Definition, request.Chart, "TABLE", "PRIVATE");

        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        var result = await _mediator.Send(new PreviewReportDefinitionQuery(
            request.DatasetKey, input.Columns, input.Filters, input.GroupBy, input.Sort, input.Kpis, fromDate, toDate, input.CalcFields), ct);

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
                kpis = result.Kpis.Select(k => new
                {
                    label = k.Label,
                    tint = k.Tint,
                    tint_token = ReportTintTokens.FromHex(k.Tint),
                    value = k.Value,
                    is_money = k.IsMoney
                })
            },
            meta = new { page = 1, page_size = DynamicDescriptorFactory.PreviewLimit, total = result.TotalRows }
        });
    }

    // ======== LICH GUI BAO CAO QUA EMAIL P3.3 (docs/prd/report-builder-prd.md) ======== //

    /// <summary>Danh sach lich gui bao cao qua email cua tenant hien tai.</summary>
    [HttpGet("schedules")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> GetSchedules(CancellationToken ct)
    {
        var schedules = await _mediator.Send(new GetReportSchedulesQuery(), ct);
        return Ok(new { data = schedules.Select(ToScheduleResponse) });
    }

    /// <summary>Tao 1 lich gui bao cao qua email moi.</summary>
    [HttpPost("schedules")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> CreateSchedule([FromBody] SaveReportScheduleRequest request, CancellationToken ct)
    {
        var input = ReportScheduleRequestMapper.ToInput(request);
        var created = await _mediator.Send(new CreateReportScheduleCommand(input), ct);
        return Ok(new { data = ToScheduleResponse(created) });
    }

    /// <summary>Sua 1 lich gui bao cao (chi chu so huu hoac admin).</summary>
    [HttpPut("schedules/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> UpdateSchedule(string id, [FromBody] SaveReportScheduleRequest request, CancellationToken ct)
    {
        var input = ReportScheduleRequestMapper.ToInput(request);
        var updated = await _mediator.Send(new UpdateReportScheduleCommand(id, input), ct);
        return Ok(new { data = ToScheduleResponse(updated) });
    }

    /// <summary>Xoa (mem) 1 lich gui bao cao (chi chu so huu hoac admin).</summary>
    [HttpDelete("schedules/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> DeleteSchedule(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReportScheduleCommand(id), ct);
        return Ok(new { data = new { id, deleted = true } });
    }

    private static object ToScheduleResponse(ReportSchedule s) => new
    {
        id = s.Id,
        report_code = s.ReportCode,
        title = s.Title,
        frequency = ReportScheduleCodes.ToCode(s.Frequency),
        hour = s.Hour,
        day_of_week = s.DayOfWeek,
        day_of_month = s.DayOfMonth,
        period = ReportScheduleCodes.ToCode(s.Period),
        format = ReportScheduleCodes.ToCode(s.Format),
        recipients = s.Recipients,
        enabled = s.Enabled,
        last_run_at = s.LastRunAt,
        created_by = s.CreatedBy,
        created_at = s.CreatedAt,
        updated_by = s.UpdatedBy,
        updated_at = s.UpdatedAt
    };

    // ======== DASHBOARD TUY BIEN P2.2 (docs/prd/report-builder-prd.md) — ghim nhieu bao cao thanh widget ======== //

    /// <summary>Danh sach dashboard tuy bien (cua tenant hien tai — TENANT hoac PRIVATE cua chinh minh).</summary>
    [HttpGet("dashboards")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDashboards(CancellationToken ct)
    {
        var dashboards = await _mediator.Send(new GetReportDashboardsQuery(), ct);
        return Ok(new { data = dashboards.Select(ToDashboardResponse) });
    }

    /// <summary>Chi tiet 1 dashboard (title + widgets).</summary>
    [HttpGet("dashboards/{id}")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDashboardById(string id, CancellationToken ct)
    {
        var dashboard = await _mediator.Send(new GetReportDashboardByIdQuery(id), ct);
        return Ok(new { data = ToDashboardResponse(dashboard) });
    }

    /// <summary>Tao 1 dashboard tuy bien moi (ghim toi da 12 widget).</summary>
    [HttpPost("dashboards")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> CreateDashboard([FromBody] SaveReportDashboardRequest request, CancellationToken ct)
    {
        var input = ReportDashboardRequestMapper.ToInput(request.Title, request.Widgets, request.Visibility);
        var created = await _mediator.Send(new CreateReportDashboardCommand(input), ct);
        return Ok(new { data = ToDashboardResponse(created) });
    }

    /// <summary>Sua 1 dashboard tuy bien (chi chu so huu hoac admin).</summary>
    [HttpPut("dashboards/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> UpdateDashboard(string id, [FromBody] SaveReportDashboardRequest request, CancellationToken ct)
    {
        var input = ReportDashboardRequestMapper.ToInput(request.Title, request.Widgets, request.Visibility);
        var updated = await _mediator.Send(new UpdateReportDashboardCommand(id, input), ct);
        return Ok(new { data = ToDashboardResponse(updated) });
    }

    /// <summary>Xoa (mem) 1 dashboard tuy bien (chi chu so huu hoac admin).</summary>
    [HttpDelete("dashboards/{id}")]
    [RequirePermission("report.build")]
    public async Task<IActionResult> DeleteDashboard(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReportDashboardCommand(id), ct);
        return Ok(new { data = new { id, deleted = true } });
    }

    /// <summary>Chay du lieu tung widget cua 1 dashboard theo khoang ngay (tai su dung pipeline bao cao thuong).</summary>
    [HttpGet("dashboards/{id}/data")]
    [RequirePermission("report.read")]
    public async Task<IActionResult> GetDashboardData(
        string id,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        var result = await _mediator.Send(new GetReportDashboardDataQuery(id, fromDate, toDate), ct);

        return Ok(new
        {
            data = new
            {
                title = result.Title,
                widgets = result.Widgets.Select(w => new
                {
                    report_code = w.ReportCode,
                    title = w.Title,
                    widget_type = w.WidgetType.ToString().ToUpperInvariant(),
                    payload = ToReportDataResponse(w.Payload)
                })
            }
        });
    }

    private static object ToReportDataResponse(ReportDataResult result) => new
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
        kpis = result.Kpis.Select(k => new
        {
            label = k.Label,
            tint = k.Tint,
            tint_token = ReportTintTokens.FromHex(k.Tint),
            value = k.Value,
            is_money = k.IsMoney
        })
    };

    private static object ToDashboardResponse(ReportDashboard d) => new
    {
        id = d.Id,
        code = d.Code,
        title = d.Title,
        widgets = d.Widgets.Select(w => new
        {
            report_code = w.ReportCode,
            title = w.Title,
            widget_type = w.WidgetType.ToString().ToUpperInvariant(),
            w = w.W,
            h = w.H,
            x = w.X,
            y = w.Y
        }),
        visibility = d.Visibility.ToString().ToUpperInvariant(),
        is_active = d.IsActive,
        created_by = d.CreatedBy,
        created_at = d.CreatedAt,
        updated_by = d.UpdatedBy,
        updated_at = d.UpdatedAt
    };

    private static object ToDefinitionResponse(ReportDefinition d) => new
    {
        id = d.Id,
        code = d.Code,
        title = d.Title,
        dataset_key = d.DatasetKey,
        definition = new
        {
            columns = d.Columns.Select(c => new { field = c.Field, label = c.Label, agg = c.Agg is null ? null : ReportAggregationCodes.ToCode(c.Agg.Value), is_subtotal = c.IsSubtotal }),
            filters = d.Filters.Select(f => new { field = f.Field, op = f.Op, value = f.Value }),
            group_by = d.GroupBy,
            sort = d.Sort.Select(s => new { field = s.Field, desc = s.Desc }),
            kpis = d.Kpis.Select(k => new { label = k.Label, field = k.Field, agg = ReportAggregationCodes.ToCode(k.Agg) }),
            calc_fields = d.CalcFields.Select(c => new { key = c.Key, label = c.Label, formula = c.Formula, data_type = c.DataType })
        },
        chart = d.Chart is null ? null : new { type = d.Chart.Type, dims = d.Chart.Dims, measure = d.Chart.Measure },
        view_type = d.ViewType.ToString().ToUpperInvariant(),
        visibility = d.Visibility.ToString().ToUpperInvariant(),
        shared_roles = d.SharedRoles,
        is_active = d.IsActive,
        created_by = d.CreatedBy,
        created_at = d.CreatedAt,
        updated_by = d.UpdatedBy,
        updated_at = d.UpdatedAt
    };
}
