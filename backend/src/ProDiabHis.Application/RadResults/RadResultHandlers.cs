using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.RadResults;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════
public record ListRadResultsQuery(
    Guid? PatientId, Guid? EncounterId, Guid? RadOrderId,
    string? Status, int Page, int PageSize)
    : IRequest<Result<(IReadOnlyList<RadResultResponse> Items, int Total)>>;

public record CreateRadResultCommand(RadResultCreateRequest Req)
    : IRequest<Result<RadResultResponse>>;

public record UpdateRadResultCommand(Guid Id, RadResultUpdateRequest Req)
    : IRequest<Result<bool>>;

public record VerifyRadResultCommand(Guid Id)
    : IRequest<Result<string>>;  // returns signed_pdf_url

public record UploadDicomCommand(Guid RadResultId, IReadOnlyList<(Stream Stream, string FileName, long SizeBytes)> Files)
    : IRequest<Result<(int UploadedCount, long TotalSizeBytes)>>;

public record ExportRadResultPdfQuery(Guid Id)
    : IRequest<Result<byte[]>>;

// ═══════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════
file static class Mapper
{
    private static Guid ParseGuidSafe(object? v) =>
        v is null ? Guid.Empty
        : v is Guid g ? g
        : Guid.TryParse(v?.ToString(), out var p) ? p : Guid.Empty;

    public static RadResultResponse Map(dynamic r, string? signedPdfUrl = null) => new(
        ParseGuidSafe(r.id),
        ParseGuidSafe(r.rad_order_id),
        ParseGuidSafe(r.patient_id),
        ParseGuidSafe(r.encounter_id),
        r.modality is null ? "" : (string)r.modality,
        r.findings is null ? "" : (string)r.findings,
        (string?)r.impression,
        r.conclusion is null ? "" : (string)r.conclusion,
        (string?)r.recommendations,
        r.performed_at is null ? DateTime.MinValue : (DateTime)r.performed_at,
        string.IsNullOrEmpty((string?)r.performed_by) ? null : ParseGuidSafe(r.performed_by) is Guid pb && pb != Guid.Empty ? pb : (Guid?)null,
        r.status is null ? "PRELIMINARY" : (string)r.status,
        r.verified_at is DateTime va ? va : (DateTime?)null,
        string.IsNullOrEmpty((string?)r.verified_by) ? null : ParseGuidSafe(r.verified_by) is Guid vb && vb != Guid.Empty ? vb : (Guid?)null,
        r.dicom_count is null ? 0 : (int)r.dicom_count,
        signedPdfUrl ?? (string?)r.signed_pdf_path,
        r.created_at is DateTime ca ? ca : DateTime.MinValue);
}

// ═══════════════════════════════════════════════
// LIST
// ═══════════════════════════════════════════════
public class ListRadResultsQueryHandler
    : IRequestHandler<ListRadResultsQuery, Result<(IReadOnlyList<RadResultResponse>, int)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListRadResultsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<(IReadOnlyList<RadResultResponse>, int)>> Handle(
        ListRadResultsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE tenant_id=@TId AND deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("TId", _tenant.TenantId);

        if (q.PatientId.HasValue)  { where += " AND patient_id=@PId";   p.Add("PId", q.PatientId.Value.ToString()); }
        if (q.EncounterId.HasValue){ where += " AND encounter_id=@EId";  p.Add("EId", q.EncounterId.Value.ToString()); }
        if (q.RadOrderId.HasValue) { where += " AND rad_order_id=@RId";  p.Add("RId", q.RadOrderId.Value.ToString()); }
        if (!string.IsNullOrEmpty(q.Status)) { where += " AND status=@St"; p.Add("St", q.Status); }

        var offset = (q.Page - 1) * q.PageSize;
        p.Add("Limit", q.PageSize); p.Add("Offset", offset);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM cli_rad_results {where}", p);

        // cli_rad_results dung UPPERCASE column names — alias ve lowercase cho Mapper
        const string selectCols = @"
            id, tenant_id, deleted_at,
            RAD_ORDER_ID    AS rad_order_id,
            PATIENT_ID      AS patient_id,
            NULL            AS encounter_id,
            NULL            AS modality,
            FINDINGS        AS findings,
            IMPRESSION      AS impression,
            IMPRESSION      AS conclusion,
            RECOMMENDATIONS AS recommendations,
            REPORT_DATE     AS performed_at,
            CAST(REPORTED_BY AS CHAR(36)) AS performed_by,
            COALESCE(RESULT_STATUS,'PRELIMINARY') AS status,
            VERIFICATION_DATE AS verified_at,
            CAST(VERIFIED_BY AS CHAR(36)) AS verified_by,
            0               AS dicom_count,
            NULL            AS signed_pdf_path,
            CREATED_AT      AS created_at";
        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT {selectCols} FROM cli_rad_results {where} ORDER BY REPORT_DATE DESC LIMIT @Limit OFFSET @Offset", p);

        List<RadResultResponse> items = rows.Select(r => (RadResultResponse)Mapper.Map(r)).ToList();
        return Result<(IReadOnlyList<RadResultResponse>, int)>.Success(
            (items, total));
    }
}

// ═══════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════
public class CreateRadResultCommandHandler
    : IRequestHandler<CreateRadResultCommand, Result<RadResultResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateRadResultCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<RadResultResponse>> Handle(CreateRadResultCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var req = cmd.Req;

        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT ro.id, ro.encounter_id, ro.modality, v.patient_id
              FROM diab_his_cli_rad_orders ro
              JOIN cli_visits v ON v.id = ro.encounter_id
              WHERE ro.id=@Id AND ro.tenant_id=@TId AND ro.deleted_at IS NULL",
            new { Id = req.RadOrderId.ToString(), TId = _tenant.TenantId });

        if (order is null)
            return Result<RadResultResponse>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy chỉ định CĐHA");

        var id     = Guid.NewGuid().ToString();
        var now    = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync(@"
            INSERT INTO cli_rad_results
                (id, tenant_id, rad_order_id, patient_id, encounter_id, modality,
                 findings, impression, conclusion, recommendations,
                 performed_at, performed_by, status, dicom_count,
                 created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @OId, @PatId, @EId, @Mod,
                 @Findings, @Impression, @Conclusion, @Recs,
                 @PerAt, @UserId, 'DRAFT', 0,
                 @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId,
                OId = req.RadOrderId.ToString(), PatId = (string)order.patient_id,
                EId = (string)order.encounter_id, Mod = (string)order.modality,
                Findings = req.Findings, Impression = req.Impression,
                Conclusion = req.Conclusion, Recs = req.Recommendations,
                PerAt = req.PerformedAt, UserId = userId, Now = now
            });

        await _audit.LogAsync("CREATE", "RadResult", id, new { req.RadOrderId }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM cli_rad_results WHERE id=@Id", new { Id = id });
        return Result<RadResultResponse>.Success(Mapper.Map(row));
    }
}

// ═══════════════════════════════════════════════
// UPDATE
// ═══════════════════════════════════════════════
public class UpdateRadResultCommandHandler
    : IRequestHandler<UpdateRadResultCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public UpdateRadResultCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<bool>> Handle(UpdateRadResultCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM cli_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var req       = cmd.Req;
        var newStatus = (string)row.status == RadResultStatus.Verified ? RadResultStatus.Amended : (string)row.status;

        await conn.ExecuteAsync(@"
            UPDATE cli_rad_results SET
                findings        = COALESCE(@Findings, findings),
                impression      = COALESCE(@Impression, impression),
                conclusion      = COALESCE(@Conclusion, conclusion),
                recommendations = COALESCE(@Recs, recommendations),
                status          = @Status,
                updated_at      = @Now,
                updated_by      = @UserId
            WHERE id=@Id",
            new
            {
                Findings = req.Findings, Impression = req.Impression,
                Conclusion = req.Conclusion, Recs = req.Recommendations,
                Status = newStatus, Now = DateTime.UtcNow,
                UserId = _user.UserId?.ToString(), Id = cmd.Id.ToString()
            });

        await _audit.LogAsync("UPDATE", "RadResult", cmd.Id.ToString(), new { newStatus, req.AmendReason }, ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// VERIFY (ky PDF)
// ═══════════════════════════════════════════════
public class VerifyRadResultCommandHandler
    : IRequestHandler<VerifyRadResultCommand, Result<string>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IFileStorage _fileStorage;
    private readonly ILabResultPdfExporter _pdfExporter;

    public VerifyRadResultCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, IFileStorage fileStorage, ILabResultPdfExporter pdfExporter)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; _fileStorage = fileStorage; _pdfExporter = pdfExporter; }

    public async Task<Result<string>> Handle(VerifyRadResultCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM cli_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<string>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var pdfBytes = await _pdfExporter.ExportRadResultAsync(row, ct);

        // Upload to MinIO
        var pdfPath = $"{_tenant.TenantId}/rad-results/{cmd.Id}/result.pdf";
        using var stream = new MemoryStream(pdfBytes);
        await _fileStorage.UploadAsync("rad-results", pdfPath, stream, "application/pdf", ct);

        var signedUrl = await _fileStorage.GetSignedUrlAsync("rad-results", pdfPath, 3600, ct);

        var now    = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync(@"
            UPDATE cli_rad_results SET
                status='VERIFIED', verified_at=@Now, verified_by=@UserId,
                signed_pdf_path=@PdfPath, updated_at=@Now
            WHERE id=@Id",
            new { Now = now, UserId = userId, PdfPath = pdfPath, Id = cmd.Id.ToString() });

        await _audit.LogAsync("VERIFY", "RadResult", cmd.Id.ToString(), null, ct);
        return Result<string>.Success(signedUrl);
    }
}

// ═══════════════════════════════════════════════
// DICOM UPLOAD
// ═══════════════════════════════════════════════
public class UploadDicomCommandHandler
    : IRequestHandler<UploadDicomCommand, Result<(int, long)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _fileStorage;

    public UploadDicomCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage fileStorage)
    { _db = db; _tenant = tenant; _fileStorage = fileStorage; }

    public async Task<Result<(int, long)>> Handle(UploadDicomCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, dicom_count FROM cli_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.RadResultId.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<(int, long)>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var uploadedCount = 0;
        var totalSize     = 0L;
        var bucket        = "rad-dicom";

        foreach (var (stream, fileName, sizeBytes) in cmd.Files)
        {
            try
            {
                var objectKey = $"{_tenant.TenantId}/{cmd.RadResultId}/{fileName}";
                await _fileStorage.UploadAsync(bucket, objectKey, stream, "application/dicom", ct);
                uploadedCount++;
                totalSize += sizeBytes;
            }
            catch
            {
                return Result<(int, long)>.Failure("RAD_DICOM_UPLOAD_FAILED", "Lỗi upload DICOM lên MinIO");
            }
        }

        var newCount = (int)(row.dicom_count ?? 0) + uploadedCount;
        await conn.ExecuteAsync(
            "UPDATE cli_rad_results SET dicom_count=@Count, updated_at=@Now WHERE id=@Id",
            new { Count = newCount, Now = DateTime.UtcNow, Id = cmd.RadResultId.ToString() });

        return Result<(int, long)>.Success((uploadedCount, totalSize));
    }
}

// ═══════════════════════════════════════════════
// EXPORT PDF
// ═══════════════════════════════════════════════
public class ExportRadResultPdfQueryHandler
    : IRequestHandler<ExportRadResultPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ILabResultPdfExporter _pdfExporter;

    public ExportRadResultPdfQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ILabResultPdfExporter pdfExporter)
    { _db = db; _tenant = tenant; _pdfExporter = pdfExporter; }

    public async Task<Result<byte[]>> Handle(ExportRadResultPdfQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM cli_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = q.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<byte[]>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var pdf = await _pdfExporter.ExportRadResultAsync(row, ct);
        return Result<byte[]>.Success(pdf);
    }
}
