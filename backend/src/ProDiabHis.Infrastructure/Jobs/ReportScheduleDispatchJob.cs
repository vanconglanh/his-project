using System.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;
using ProDiabHis.Infrastructure.Reports;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job (chay moi gio) — quet <c>diab_his_rep_schedules</c> den han (Report Builder P3.3),
/// xuat bao cao (PDF/Excel) theo khoang ngay tuong doi cua <see cref="ReportSchedulePeriod"/>, gui email
/// kem file dinh kem toi danh sach recipients, roi cap nhat last_run_at.
///
/// Job chay nen (khong co HttpContext) nen KHONG dung <see cref="CompositeReportRegistry"/>/ICurrentUser
/// (phu thuoc request hien tai) de tranh bao cao PRIVATE/ROLE cua chinh chu lich bi tu choi vi "khong dang
/// nhap". Thay vao do: uu tien bao cao he thong (code-defined, khong phu thuoc tenant/user) truoc, sau do
/// tra thang <see cref="IReportDefinitionStore"/> theo tenant cua lich (BO QUA kiem tra owner/visibility —
/// da duoc xac thuc 1 lan khi tao lich qua <see cref="ReportScheduleValidation.EnsureReportCodeExists"/>).
/// Loi o 1 lich KHONG duoc lam hong cac lich con lai (catch + log tung lich).
/// </summary>
public class ReportScheduleDispatchJob
{
    private readonly IReportScheduleStore _scheduleStore;
    private readonly ReportRegistry _codeDefinedRegistry;
    private readonly IDatasetRegistry _datasets;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportScheduleDispatchJob> _logger;

    public ReportScheduleDispatchJob(
        IReportScheduleStore scheduleStore,
        ReportRegistry codeDefinedRegistry,
        IDatasetRegistry datasets,
        IServiceScopeFactory scopeFactory,
        ILogger<ReportScheduleDispatchJob> logger)
    {
        _scheduleStore = scheduleStore;
        _codeDefinedRegistry = codeDefinedRegistry;
        _datasets = datasets;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var due = await _scheduleStore.GetDueAsync(DateTime.UtcNow, ct);
        _logger.LogInformation("ReportScheduleDispatchJob: {Count} lich bao cao den han", due.Count);

        foreach (var schedule in due)
        {
            try
            {
                await DispatchAsync(schedule, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportScheduleDispatchJob: loi xu ly lich {Id} (report_code={Code}, tenant={TenantId})",
                    schedule.Id, schedule.ReportCode, schedule.TenantId);
            }
        }
    }

    private async Task DispatchAsync(ReportSchedule schedule, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var descriptor = await ResolveDescriptorAsync(sp, schedule, ct);
        if (descriptor is null)
        {
            _logger.LogWarning("ReportScheduleDispatchJob: khong tim thay bao cao '{Code}' cho lich {Id} — bo qua",
                schedule.ReportCode, schedule.Id);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (from, to) = ReportScheduleCodes.ResolveDateRange(schedule.Period, today);

        var db = sp.GetRequiredService<IDapperConnectionFactory>();
        using var conn = (IDbConnection)db.CreateConnection();

        var ctx = new ReportQueryContext(schedule.TenantId, from, to, new Dictionary<string, string?>());
        var (sql, parameters) = descriptor.BuildQuery(ctx);

        var rawRows = await conn.QueryAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        var rows = rawRows
            .Select(r =>
            {
                var src = (IDictionary<string, object>)r;
                var dict = new Dictionary<string, object?>(src.Count);
                foreach (var kv in src) dict[kv.Key] = kv.Value;
                return (IDictionary<string, object?>)dict;
            })
            .ToList();

        var data = ReportRowsAggregator.BuildFull(descriptor, rows);

        var (bytes, contentType, fileName) = schedule.Format == ReportScheduleFormat.Excel
            ? ExportExcel(sp, descriptor, data, from, to)
            : await ExportPdfAsync(sp, conn, descriptor, data, schedule.TenantId, from, to, today, ct);

        var emailSender = sp.GetRequiredService<IEmailSender>();
        var subject = $"[Pro-Diab HIS] Báo cáo định kỳ: {schedule.Title}";
        var html =
            $"<p>Báo cáo <b>{descriptor.Title}</b> kỳ {from:dd/MM/yyyy} – {to:dd/MM/yyyy} được gửi tự động " +
            $"theo lịch <b>'{schedule.Title}'</b>. File đính kèm: {fileName}.</p>" +
            "<p><i>Email này được hệ thống Pro-Diab HIS gửi tự động, vui lòng không trả lời.</i></p>";

        foreach (var recipient in schedule.Recipients)
        {
            try
            {
                await emailSender.SendWithAttachmentAsync(recipient, subject, html,
                    new EmailAttachment(fileName, bytes, contentType), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportScheduleDispatchJob: loi gui email lich {Id} toi {Recipient}", schedule.Id, recipient);
            }
        }

        await _scheduleStore.MarkRunAsync(schedule.Id, DateTime.UtcNow, ct);
    }

    /// <summary>Uu tien bao cao he thong (code-defined, khong phu thuoc tenant); neu khong thay thi tra
    /// thang IReportDefinitionStore theo tenant cua lich (BO QUA owner check — da xac thuc luc tao lich).</summary>
    private async Task<ReportDescriptor?> ResolveDescriptorAsync(IServiceProvider sp, ReportSchedule schedule, CancellationToken ct)
    {
        var fromCode = _codeDefinedRegistry.GetByCode(schedule.ReportCode);
        if (fromCode is not null) return fromCode;

        var store = sp.GetRequiredService<IReportDefinitionStore>();
        var definition = await store.GetByCodeAsync(schedule.TenantId, schedule.ReportCode, ct);
        if (definition is null) return null;

        var dataset = _datasets.GetByKey(definition.DatasetKey);
        if (dataset is null) return null;

        try { return DynamicDescriptorFactory.Create(definition, dataset); }
        catch (ReportValidationException) { return null; }
    }

    private static (byte[] Bytes, string ContentType, string FileName) ExportExcel(
        IServiceProvider sp, ReportDescriptor descriptor, ReportDataResult data, DateOnly from, DateOnly to)
    {
        var excelExporter = sp.GetRequiredService<IExcelExporter>();
        var bytes = excelExporter.ExportGeneric(descriptor, data, descriptor.Title);
        var fileName = $"{descriptor.Code}-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx";
        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static async Task<(byte[] Bytes, string ContentType, string FileName)> ExportPdfAsync(
        IServiceProvider sp, IDbConnection conn, ReportDescriptor descriptor, ReportDataResult data,
        int tenantId, DateOnly from, DateOnly to, DateOnly today, CancellationToken ct)
    {
        var pdfExporter = sp.GetRequiredService<IGenericReportPdfExporter>();
        var codeGen = sp.GetRequiredService<IReportCodeGenerator>();

        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId });
        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var reportCode = await codeGen.NextAsync(tenantId, descriptor.PdfTypeCode, today, ct);

        var pdfReq = new ReportPdfRequest(
            TenantId: tenantId,
            ReportType: ReportType.Financial, // gia tri phu tro — Report Engine dung PdfTypeCode rieng
            FromDate: from,
            ToDate: to,
            ClinicId: null,
            ExportedByUserId: null,
            ReportCode: reportCode,
            ExportedByFullName: "Hệ thống (lịch gửi tự động)");

        var bytes = await pdfExporter.ExportAsync(descriptor, pdfReq, lh, data, ct);
        var fileName = $"{descriptor.Code}-{reportCode}.pdf";
        return (bytes, "application/pdf", fileName);
    }
}
