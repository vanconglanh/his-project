using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Files;

public class ListFileAnnotationsQueryHandler : IRequestHandler<ListFileAnnotationsQuery, Result<List<FileAnnotationResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListFileAnnotationsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<List<FileAnnotationResponse>>> Handle(ListFileAnnotationsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var fileExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM fil_files WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = request.FileId.ToString(), TenantId = _tenant.TenantId });
        if (fileExists == 0)
            return Result<List<FileAnnotationResponse>>.Failure("FILE_NOT_FOUND", "Không tìm thấy tệp");

        var sql = @"
            SELECT a.id, a.file_id, a.patient_id, a.encounter_id, a.annotation_data, a.version,
                   a.created_at, a.created_by, u.full_name AS created_by_name, a.updated_at, a.updated_by
            FROM diab_his_fil_file_annotations a
            LEFT JOIN sec_users u ON a.created_by = u.id
            WHERE a.file_id=@FileId AND a.tenant_id=@TenantId AND a.deleted_at IS NULL
            ORDER BY a.created_at ASC";

        var rows = await conn.QueryAsync(sql, new { FileId = request.FileId.ToString(), TenantId = _tenant.TenantId });
        var items = rows.Select(MapRow).ToList();

        return Result<List<FileAnnotationResponse>>.Success(items);
    }

    internal static FileAnnotationResponse MapRow(dynamic r)
    {
        return new FileAnnotationResponse(
            Guid.Parse(((object)r.id).ToString()!),
            Guid.Parse(((object)r.file_id).ToString()!),
            r.patient_id is not null ? Guid.Parse(((object)r.patient_id).ToString()!) : null,
            r.encounter_id is not null ? Guid.Parse(((object)r.encounter_id).ToString()!) : null,
            (string)r.annotation_data,
            (int)r.version,
            (DateTime)r.created_at,
            r.created_by is not null ? Guid.Parse(((object)r.created_by).ToString()!) : null,
            (string?)r.created_by_name,
            (DateTime)r.updated_at,
            r.updated_by is not null ? Guid.Parse(((object)r.updated_by).ToString()!) : null);
    }
}

public class CreateFileAnnotationCommandHandler : IRequestHandler<CreateFileAnnotationCommand, Result<FileAnnotationResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CreateFileAnnotationCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<FileAnnotationResponse>> Handle(CreateFileAnnotationCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var fileExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM fil_files WHERE id=@Id AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.FileId.ToString(), TenantId = _tenant.TenantId });
        if (fileExists == 0)
            return Result<FileAnnotationResponse>.Failure("FILE_NOT_FOUND", "Không tìm thấy tệp");

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId?.ToString();

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_fil_file_annotations
                (id, tenant_id, file_id, patient_id, encounter_id, annotation_data, version, created_at, created_by, updated_at, updated_by)
            VALUES
                (@Id, @TenantId, @FileId, @PatientId, @EncounterId, @Data, 1, @Now, @UserId, @Now, @UserId)",
            new
            {
                Id = id.ToString(),
                TenantId = _tenant.TenantId,
                FileId = command.FileId.ToString(),
                PatientId = command.PatientId?.ToString(),
                EncounterId = command.EncounterId?.ToString(),
                Data = command.AnnotationData,
                Now = now,
                UserId = userId
            });

        await _audit.LogAsync("CREATE", "FileAnnotations", id.ToString(), new { fileId = command.FileId }, cancellationToken);

        var uploaderName = await conn.ExecuteScalarAsync<string>(
            "SELECT full_name FROM sec_users WHERE id=@Id", new { Id = userId });

        return Result<FileAnnotationResponse>.Success(new FileAnnotationResponse(
            id, command.FileId, command.PatientId, command.EncounterId, command.AnnotationData, 1,
            now, _currentUser.UserId, uploaderName, now, _currentUser.UserId));
    }
}

public class UpdateFileAnnotationCommandHandler : IRequestHandler<UpdateFileAnnotationCommand, Result<FileAnnotationResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public UpdateFileAnnotationCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<FileAnnotationResponse>> Handle(UpdateFileAnnotationCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT id FROM diab_his_fil_file_annotations WHERE id=@Id AND file_id=@FileId AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.Id.ToString(), FileId = command.FileId.ToString(), TenantId = _tenant.TenantId });
        if (row is null)
            return Result<FileAnnotationResponse>.Failure("FILE_ANNOTATION_NOT_FOUND", "Không tìm thấy annotation");

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId?.ToString();

        await conn.ExecuteAsync(@"
            UPDATE diab_his_fil_file_annotations
            SET annotation_data=@Data, version = version + 1, updated_at=@Now, updated_by=@UserId
            WHERE id=@Id AND tenant_id=@TenantId",
            new { Data = command.AnnotationData, Now = now, UserId = userId, Id = command.Id.ToString(), TenantId = _tenant.TenantId });

        await _audit.LogAsync("UPDATE", "FileAnnotations", command.Id.ToString(), new { fileId = command.FileId }, cancellationToken);

        var updated = await conn.QueryFirstOrDefaultAsync(@"
            SELECT a.id, a.file_id, a.patient_id, a.encounter_id, a.annotation_data, a.version,
                   a.created_at, a.created_by, u.full_name AS created_by_name, a.updated_at, a.updated_by
            FROM diab_his_fil_file_annotations a
            LEFT JOIN sec_users u ON a.created_by = u.id
            WHERE a.id=@Id AND a.tenant_id=@TenantId",
            new { Id = command.Id.ToString(), TenantId = _tenant.TenantId });

        return Result<FileAnnotationResponse>.Success(ListFileAnnotationsQueryHandler.MapRow(updated!));
    }
}

public class DeleteFileAnnotationCommandHandler : IRequestHandler<DeleteFileAnnotationCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public DeleteFileAnnotationCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<Result<bool>> Handle(DeleteFileAnnotationCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT id FROM diab_his_fil_file_annotations WHERE id=@Id AND file_id=@FileId AND tenant_id=@TenantId AND deleted_at IS NULL",
            new { Id = command.Id.ToString(), FileId = command.FileId.ToString(), TenantId = _tenant.TenantId });
        if (row is null)
            return Result<bool>.Failure("FILE_ANNOTATION_NOT_FOUND", "Không tìm thấy annotation");

        await conn.ExecuteAsync(
            "UPDATE diab_his_fil_file_annotations SET deleted_at=@Now WHERE id=@Id AND tenant_id=@TenantId",
            new { Now = DateTime.UtcNow, Id = command.Id.ToString(), TenantId = _tenant.TenantId });

        await _audit.LogAsync("DELETE", "FileAnnotations", command.Id.ToString(), new { fileId = command.FileId }, cancellationToken);

        return Result<bool>.Success(true);
    }
}
