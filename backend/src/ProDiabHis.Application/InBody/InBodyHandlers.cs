using System.Data;
using System.Text.Json;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.InBody;

file static class InBodyMapper
{
    public static InBodyReportResponse Map(dynamic r, IReadOnlyList<InBodyFieldDto> fields, string? fileUrl)
    {
        return new InBodyReportResponse(
            Guid.Parse((string)r.id),
            Guid.Parse((string)r.patient_id),
            r.encounter_id is not null ? Guid.Parse((string)r.encounter_id) : null,
            (string)r.extraction_status,
            fileUrl,
            fields,
            r.confirmed_by is not null ? Guid.Parse((string)r.confirmed_by) : null,
            r.confirmed_at is not null ? (DateTime)r.confirmed_at : null,
            (DateTime)r.created_at);
    }

    public static IReadOnlyList<InBodyFieldDto> ParseFieldsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<InBodyFieldDto>();
        try
        {
            return JsonSerializer.Deserialize<List<InBodyFieldDto>>(json) ?? new List<InBodyFieldDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<InBodyFieldDto>();
        }
    }
}

// ─────────────────────────────────────────────────
// UPLOAD — chi extract + luu pending, KHONG ghi vao VitalSigns/indicator_reading
// ─────────────────────────────────────────────────
public class UploadInBodyReportCommandHandler : IRequestHandler<UploadInBodyReportCommand, Result<InBodyReportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly IInBodyDataProvider _provider;
    private readonly IAuditService _audit;

    private static readonly string[] AllowedMimes = { "application/pdf" };
    private const long MaxBytes = 15L * 1024 * 1024;

    public UploadInBodyReportCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, IInBodyDataProvider provider, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _storage = storage; _provider = provider; _audit = audit;
    }

    public async Task<Result<InBodyReportResponse>> Handle(UploadInBodyReportCommand cmd, CancellationToken ct)
    {
        if (!AllowedMimes.Contains(cmd.ContentType))
            return Result<InBodyReportResponse>.Failure("INBODY_INVALID_FORMAT", "Chỉ chấp nhận file PDF");

        using var conn = (IDbConnection)_db.CreateConnection();
        var patientIdStr = cmd.PatientId.ToString();
        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = patientIdStr, TenantId = _tenant.TenantId });
        if (patientExists == 0)
            return Result<InBodyReportResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        // Doc toan bo stream vao memory de vua upload len storage, vua dua cho provider extract
        // (tranh doc stream 2 lan tren cung 1 Stream chi forward-only).
        using var buffer = new MemoryStream();
        await cmd.FileStream.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxBytes)
            return Result<InBodyReportResponse>.Failure("INBODY_TOO_LARGE", "File vượt quá dung lượng tối đa 15MB");
        var sizeBytes = buffer.Length;

        buffer.Position = 0;
        var extractResult = await _provider.ExtractAsync(buffer, cmd.FileName, ct);
        if (!extractResult.IsSuccess)
            return Result<InBodyReportResponse>.Failure(extractResult.ErrorCode!, extractResult.ErrorMessage!);
        var data = extractResult.Value!;

        var fileId = Guid.NewGuid();
        var objectKey = $"inbody/{_tenant.TenantId}/{cmd.PatientId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}.pdf";

        buffer.Position = 0;
        await _storage.UploadAsync(FileBuckets.InBodyReports, objectKey, buffer, cmd.ContentType, ct);
        var signedUrl = await TryGetSignedUrlAsync(objectKey, ct);

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(@"
            INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
            VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, @Size, 'INBODY', @UploadedBy, @Now, @Now)",
            new
            {
                Id = fileId.ToString(),
                TenantId = _tenant.TenantId,
                Bucket = FileBuckets.InBodyReports,
                Key = objectKey,
                FileName = cmd.FileName,
                Mime = cmd.ContentType,
                Size = sizeBytes,
                UploadedBy = _user.UserId?.ToString(),
                Now = now
            });

        var fieldDtos = data.Fields.Select(f => new InBodyFieldDto(f.IndicatorType, f.Value, f.Unit, f.Extracted)).ToList();
        var status = !data.HasAnyExtracted ? "failed" : (data.IsFullyExtracted ? "pending" : "partial");
        // Luu y: status "pending" khi extract du/1 phan deu la trang thai CHUA XAC NHAN — dung
        // "pending" lam mac dinh chung, chi dung "failed" khi khong doc duoc gi (vd PDF scan anh).
        if (status == "partial") status = "pending"; // van la pending cho toi khi confirm; "partial" chi dung SAU khi confirm thieu field

        var reportId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_cli_inbody_report
                (id, tenant_id, patient_id, encounter_id, file_id, file_url, file_name, raw_text, extracted_fields_json, extraction_status, extracted_by, created_at, updated_at)
            VALUES
                (@Id, @TenantId, @PatId, @EncId, @FileId, @FileUrl, @FileName, @RawText, @FieldsJson, @Status, @ExtractedBy, @Now, @Now)",
            new
            {
                Id = reportId.ToString(),
                TenantId = _tenant.TenantId,
                PatId = patientIdStr,
                EncId = cmd.EncounterId?.ToString(),
                FileId = fileId.ToString(),
                FileUrl = objectKey,
                FileName = cmd.FileName,
                RawText = data.RawText,
                FieldsJson = JsonSerializer.Serialize(fieldDtos),
                Status = status,
                ExtractedBy = _user.UserId?.ToString(),
                Now = now
            });

        await _audit.LogAsync("CREATE", "InBodyReport", reportId.ToString(),
            new { patientId = cmd.PatientId, fileName = cmd.FileName, status }, ct);

        var response = new InBodyReportResponse(reportId, cmd.PatientId, cmd.EncounterId, status,
            signedUrl, fieldDtos, null, null, now);
        return Result<InBodyReportResponse>.Success(response);
    }

    private async Task<string?> TryGetSignedUrlAsync(string objectKey, CancellationToken ct)
    {
        try { return await _storage.GetSignedUrlAsync(FileBuckets.InBodyReports, objectKey, 900, ct); }
        catch { return null; }
    }
}

// ─────────────────────────────────────────────────
// CONFIRM — ghi vao VitalSigns (weight) + diab_his_cli_indicator_reading (con lai)
// ─────────────────────────────────────────────────
public class ConfirmInBodyReportCommandHandler : IRequestHandler<ConfirmInBodyReportCommand, Result<InBodyReportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public ConfirmInBodyReportCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _storage = storage; _audit = audit;
    }

    public async Task<Result<InBodyReportResponse>> Handle(ConfirmInBodyReportCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var reportIdStr = cmd.ReportId.ToString();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM diab_his_cli_inbody_report WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = reportIdStr, TenantId = _tenant.TenantId });
        if (row is null)
            return Result<InBodyReportResponse>.Failure("INBODY_REPORT_NOT_FOUND", "Không tìm thấy báo cáo InBody");

        var encounterId = cmd.EncounterId ?? (row.encounter_id is not null ? Guid.Parse((string)row.encounter_id) : (Guid?)null);
        var patientId = Guid.Parse((string)row.patient_id);

        var included = cmd.Fields.Where(f => f.Include).ToList();
        var weightField = included.FirstOrDefault(f => f.IndicatorType == InBodyIndicatorTypes.Weight);

        if (weightField is not null)
        {
            if (encounterId is null)
                return Result<InBodyReportResponse>.Failure("INBODY_ENCOUNTER_REQUIRED", "Cần chọn lượt khám để ghi cân nặng vào sinh hiệu");
            if (!weightField.Value.HasValue)
                return Result<InBodyReportResponse>.Failure("INBODY_INVALID_VALUE", "Giá trị cân nặng không hợp lệ");

            var encIdStr = encounterId.Value.ToString();
            var encExists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_enc_encounters WHERE id=@Id AND tenant_id=@TenantId",
                new { Id = encIdStr, TenantId = _tenant.TenantId });
            if (encExists == 0)
                return Result<InBodyReportResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

            var maxSeq = await conn.ExecuteScalarAsync<int?>(
                "SELECT MAX(record_sequence) FROM diab_his_enc_vital_signs WHERE encounter_id=@EncId", new { EncId = encIdStr }) ?? 0;

            var vitalId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_enc_vital_signs
                    (id, tenant_id, encounter_id, patient_id, recorded_at, recorded_by, record_sequence, weight_kg, note, created_at, created_by)
                VALUES
                    (@Id, @TenantId, @EncId, @PatId, @Now, @RecordedBy, @Seq, @WeightKg, @Note, @Now, @RecordedBy)",
                new
                {
                    Id = vitalId.ToString(),
                    TenantId = _tenant.TenantId,
                    EncId = encIdStr,
                    PatId = patientId.ToString(),
                    Now = DateTime.UtcNow,
                    RecordedBy = _user.UserId?.ToString(),
                    Seq = maxSeq + 1,
                    WeightKg = weightField.Value,
                    Note = "Nhập từ kết quả máy InBody (đã xác nhận)"
                });
        }

        foreach (var f in included.Where(f => InBodyIndicatorTypes.IndicatorTableTypes.Contains(f.IndicatorType)))
        {
            if (!f.Value.HasValue) continue;
            await conn.ExecuteAsync(@"
                INSERT INTO diab_his_cli_indicator_reading
                    (id, tenant_id, patient_id, encounter_id, indicator_type, value, unit, source, source_ref_id, recorded_at, recorded_by, created_at)
                VALUES
                    (@Id, @TenantId, @PatId, @EncId, @IndicatorType, @Value, @Unit, 'inbody_ocr', @SourceRefId, @Now, @RecordedBy, @Now)",
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = _tenant.TenantId,
                    PatId = patientId.ToString(),
                    EncId = encounterId?.ToString(),
                    f.IndicatorType,
                    f.Value,
                    f.Unit,
                    SourceRefId = reportIdStr,
                    Now = DateTime.UtcNow,
                    RecordedBy = _user.UserId?.ToString()
                });
        }

        var allOriginalFields = InBodyMapper.ParseFieldsJson((string?)row.extracted_fields_json);
        var missingExtracted = allOriginalFields.Any(f => !f.Extracted);
        var noneIncluded = included.Count == 0;
        var status = noneIncluded ? "failed" : (missingExtracted ? "partial" : "success");

        var confirmedAt = DateTime.UtcNow;
        await conn.ExecuteAsync(@"
            UPDATE diab_his_cli_inbody_report
            SET extraction_status=@Status, confirmed_by=@ConfirmedBy, confirmed_at=@ConfirmedAt,
                encounter_id=COALESCE(encounter_id, @EncId), updated_at=@Now
            WHERE id=@Id AND tenant_id=@TenantId",
            new
            {
                Status = status,
                ConfirmedBy = _user.UserId?.ToString(),
                ConfirmedAt = confirmedAt,
                EncId = encounterId?.ToString(),
                Now = confirmedAt,
                Id = reportIdStr,
                TenantId = _tenant.TenantId
            });

        await _audit.LogAsync("CONFIRM", "InBodyReport", reportIdStr,
            new { patientId, encounterId, includedFields = included.Select(f => f.IndicatorType), status }, ct);

        string? signedUrl = null;
        if (row.file_url is not null)
        {
            try { signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.InBodyReports, (string)row.file_url, 900, ct); }
            catch { /* MinIO co the khong san sang trong test */ }
        }

        InBodyReportResponse baseResponse = InBodyMapper.Map(row, allOriginalFields, signedUrl);
        var response = baseResponse with
        {
            ExtractionStatus = status,
            ConfirmedBy = _user.UserId,
            ConfirmedAt = confirmedAt,
            EncounterId = encounterId
        };
        return Result<InBodyReportResponse>.Success(response);
    }
}

// ─────────────────────────────────────────────────
// LIST — lich su theo benh nhan
// ─────────────────────────────────────────────────
public class ListInBodyReportsQueryHandler : IRequestHandler<ListInBodyReportsQuery, Result<PagedResult<InBodyReportResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public ListInBodyReportsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    { _db = db; _tenant = tenant; _storage = storage; }

    public async Task<Result<PagedResult<InBodyReportResponse>>> Handle(ListInBodyReportsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var patientIdStr = q.PatientId.ToString();

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = patientIdStr, TenantId = _tenant.TenantId });
        if (patientExists == 0)
            return Result<PagedResult<InBodyReportResponse>>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : q.PageSize;
        var offset = (page - 1) * pageSize;

        var rows = await conn.QueryAsync(@"
            SELECT * FROM diab_his_cli_inbody_report
            WHERE tenant_id=@TenantId AND patient_id=@PatientId AND deleted_at IS NULL
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset",
            new { TenantId = _tenant.TenantId, PatientId = patientIdStr, PageSize = pageSize, Offset = offset });

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_cli_inbody_report WHERE tenant_id=@TenantId AND patient_id=@PatientId AND deleted_at IS NULL",
            new { TenantId = _tenant.TenantId, PatientId = patientIdStr });

        var items = new List<InBodyReportResponse>();
        foreach (var r in rows)
        {
            var fields = InBodyMapper.ParseFieldsJson((string?)r.extracted_fields_json);
            string? signedUrl = null;
            if (r.file_url is not null)
            {
                try { signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.InBodyReports, (string)r.file_url, 900, ct); }
                catch { }
            }
            items.Add(InBodyMapper.Map(r, fields, signedUrl));
        }

        return Result<PagedResult<InBodyReportResponse>>.Success(new PagedResult<InBodyReportResponse>(items, page, pageSize, total));
    }
}
