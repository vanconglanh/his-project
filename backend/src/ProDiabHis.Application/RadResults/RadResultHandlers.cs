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
// Bug tien nhiem #2: cac handler duoi day truoc kia tro toi bang khong ton tai
// "cli_rad_results". Bang that la diab_his_rad_results, FK "order_id" ->
// diab_his_rad_orders.id (xem constraint fk_rad_results_order trong DB).
// patient_id/encounter_id/modality KHONG luu trung lap tren diab_his_rad_results
// ma duoc JOIN qua diab_his_rad_orders (encounter_id, modality) roi
// diab_his_enc_encounters (patient_id) — tranh du thua du lieu.
// Cac cot conclusion/status/verified_at/verified_by/dicom_count duoc bo sung
// boi migration 9037_rad_results_add_workflow_columns.sql.
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
        r.status is null ? "DRAFT" : (string)r.status,
        r.verified_at is DateTime va ? va : (DateTime?)null,
        string.IsNullOrEmpty((string?)r.verified_by) ? null : ParseGuidSafe(r.verified_by) is Guid vb && vb != Guid.Empty ? vb : (Guid?)null,
        r.dicom_count is null ? 0 : (int)r.dicom_count,
        signedPdfUrl ?? (string?)r.signed_pdf_path,
        r.created_at is DateTime ca ? ca : DateTime.MinValue);

    // Cau truc SELECT dung chung cho List/Create/Update/Verify — alias ve dung
    // ten field ma Map() phia tren doc.
    public const string SelectCols = @"
        rr.id, rr.tenant_id, rr.deleted_at,
        rr.order_id       AS rad_order_id,
        enc.patient_id    AS patient_id,
        ro.encounter_id   AS encounter_id,
        ro.modality       AS modality,
        rr.description    AS findings,
        rr.impression     AS impression,
        rr.conclusion     AS conclusion,
        rr.recommendation AS recommendations,
        rr.performed_at   AS performed_at,
        rr.performed_by   AS performed_by,
        rr.status         AS status,
        rr.verified_at    AS verified_at,
        rr.verified_by    AS verified_by,
        rr.dicom_count    AS dicom_count,
        rr.result_pdf_path AS signed_pdf_path,
        rr.created_at     AS created_at";

    public const string FromJoin = @"
        FROM diab_his_rad_results rr
        JOIN diab_his_rad_orders ro ON ro.id = rr.order_id
        LEFT JOIN diab_his_enc_encounters enc ON enc.id = ro.encounter_id";
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
        var where = "WHERE rr.tenant_id=@TId AND rr.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("TId", _tenant.TenantId);

        if (q.PatientId.HasValue)  { where += " AND enc.patient_id=@PId"; p.Add("PId", q.PatientId.Value.ToString()); }
        if (q.EncounterId.HasValue){ where += " AND ro.encounter_id=@EId"; p.Add("EId", q.EncounterId.Value.ToString()); }
        if (q.RadOrderId.HasValue) { where += " AND rr.order_id=@RId";     p.Add("RId", q.RadOrderId.Value.ToString()); }
        if (!string.IsNullOrEmpty(q.Status)) { where += " AND rr.status=@St"; p.Add("St", q.Status); }

        var offset = (q.Page - 1) * q.PageSize;
        p.Add("Limit", q.PageSize); p.Add("Offset", offset);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) {Mapper.FromJoin} {where}", p);

        var rows = await conn.QueryAsync<dynamic>(
            $"SELECT {Mapper.SelectCols} {Mapper.FromJoin} {where} ORDER BY rr.performed_at DESC LIMIT @Limit OFFSET @Offset", p);

        List<RadResultResponse> items = rows.Select(r => (RadResultResponse)Mapper.Map(r)).ToList();
        return Result<(IReadOnlyList<RadResultResponse>, int)>.Success((items, total));
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
            @"SELECT ro.id, ro.encounter_id, ro.modality, enc.patient_id
              FROM diab_his_rad_orders ro
              LEFT JOIN diab_his_enc_encounters enc ON enc.id = ro.encounter_id
              WHERE ro.id=@Id AND ro.tenant_id=@TId AND ro.deleted_at IS NULL",
            new { Id = req.RadOrderId.ToString(), TId = _tenant.TenantId });

        if (order is null)
            return Result<RadResultResponse>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy chỉ định CĐHA");

        var id     = Guid.NewGuid().ToString();
        var now    = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_rad_results
                (id, tenant_id, order_id, description, impression, conclusion, recommendation,
                 performed_at, performed_by, status, dicom_count,
                 created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @OId, @Findings, @Impression, @Conclusion, @Recs,
                 @PerAt, @UserId, 'DRAFT', 0,
                 @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId,
                OId = req.RadOrderId.ToString(),
                Findings = req.Findings, Impression = req.Impression,
                Conclusion = req.Conclusion, Recs = req.Recommendations,
                PerAt = req.PerformedAt, UserId = userId, Now = now
            });

        await _audit.LogAsync("CREATE", "RadResult", id, new { req.RadOrderId }, ct);

        var row = await conn.QueryFirstAsync<dynamic>(
            $"SELECT {Mapper.SelectCols} {Mapper.FromJoin} WHERE rr.id=@Id", new { Id = id });
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
            "SELECT id, status FROM diab_his_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var req       = cmd.Req;
        var newStatus = (string)row.status == RadResultStatus.Verified ? RadResultStatus.Amended : (string)row.status;

        await conn.ExecuteAsync(@"
            UPDATE diab_his_rad_results SET
                description     = COALESCE(@Findings, description),
                impression      = COALESCE(@Impression, impression),
                conclusion      = COALESCE(@Conclusion, conclusion),
                recommendation  = COALESCE(@Recs, recommendation),
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

        // Row day du (giong ExportRadResultPdfQueryHandler) de _pdfExporter co du thong tin
        // benh nhan/bac si/phong kham can thiet khi build PDF ky so.
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT rr.id, rr.tenant_id, rr.order_id, rr.impression, rr.description, rr.recommendation,
                     rr.performed_at, rr.performed_by,
                     ro.modality, ro.body_part, ro.contrast, ro.procedure_name, ro.encounter_id,
                     enc.patient_id, enc.doctor_id,
                     pat.code AS patient_code, pat.full_name AS patient_full_name, pat.gender AS patient_gender,
                     pat.date_of_birth AS patient_dob, pat.street AS patient_address,
                     doc.full_name AS doctor_full_name,
                     t.name AS clinic_name, t.cskcb_code AS cskcb_code, t.company_name AS company_name,
                     t.address AS clinic_address, t.phone AS clinic_phone, t.email AS clinic_email,
                     t.email_support AS clinic_email_support, t.logo_url AS clinic_logo_url,
                     t.slogan AS clinic_slogan, t.website AS clinic_website
              FROM diab_his_rad_results rr
              JOIN diab_his_rad_orders ro ON ro.id = rr.order_id
              LEFT JOIN diab_his_enc_encounters enc ON enc.id = ro.encounter_id
              LEFT JOIN diab_his_pat_patients pat ON pat.id = enc.patient_id AND pat.tenant_id = rr.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = enc.doctor_id
              LEFT JOIN diab_his_sys_tenants t ON t.id = rr.tenant_id
              WHERE rr.id=@Id AND rr.tenant_id=@TId AND rr.deleted_at IS NULL",
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
            UPDATE diab_his_rad_results SET
                status='VERIFIED', verified_at=@Now, verified_by=@UserId,
                result_pdf_path=@PdfPath, updated_at=@Now
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
            "SELECT id, dicom_count FROM diab_his_rad_results WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
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
            "UPDATE diab_his_rad_results SET dicom_count=@Count, updated_at=@Now WHERE id=@Id",
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
            @"SELECT rr.id, rr.tenant_id, rr.order_id, rr.impression, rr.description, rr.recommendation,
                     rr.performed_at, rr.performed_by,
                     ro.modality, ro.body_part, ro.contrast, ro.procedure_name, ro.encounter_id,
                     enc.patient_id, enc.doctor_id,
                     pat.code AS patient_code, pat.full_name AS patient_full_name, pat.gender AS patient_gender,
                     pat.date_of_birth AS patient_dob, pat.street AS patient_address,
                     doc.full_name AS doctor_full_name,
                     t.name AS clinic_name, t.cskcb_code AS cskcb_code, t.company_name AS company_name,
                     t.address AS clinic_address, t.phone AS clinic_phone, t.email AS clinic_email,
                     t.email_support AS clinic_email_support, t.logo_url AS clinic_logo_url,
                     t.slogan AS clinic_slogan, t.website AS clinic_website
              FROM diab_his_rad_results rr
              JOIN diab_his_rad_orders ro ON ro.id = rr.order_id
              LEFT JOIN diab_his_enc_encounters enc ON enc.id = ro.encounter_id
              LEFT JOIN diab_his_pat_patients pat ON pat.id = enc.patient_id AND pat.tenant_id = rr.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = enc.doctor_id
              LEFT JOIN diab_his_sys_tenants t ON t.id = rr.tenant_id
              WHERE rr.id=@Id AND rr.tenant_id=@TId AND rr.deleted_at IS NULL",
            new { Id = q.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<byte[]>.Failure("RAD_RESULT_NOT_FOUND", "Không tìm thấy kết quả CĐHA");

        var pdf = await _pdfExporter.ExportRadResultAsync(row, ct);
        return Result<byte[]>.Success(pdf);
    }
}
