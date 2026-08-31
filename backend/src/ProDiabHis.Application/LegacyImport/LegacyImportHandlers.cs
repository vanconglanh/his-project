using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.LegacyImport;

file static class LegacyImportMapper
{
    public static LegacyImportBatchResponse MapBatch(dynamic r) => new(
        Guid.Parse((string)r.id),
        (string?)r.zip_file_name,
        (int)r.total_items,
        (int)r.processed_items,
        (string)r.status,
        (string?)r.error_message,
        (DateTime)r.created_at,
        (DateTime)r.updated_at);

    public static LegacyImportItemResponse MapItem(dynamic r, string? imageUrl, string? patientName, string? patientCode) => new(
        Guid.Parse((string)r.id),
        Guid.Parse((string)r.batch_id),
        (string?)r.original_filename,
        imageUrl,
        (string?)r.ocr_text,
        r.ocr_confidence is not null ? (decimal?)r.ocr_confidence : null,
        r.matched_patient_id is not null ? Guid.Parse((string)r.matched_patient_id) : null,
        patientName,
        patientCode,
        (string?)r.match_method,
        (string)r.status,
        r.saved_cls_upload_id is not null ? Guid.Parse((string)r.saved_cls_upload_id) : null,
        (string?)r.item_error,
        (DateTime)r.created_at);
}

// ─────────────────────────────────────────────────
// CREATE BATCH — upload ZIP goc, tao batch pending, enqueue job OCR nen
// ─────────────────────────────────────────────────
public class CreateLegacyImportBatchCommandHandler : IRequestHandler<CreateLegacyImportBatchCommand, Result<LegacyImportBatchResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly IBackgroundJobEnqueuer _jobs;
    private readonly IAuditService _audit;

    private const long MaxBytes = 200L * 1024 * 1024;

    public CreateLegacyImportBatchCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, IBackgroundJobEnqueuer jobs, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _storage = storage; _jobs = jobs; _audit = audit;
    }

    public async Task<Result<LegacyImportBatchResponse>> Handle(CreateLegacyImportBatchCommand cmd, CancellationToken ct)
    {
        var ext = Path.GetExtension(cmd.FileName);
        if (!string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            return Result<LegacyImportBatchResponse>.Failure("LEGACY_IMPORT_INVALID_ZIP", "Chỉ chấp nhận file .zip");

        using var buffer = new MemoryStream();
        await cmd.ZipStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            return Result<LegacyImportBatchResponse>.Failure("LEGACY_IMPORT_INVALID_ZIP", "File ZIP rỗng, vui lòng thử lại");
        if (buffer.Length > MaxBytes)
            return Result<LegacyImportBatchResponse>.Failure("LEGACY_IMPORT_TOO_LARGE", "File ZIP vượt quá dung lượng tối đa 200MB");

        var batchId = Guid.NewGuid();
        var objectKey = $"batches/{_tenant.TenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{batchId}.zip";

        buffer.Position = 0;
        await _storage.UploadAsync(FileBuckets.LegacyScans, objectKey, buffer, cmd.ContentType, ct);

        var now = DateTime.UtcNow;
        using var conn = (IDbConnection)_db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_leg_import_batch
                (id, tenant_id, uploaded_by, zip_file_name, zip_object_key, total_items, processed_items, status, created_at, updated_at)
            VALUES
                (@Id, @TenantId, @UploadedBy, @ZipFileName, @ZipKey, 0, 0, 'pending', @Now, @Now)",
            new
            {
                Id = batchId.ToString(),
                TenantId = _tenant.TenantId,
                UploadedBy = _user.UserId?.ToString(),
                ZipFileName = cmd.FileName,
                ZipKey = objectKey,
                Now = now
            });

        _jobs.EnqueueLegacyOcrBatch(batchId.ToString(), _tenant.TenantId);

        await _audit.LogAsync("CREATE", "LegacyImportBatch", batchId.ToString(),
            new { zipFileName = cmd.FileName, sizeBytes = buffer.Length }, ct);

        return Result<LegacyImportBatchResponse>.Success(
            new LegacyImportBatchResponse(batchId, cmd.FileName, 0, 0, "pending", null, now, now));
    }
}

// ─────────────────────────────────────────────────
// LIST BATCH
// ─────────────────────────────────────────────────
public class ListLegacyImportBatchesQueryHandler : IRequestHandler<ListLegacyImportBatchesQuery, Result<PagedResult<LegacyImportBatchResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListLegacyImportBatchesQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<PagedResult<LegacyImportBatchResponse>>> Handle(ListLegacyImportBatchesQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : q.PageSize;
        var offset = (page - 1) * pageSize;

        var rows = await conn.QueryAsync(@"
            SELECT * FROM diab_his_leg_import_batch
            WHERE tenant_id=@TenantId AND deleted_at IS NULL
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset",
            new { TenantId = _tenant.TenantId, PageSize = pageSize, Offset = offset });

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_leg_import_batch WHERE tenant_id=@TenantId AND deleted_at IS NULL",
            new { TenantId = _tenant.TenantId });

        var items = new List<LegacyImportBatchResponse>();
        foreach (var r in rows) items.Add(LegacyImportMapper.MapBatch(r));
        return Result<PagedResult<LegacyImportBatchResponse>>.Success(new PagedResult<LegacyImportBatchResponse>(items, page, pageSize, total));
    }
}

// ─────────────────────────────────────────────────
// GET BATCH BY ID
// ─────────────────────────────────────────────────
public class GetLegacyImportBatchQueryHandler : IRequestHandler<GetLegacyImportBatchQuery, Result<LegacyImportBatchResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetLegacyImportBatchQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<LegacyImportBatchResponse>> Handle(GetLegacyImportBatchQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM diab_his_leg_import_batch WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = q.BatchId.ToString(), TenantId = _tenant.TenantId });
        if (row is null)
            return Result<LegacyImportBatchResponse>.Failure("LEGACY_IMPORT_BATCH_NOT_FOUND", "Không tìm thấy batch nhập liệu");

        return Result<LegacyImportBatchResponse>.Success(LegacyImportMapper.MapBatch(row));
    }
}

// ─────────────────────────────────────────────────
// LIST ITEMS trong 1 batch
// ─────────────────────────────────────────────────
public class ListLegacyImportItemsQueryHandler : IRequestHandler<ListLegacyImportItemsQuery, Result<PagedResult<LegacyImportItemResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public ListLegacyImportItemsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    { _db = db; _tenant = tenant; _storage = storage; }

    public async Task<Result<PagedResult<LegacyImportItemResponse>>> Handle(ListLegacyImportItemsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var batchExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_leg_import_batch WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = q.BatchId.ToString(), TenantId = _tenant.TenantId });
        if (batchExists == 0)
            return Result<PagedResult<LegacyImportItemResponse>>.Failure("LEGACY_IMPORT_BATCH_NOT_FOUND", "Không tìm thấy batch nhập liệu");

        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : q.PageSize;
        var offset = (page - 1) * pageSize;
        var hasStatus = !string.IsNullOrWhiteSpace(q.Status);

        var rows = await conn.QueryAsync($@"
            SELECT i.*, p.full_name AS patient_full_name, p.code AS patient_code
            FROM diab_his_leg_import_item i
            LEFT JOIN pat_patients p ON p.id = i.matched_patient_id AND p.tenant_id = i.tenant_id AND p.deleted_at IS NULL
            WHERE i.tenant_id=@TenantId AND i.batch_id=@BatchId AND i.deleted_at IS NULL
            {(hasStatus ? "AND i.status=@Status" : "")}
            ORDER BY i.created_at ASC
            LIMIT @PageSize OFFSET @Offset",
            new { TenantId = _tenant.TenantId, BatchId = q.BatchId.ToString(), Status = q.Status, PageSize = pageSize, Offset = offset });

        var total = await conn.ExecuteScalarAsync<int>($@"
            SELECT COUNT(*) FROM diab_his_leg_import_item i
            WHERE i.tenant_id=@TenantId AND i.batch_id=@BatchId AND i.deleted_at IS NULL
            {(hasStatus ? "AND i.status=@Status" : "")}",
            new { TenantId = _tenant.TenantId, BatchId = q.BatchId.ToString(), Status = q.Status });

        var items = new List<LegacyImportItemResponse>();
        foreach (var r in rows)
        {
            string? imageUrl = null;
            if (r.image_object_key is not null)
            {
                try { imageUrl = await _storage.GetSignedUrlAsync(FileBuckets.LegacyScans, (string)r.image_object_key, 900, ct); }
                catch { /* storage co the khong san sang trong test */ }
            }
            items.Add(LegacyImportMapper.MapItem(r, imageUrl, (string?)r.patient_full_name, (string?)r.patient_code));
        }

        return Result<PagedResult<LegacyImportItemResponse>>.Success(new PagedResult<LegacyImportItemResponse>(items, page, pageSize, total));
    }
}

// ─────────────────────────────────────────────────
// MATCH THU CONG — gan patient_id cho 1 item
// ─────────────────────────────────────────────────
public class MatchLegacyImportItemCommandHandler : IRequestHandler<MatchLegacyImportItemCommand, Result<LegacyImportItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public MatchLegacyImportItemCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage, IAuditService audit)
    { _db = db; _tenant = tenant; _storage = storage; _audit = audit; }

    public async Task<Result<LegacyImportItemResponse>> Handle(MatchLegacyImportItemCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var item = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM diab_his_leg_import_item WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });
        if (item is null)
            return Result<LegacyImportItemResponse>.Failure("LEGACY_IMPORT_ITEM_NOT_FOUND", "Không tìm thấy item nhập liệu");

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = cmd.PatientId.ToString(), TenantId = _tenant.TenantId });
        if (patientExists == 0)
            return Result<LegacyImportItemResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(@"
            UPDATE diab_his_leg_import_item
            SET matched_patient_id=@PatientId, match_method='manual', status='pending_review', updated_at=@Now
            WHERE id=@Id AND tenant_id=@TenantId",
            new { PatientId = cmd.PatientId.ToString(), Now = now, Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });

        await _audit.LogAsync("MATCH", "LegacyImportItem", cmd.ItemId.ToString(), new { patientId = cmd.PatientId }, ct);

        var row = await conn.QueryFirstOrDefaultAsync(@"
            SELECT i.*, p.full_name AS patient_full_name, p.code AS patient_code
            FROM diab_his_leg_import_item i
            LEFT JOIN pat_patients p ON p.id = i.matched_patient_id AND p.tenant_id = i.tenant_id AND p.deleted_at IS NULL
            WHERE i.id=@Id AND i.tenant_id=@TenantId",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });

        string? imageUrl = null;
        if (row!.image_object_key is not null)
        {
            try { imageUrl = await _storage.GetSignedUrlAsync(FileBuckets.LegacyScans, (string)row.image_object_key, 900, ct); }
            catch { }
        }

        return Result<LegacyImportItemResponse>.Success(
            LegacyImportMapper.MapItem(row, imageUrl, (string?)row.patient_full_name, (string?)row.patient_code));
    }
}

// ─────────────────────────────────────────────────
// CONFIRM — luu thanh tai lieu dinh kem ho so benh nhan (diab_his_fil_cls_uploads)
// ─────────────────────────────────────────────────
public class ConfirmLegacyImportItemCommandHandler : IRequestHandler<ConfirmLegacyImportItemCommand, Result<LegacyImportItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public ConfirmLegacyImportItemCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage storage, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _storage = storage; _audit = audit; }

    public async Task<Result<LegacyImportItemResponse>> Handle(ConfirmLegacyImportItemCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var item = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM diab_his_leg_import_item WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });
        if (item is null)
            return Result<LegacyImportItemResponse>.Failure("LEGACY_IMPORT_ITEM_NOT_FOUND", "Không tìm thấy item nhập liệu");

        if ((string)item.status == "confirmed")
            return Result<LegacyImportItemResponse>.Failure("LEGACY_IMPORT_ITEM_ALREADY_CONFIRMED", "Item này đã được xác nhận");

        var patientIdStr = cmd.PatientId?.ToString() ?? (string?)item.matched_patient_id;
        if (string.IsNullOrEmpty(patientIdStr))
            return Result<LegacyImportItemResponse>.Failure("ITEM_NOT_MATCHED", "Cần chọn bệnh nhân trước khi xác nhận");

        var patientExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = patientIdStr, TenantId = _tenant.TenantId });
        if (patientExists == 0)
            return Result<LegacyImportItemResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        if (item.image_object_key is null)
            return Result<LegacyImportItemResponse>.Failure("LEGACY_IMPORT_ITEM_NO_IMAGE", "Item chưa có ảnh scan để lưu");

        var imageObjectKey = (string)item.image_object_key;
        var fileName = (string?)item.original_filename ?? "legacy-scan.jpg";
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpeg" or ".jpg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var now = DateTime.UtcNow;
        var fileId = Guid.NewGuid();

        // Insert fil_files tro toi cung object_key da co san tren bucket legacy-scans (khong upload lai)
        await conn.ExecuteAsync(@"
            INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
            VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, NULL, 'LEGACY_SCAN', @UploadedBy, @Now, @Now)",
            new
            {
                Id = fileId.ToString(),
                TenantId = _tenant.TenantId,
                Bucket = FileBuckets.LegacyScans,
                Key = imageObjectKey,
                FileName = fileName,
                Mime = mime,
                UploadedBy = _user.UserId?.ToString(),
                Now = now
            });

        // GAP-9: cho phep phan loai tai lieu (don thuoc ngoai / giay chuyen vien / ho so cu),
        // whitelist + mac dinh HO_SO_CU_SCAN. KHONG tu tao don thuoc chinh thuc — chi luu dinh kem.
        var docType = LegacyImportDocTypes.Normalize(cmd.DocType);

        var clsUploadId = Guid.NewGuid();
        var ocrTextToSave = cmd.OcrText ?? (string?)item.ocr_text;
        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_fil_cls_uploads
                (id, tenant_id, patient_id, encounter_id, doc_type, file_id, file_path, file_name, mime_type, file_size_bytes, note, uploaded_by, uploaded_at, created_at, created_by, updated_at)
            VALUES
                (@Id, @TenantId, @PatId, NULL, @DocType, @FileId, @FilePath, @FileName, @Mime, NULL, @Note, @UploadedBy, @Now, @Now, @UploadedBy, @Now)",
            new
            {
                Id = clsUploadId.ToString(),
                TenantId = _tenant.TenantId,
                PatId = patientIdStr,
                DocType = docType,
                FileId = fileId.ToString(),
                FilePath = imageObjectKey,
                FileName = fileName,
                Mime = mime,
                Note = ocrTextToSave,
                UploadedBy = _user.UserId?.ToString(),
                Now = now
            });

        await conn.ExecuteAsync(@"
            UPDATE diab_his_leg_import_item
            SET status='confirmed', matched_patient_id=@PatientId, ocr_text=@OcrText,
                saved_cls_upload_id=@ClsUploadId, confirmed_by=@ConfirmedBy, confirmed_at=@Now, updated_at=@Now
            WHERE id=@Id AND tenant_id=@TenantId",
            new
            {
                PatientId = patientIdStr,
                OcrText = ocrTextToSave,
                ClsUploadId = clsUploadId.ToString(),
                ConfirmedBy = _user.UserId?.ToString(),
                Now = now,
                Id = cmd.ItemId.ToString(),
                TenantId = _tenant.TenantId
            });

        await _audit.LogAsync("CONFIRM", "LegacyImportItem", cmd.ItemId.ToString(),
            new { patientId = patientIdStr, clsUploadId, docType }, ct);

        var row = await conn.QueryFirstOrDefaultAsync(@"
            SELECT i.*, p.full_name AS patient_full_name, p.code AS patient_code
            FROM diab_his_leg_import_item i
            LEFT JOIN pat_patients p ON p.id = i.matched_patient_id AND p.tenant_id = i.tenant_id AND p.deleted_at IS NULL
            WHERE i.id=@Id AND i.tenant_id=@TenantId",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });

        string? imageUrl = null;
        try { imageUrl = await _storage.GetSignedUrlAsync(FileBuckets.LegacyScans, imageObjectKey, 900, ct); }
        catch { }

        LegacyImportItemResponse mapped = LegacyImportMapper.MapItem(row, imageUrl, (string?)row!.patient_full_name, (string?)row.patient_code);
        return Result<LegacyImportItemResponse>.Success(mapped with { DocType = docType });
    }
}

// ─────────────────────────────────────────────────
// REJECT
// ─────────────────────────────────────────────────
public class RejectLegacyImportItemCommandHandler : IRequestHandler<RejectLegacyImportItemCommand, Result<LegacyImportItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public RejectLegacyImportItemCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage, IAuditService audit)
    { _db = db; _tenant = tenant; _storage = storage; _audit = audit; }

    public async Task<Result<LegacyImportItemResponse>> Handle(RejectLegacyImportItemCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var item = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM diab_his_leg_import_item WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });
        if (item is null)
            return Result<LegacyImportItemResponse>.Failure("LEGACY_IMPORT_ITEM_NOT_FOUND", "Không tìm thấy item nhập liệu");

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            "UPDATE diab_his_leg_import_item SET status='rejected', updated_at=@Now WHERE id=@Id AND tenant_id=@TenantId",
            new { Now = now, Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });

        await _audit.LogAsync("REJECT", "LegacyImportItem", cmd.ItemId.ToString(), null, ct);

        var row = await conn.QueryFirstOrDefaultAsync(@"
            SELECT i.*, p.full_name AS patient_full_name, p.code AS patient_code
            FROM diab_his_leg_import_item i
            LEFT JOIN pat_patients p ON p.id = i.matched_patient_id AND p.tenant_id = i.tenant_id AND p.deleted_at IS NULL
            WHERE i.id=@Id AND i.tenant_id=@TenantId",
            new { Id = cmd.ItemId.ToString(), TenantId = _tenant.TenantId });

        string? imageUrl = null;
        if (row!.image_object_key is not null)
        {
            try { imageUrl = await _storage.GetSignedUrlAsync(FileBuckets.LegacyScans, (string)row.image_object_key, 900, ct); }
            catch { }
        }

        return Result<LegacyImportItemResponse>.Success(
            LegacyImportMapper.MapItem(row, imageUrl, (string?)row.patient_full_name, (string?)row.patient_code));
    }
}
