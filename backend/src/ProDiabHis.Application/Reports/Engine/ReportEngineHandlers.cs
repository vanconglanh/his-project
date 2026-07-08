using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Reports.Engine;

// ---- Catalog ---- //
public class GetReportCatalogHandler : IRequestHandler<GetReportCatalogQuery, IReadOnlyList<ReportDescriptor>>
{
    private readonly IReportRegistry _registry;

    public GetReportCatalogHandler(IReportRegistry registry) => _registry = registry;

    public Task<IReadOnlyList<ReportDescriptor>> Handle(GetReportCatalogQuery request, CancellationToken ct)
        => Task.FromResult(_registry.GetAll());
}

// ---- Data (grid JSON) ---- //
public class GetReportDataHandler : IRequestHandler<GetReportDataQuery, ReportDataResult>
{
    private readonly IGenericReportDataService _svc;

    public GetReportDataHandler(IGenericReportDataService svc) => _svc = svc;

    public Task<ReportDataResult> Handle(GetReportDataQuery request, CancellationToken ct)
        => _svc.GetDataAsync(request.Code, request.From, request.To, request.Filters, request.Page, request.PageSize, ct);
}

// ---- Export (PDF / Excel) ---- //
public class ExportGenericReportHandler : IRequestHandler<ExportGenericReportQuery, (byte[] Bytes, string ContentType, string FileName)>
{
    private readonly IReportRegistry _registry;
    private readonly IGenericReportDataService _dataSvc;
    private readonly IGenericReportPdfExporter _pdfExporter;
    private readonly IExcelExporter _excelExporter;
    private readonly IReportCodeGenerator _codeGen;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public ExportGenericReportHandler(
        IReportRegistry registry,
        IGenericReportDataService dataSvc,
        IGenericReportPdfExporter pdfExporter,
        IExcelExporter excelExporter,
        IReportCodeGenerator codeGen,
        IDapperConnectionFactory db,
        ITenantProvider tenant,
        ICurrentUser currentUser)
    {
        _registry = registry;
        _dataSvc = dataSvc;
        _pdfExporter = pdfExporter;
        _excelExporter = excelExporter;
        _codeGen = codeGen;
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> Handle(
        ExportGenericReportQuery request, CancellationToken ct)
    {
        var descriptor = _registry.GetByCode(request.Code)
            ?? throw new ReportValidationException("REPORT_NOT_FOUND", $"Không tìm thấy báo cáo '{request.Code}'");

        var data = await _dataSvc.GetDataAsync(request.Code, request.From, request.To, request.Filters, 1, int.MaxValue, ct);

        var format = request.Format.ToLowerInvariant();

        if (format == "excel" || format == "xlsx")
        {
            var bytes = _excelExporter.ExportGeneric(descriptor, data, descriptor.Title);
            var fileName = $"{descriptor.Code}-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.xlsx";
            return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // PDF
        int tenantId = _tenant.TenantId;
        using var conn = _db.CreateConnection();

        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });
        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reportCode = await _codeGen.NextAsync(tenantId, descriptor.PdfTypeCode, today, ct);

        string? exportedByFullName = null;
        if (_currentUser.UserId.HasValue)
        {
            exportedByFullName = await conn.ExecuteScalarAsync<string?>(
                "SELECT full_name FROM diab_his_sec_users WHERE id = @id AND deleted_at IS NULL LIMIT 1",
                new { id = _currentUser.UserId.Value.ToString() });
        }

        var pdfReq = new ReportPdfRequest(
            TenantId: tenantId,
            ReportType: ReportType.Financial, // gia tri phu tro — Report Engine dung PdfTypeCode rieng, khong dung enum nay
            FromDate: request.From,
            ToDate: request.To,
            ClinicId: null,
            ExportedByUserId: _currentUser.UserId,
            ReportCode: reportCode,
            ExportedByFullName: exportedByFullName);

        var pdfBytes = await _pdfExporter.ExportAsync(descriptor, pdfReq, lh, data, ct);
        var pdfFileName = $"{descriptor.Code}-{reportCode}.pdf";
        return (pdfBytes, "application/pdf", pdfFileName);
    }
}

// ---- Options (dropdown filters) ---- //
public class GetReportOptionsHandler : IRequestHandler<GetReportOptionsQuery, IReadOnlyList<ReportOptionItem>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetReportOptionsHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ReportOptionItem>> Handle(GetReportOptionsQuery request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;

        IEnumerable<(string Value, string Label)> rows = request.Source.ToLowerInvariant() switch
        {
            "collectors" => await conn.QueryAsync<(string, string)>(
                @"SELECT DISTINCT u.id, u.full_name
                  FROM diab_his_sec_users u
                  INNER JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id AND ur.tenant_id = @tenantId
                  INNER JOIN diab_his_sec_roles r ON r.id = ur.role_id
                  WHERE u.tenant_id = @tenantId AND u.deleted_at IS NULL AND u.is_active = 1
                    AND r.code IN ('ke_toan', 'le_tan', 'admin')
                  ORDER BY u.full_name",
                new { tenantId }),

            "doctors" => await conn.QueryAsync<(string, string)>(
                @"SELECT DISTINCT u.id, u.full_name
                  FROM diab_his_sec_users u
                  INNER JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id AND ur.tenant_id = @tenantId
                  INNER JOIN diab_his_sec_roles r ON r.id = ur.role_id
                  WHERE u.tenant_id = @tenantId AND u.deleted_at IS NULL AND u.is_active = 1
                    AND r.code = 'bac_si'
                  ORDER BY u.full_name",
                new { tenantId }),

            "counters" => await conn.QueryAsync<(string, string)>(
                @"SELECT id, name FROM diab_his_bil_counters
                  WHERE tenant_id = @tenantId AND deleted_at IS NULL AND status = 1
                  ORDER BY sort_order",
                new { tenantId }),

            "clinics" => await conn.QueryAsync<(string, string)>(
                "SELECT CAST(id AS CHAR) AS Value, name AS Label FROM diab_his_sys_tenants WHERE id = @tenantId",
                new { tenantId }),

            "patients" => await conn.QueryAsync<(string, string)>(
                @"SELECT id, CONCAT(full_name, ' (', code, ')')
                  FROM diab_his_pat_patients
                  WHERE tenant_id = @tenantId AND deleted_at IS NULL
                  ORDER BY full_name
                  LIMIT 200",
                new { tenantId }),

            _ => throw new ReportValidationException("REPORT_INVALID_OPTIONS_SOURCE", $"Nguồn tùy chọn '{request.Source}' không hợp lệ")
        };

        return rows.Select(r => new ReportOptionItem(r.Item1, r.Item2)).ToList();
    }
}
