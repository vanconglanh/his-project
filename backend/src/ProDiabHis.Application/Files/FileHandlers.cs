using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Files;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _storage;
    private const long MaxBytes = 20L * 1024 * 1024;

    public UploadFileCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser currentUser, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<FileUploadResponse>> Handle(UploadFileCommand command, CancellationToken cancellationToken)
    {
        if (command.SizeBytes > MaxBytes)
            return Result<FileUploadResponse>.Failure("FILE_UPLOAD_FAILED", "File vượt quá dung lượng tối đa 20MB");

        var id = Guid.NewGuid();
        var ext = Path.GetExtension(command.FileName);
        var objectKey = $"generic/{_tenant.TenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{id}{ext}";

        await _storage.UploadAsync(FileBuckets.FilesGeneric, objectKey, command.FileStream, command.ContentType, cancellationToken);
        var signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.FilesGeneric, objectKey, 900, cancellationToken);
        var expiresAt = DateTime.UtcNow.AddSeconds(900);

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
            VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, @Size, @Category, @UploadedBy, @Now, @Now)",
            new
            {
                Id = id.ToString(),
                TenantId = _tenant.TenantId,
                Bucket = FileBuckets.FilesGeneric,
                Key = objectKey,
                FileName = command.FileName,
                Mime = command.ContentType,
                Size = command.SizeBytes,
                Category = command.Category,
                UploadedBy = _currentUser.UserId?.ToString(),
                Now = DateTime.UtcNow
            });

        return Result<FileUploadResponse>.Success(new FileUploadResponse(id, command.FileName, command.ContentType, command.SizeBytes, signedUrl, expiresAt));
    }
}

public class GetSignedUrlQueryHandler : IRequestHandler<GetSignedUrlQuery, Result<FileUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public GetSignedUrlQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<Result<FileUploadResponse>> Handle(GetSignedUrlQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT id, bucket, object_key, file_name, mime_type, file_size_bytes FROM fil_files WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = request.FileId.ToString(), TenantId = _tenant.TenantId });

        if (row is null)
            return Result<FileUploadResponse>.Failure("FILE_NOT_FOUND", "Không tìm thấy tệp");

        var signedUrl = await _storage.GetSignedUrlAsync((string)row.bucket, (string)row.object_key, 900, cancellationToken);
        var expiresAt = DateTime.UtcNow.AddSeconds(900);

        return Result<FileUploadResponse>.Success(new FileUploadResponse(
            request.FileId, (string)row.file_name, (string?)row.mime_type,
            row.file_size_bytes is not null ? (long?)row.file_size_bytes : null,
            signedUrl, expiresAt));
    }
}

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public DeleteFileCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<Result<bool>> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT bucket, object_key FROM fil_files WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.FileId.ToString(), TenantId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("FILE_NOT_FOUND", "Không tìm thấy tệp");

        await _storage.DeleteAsync((string)row.bucket, (string)row.object_key, cancellationToken);
        await conn.ExecuteAsync(
            "UPDATE fil_files SET deleted_at=@Now WHERE id=@Id",
            new { Id = command.FileId.ToString(), Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

public class UploadClsCommandHandler : IRequestHandler<UploadClsCommand, Result<ClsUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _storage;
    private const long MaxBytes = 10L * 1024 * 1024;

    private static readonly string[] AllowedMimes = { "image/jpeg", "image/png", "application/pdf" };

    public UploadClsCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser currentUser, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<ClsUploadResponse>> Handle(UploadClsCommand command, CancellationToken cancellationToken)
    {
        if (command.SizeBytes > MaxBytes)
            return Result<ClsUploadResponse>.Failure("CLS_UPLOAD_TOO_LARGE", "Tài liệu vượt quá dung lượng tối đa 10MB");

        if (!AllowedMimes.Contains(command.ContentType))
            return Result<ClsUploadResponse>.Failure("CLS_UPLOAD_INVALID_FORMAT", "Chỉ chấp nhận file PNG/JPEG/PDF");

        using var conn = _db.CreateConnection();
        var intPatId = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM pat_patients WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.PatientId.ToString(), TenantId = _tenant.TenantId });
        if (intPatId is null)
            return Result<ClsUploadResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var fileId = Guid.NewGuid();
        var ext = Path.GetExtension(command.FileName);
        var objectKey = $"cls/{_tenant.TenantId}/{command.PatientId}/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{ext}";

        await _storage.UploadAsync(FileBuckets.ClsUploads, objectKey, command.FileStream, command.ContentType, cancellationToken);
        var signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.ClsUploads, objectKey, 900, cancellationToken);

        // Insert file meta
        await conn.ExecuteAsync(@"
            INSERT INTO fil_files (id, tenant_id, bucket, object_key, file_name, mime_type, file_size_bytes, category, uploaded_by, created_at, updated_at)
            VALUES (@Id, @TenantId, @Bucket, @Key, @FileName, @Mime, @Size, 'CLS', @UploadedBy, @Now, @Now)",
            new
            {
                Id = fileId.ToString(),
                TenantId = _tenant.TenantId,
                Bucket = FileBuckets.ClsUploads,
                Key = objectKey,
                FileName = command.FileName,
                Mime = command.ContentType,
                Size = command.SizeBytes,
                UploadedBy = _currentUser.UserId?.ToString(),
                Now = DateTime.UtcNow
            });

        var clsId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_fil_cls_uploads
                (id, tenant_id, patient_id, encounter_id, doc_type, file_id, file_path, file_name, mime_type, file_size_bytes, note, uploaded_by, uploaded_at, created_at, created_by, updated_at)
            VALUES
                (@Id, @TenantId, @PatId, @EncId, @DocType, @FileId, @FilePath, @FileName, @Mime, @Size, @Note, @UploadedBy, @Now, @Now, @UploadedBy, @Now)",
            new
            {
                Id = clsId,
                TenantId = _tenant.TenantId,
                PatId = intPatId.Value,
                EncId = command.EncounterId?.ToString(),
                DocType = command.DocType,
                FileId = fileId.ToString(),
                FilePath = objectKey,
                FileName = command.FileName,
                Mime = command.ContentType,
                Size = command.SizeBytes,
                Note = command.Note,
                UploadedBy = _currentUser.UserId?.ToString(),
                Now = now
            });

        // Get uploader name
        var uploaderName = await conn.ExecuteScalarAsync<string>(
            "SELECT full_name FROM sec_users WHERE id=@Id", new { Id = _currentUser.UserId?.ToString() });

        return Result<ClsUploadResponse>.Success(new ClsUploadResponse(
            Guid.Parse(clsId),
            command.PatientId,
            command.EncounterId,
            command.DocType,
            fileId,
            command.FileName,
            command.SizeBytes,
            command.ContentType,
            signedUrl,
            now,
            _currentUser.UserId,
            uploaderName,
            command.Note));
    }
}

public class ListClsUploadsQueryHandler : IRequestHandler<ListClsUploadsQuery, PagedResult<ClsUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public ListClsUploadsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<PagedResult<ClsUploadResponse>> Handle(ListClsUploadsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var offset = (request.Page - 1) * request.PageSize;

        var where = "WHERE c.tenant_id=@TenantId AND c.patient_id=(SELECT id FROM pat_patients WHERE id=@PatientId AND tenant_id=@TenantId) AND c.deleted_at IS NULL";
        if (!string.IsNullOrEmpty(request.DocType)) where += " AND c.doc_type LIKE @DocType";

        var sql = $@"
            SELECT c.id, c.patient_id, c.encounter_id, c.doc_type, c.file_id,
                   c.file_name, c.file_size_bytes, c.mime_type, c.file_path,
                   c.uploaded_at, c.uploaded_by, u.full_name as uploader_name, c.note
            FROM diab_his_fil_cls_uploads c
            LEFT JOIN sec_users u ON c.uploaded_by = u.id
            {where}
            ORDER BY c.uploaded_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var countSql = $"SELECT COUNT(*) FROM diab_his_fil_cls_uploads c {where}";

        var rows = await conn.QueryAsync(sql, new
        {
            TenantId = _tenant.TenantId,
            PatientId = request.PatientId.ToString(),
            DocType = $"%{request.DocType}%",
            PageSize = request.PageSize,
            Offset = offset
        });
        var total = await conn.ExecuteScalarAsync<int>(countSql, new { TenantId = _tenant.TenantId, PatientId = request.PatientId.ToString(), DocType = $"%{request.DocType}%" });

        var items = new List<ClsUploadResponse>();
        foreach (var r in rows)
        {
            string? signedUrl = null;
            try { signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.ClsUploads, (string)r.file_path, 900, cancellationToken); }
            catch { /* MinIO may not be available in test */ }

            items.Add(MapClsRow(r, signedUrl));
        }

        return new PagedResult<ClsUploadResponse>(items, request.Page, request.PageSize, total);
    }

    internal static ClsUploadResponse MapClsRow(dynamic r, string? signedUrl)
    {
        return new ClsUploadResponse(
            Guid.Parse(((object)r.id).ToString()!),
            Guid.Parse(((object)r.patient_id).ToString()!),
            r.encounter_id is not null ? Guid.Parse(((object)r.encounter_id).ToString()!) : null,
            (string)r.doc_type,
            r.file_id is not null ? Guid.Parse(((object)r.file_id).ToString()!) : null,
            (string)r.file_name,
            r.file_size_bytes is not null ? (long?)r.file_size_bytes : null,
            (string?)r.mime_type,
            signedUrl,
            (DateTime)r.uploaded_at,
            r.uploaded_by is not null ? Guid.Parse(((object)r.uploaded_by).ToString()!) : null,
            (string?)r.uploader_name,
            (string?)r.note);
    }
}

public class GetClsUploadQueryHandler : IRequestHandler<GetClsUploadQuery, Result<ClsUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public GetClsUploadQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<Result<ClsUploadResponse>> Handle(GetClsUploadQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT c.id, c.patient_id, c.encounter_id, c.doc_type, c.file_id,
                   c.file_name, c.file_size_bytes, c.mime_type, c.file_path,
                   c.uploaded_at, c.uploaded_by, u.full_name as uploader_name, c.note
            FROM diab_his_fil_cls_uploads c
            LEFT JOIN sec_users u ON c.uploaded_by = u.id
            WHERE c.id=@Id AND c.tenant_id=@TenantId AND c.deleted_at IS NULL";

        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = request.Id.ToString(), TenantId = _tenant.TenantId });
        if (row is null)
            return Result<ClsUploadResponse>.Failure("CLS_UPLOAD_NOT_FOUND", "Không tìm thấy tài liệu CLS");

        string? signedUrl = null;
        try { signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.ClsUploads, (string)row.file_path, 900, cancellationToken); }
        catch { /* MinIO may not be available */ }

        return Result<ClsUploadResponse>.Success(ListClsUploadsQueryHandler.MapClsRow(row, signedUrl));
    }
}

public class DeleteClsUploadCommandHandler : IRequestHandler<DeleteClsUploadCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public DeleteClsUploadCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<Result<bool>> Handle(DeleteClsUploadCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT file_path FROM diab_his_fil_cls_uploads WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.Id.ToString(), TenantId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("CLS_UPLOAD_NOT_FOUND", "Không tìm thấy tài liệu CLS");

        try { await _storage.DeleteAsync(FileBuckets.ClsUploads, (string)row.file_path, cancellationToken); }
        catch { /* log only */ }

        await conn.ExecuteAsync(
            "UPDATE diab_his_fil_cls_uploads SET deleted_at=@Now WHERE id=@Id",
            new { Id = command.Id.ToString(), Now = DateTime.UtcNow });

        return Result<bool>.Success(true);
    }
}

public class ListEncounterClsUploadsQueryHandler : IRequestHandler<ListEncounterClsUploadsQuery, List<ClsUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _storage;

    public ListEncounterClsUploadsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IFileStorage storage)
    {
        _db = db;
        _tenant = tenant;
        _storage = storage;
    }

    public async Task<List<ClsUploadResponse>> Handle(ListEncounterClsUploadsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT c.id, c.patient_id, c.encounter_id, c.doc_type, c.file_id,
                   c.file_name, c.file_size_bytes, c.mime_type, c.file_path,
                   c.uploaded_at, c.uploaded_by, u.full_name as uploader_name, c.note
            FROM diab_his_fil_cls_uploads c
            LEFT JOIN sec_users u ON c.uploaded_by = u.id
            WHERE c.encounter_id=@EncId AND c.tenant_id=@TenantId AND c.deleted_at IS NULL
            ORDER BY c.uploaded_at ASC";

        var rows = await conn.QueryAsync(sql, new { EncId = request.EncounterId.ToString(), TenantId = _tenant.TenantId });

        var items = new List<ClsUploadResponse>();
        foreach (var r in rows)
        {
            string? signedUrl = null;
            try { signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.ClsUploads, (string)r.file_path, 900, cancellationToken); }
            catch { }
            items.Add(ListClsUploadsQueryHandler.MapClsRow(r, signedUrl));
        }
        return items;
    }
}
