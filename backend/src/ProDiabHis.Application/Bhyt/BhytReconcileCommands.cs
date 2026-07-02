using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Bhyt;

// ── Import Reconcile File ─────────────────────────────────────────────────────

/// <summary>
/// Command upload file ket qua doi soat.
/// FileStream = null khi dung xml_file_path da co tren MinIO.
/// </summary>
public record ImportReconcileFileCommand(
    int ExportId,
    Stream? FileStream,
    string? OriginalFileName,
    long FileSize,
    string? XmlFilePath) : IRequest<Result<ReconcileUploadResponse>>;

public class ImportReconcileFileHandler : IRequestHandler<ImportReconcileFileCommand, Result<ReconcileUploadResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _fileStorage;
    private readonly IBackgroundJobEnqueuer _jobs;

    public ImportReconcileFileHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IFileStorage fileStorage, IBackgroundJobEnqueuer jobs)
    {
        _db = db; _tenant = tenant; _user = user; _fileStorage = fileStorage; _jobs = jobs;
    }

    public async Task<Result<ReconcileUploadResponse>> Handle(ImportReconcileFileCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var export = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (export == null)
            return Result<ReconcileUploadResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        string filePath;
        long fileSize = cmd.FileSize;

        if (cmd.FileStream != null)
        {
            var objectName = $"bhyt/reconcile/{_tenant.TenantId}/{cmd.ExportId}/{Guid.NewGuid()}.xml";
            await _fileStorage.UploadAsync("prodiab-his", objectName, cmd.FileStream, "application/xml", ct);
            filePath = objectName;
        }
        else if (!string.IsNullOrEmpty(cmd.XmlFilePath))
        {
            filePath = cmd.XmlFilePath;
        }
        else
        {
            return Result<ReconcileUploadResponse>.Failure("BHYT_RECONCILE_FILE_INVALID", "Phai cung cap file hoac xml_file_path");
        }

        var uploadId = Guid.NewGuid().ToString();
        var userId = _user.UserId?.ToString();

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_int_bhyt_reconcile_uploads
              (id, tenant_id, export_id, file_path, file_size, uploaded_at, parse_status, created_at, created_by, updated_at)
              VALUES (@id, @t, @eid, @fp, @fs, NOW(), 'PENDING', NOW(), @cb, NOW())",
            new { id = uploadId, t = _tenant.TenantId, eid = cmd.ExportId, fp = filePath, fs = fileSize, cb = userId });

        _jobs.EnqueueBhytReconcileParse(uploadId, cmd.ExportId, _tenant.TenantId, filePath);

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_reconcile_uploads WHERE id=@id", new { id = uploadId });

        return Result<ReconcileUploadResponse>.Success(MapUpload(row));
    }

    internal static ReconcileUploadResponse MapUpload(dynamic row) => new(
        Id: Guid.Parse((string)row.id),
        TenantId: (int)row.tenant_id,
        ExportId: (int)row.export_id,
        FilePath: (string)row.file_path,
        UploadedAt: (DateTime)row.uploaded_at,
        ParsedAt: (DateTime?)row.parsed_at,
        ParseStatus: (string)row.parse_status,
        ParseError: (string?)row.parse_error,
        CreatedAt: (DateTime)row.created_at,
        CreatedBy: row.created_by is string cb && Guid.TryParse(cb, out var cg2) ? cg2 : (Guid?)null);
}

// ── Dispute Item ──────────────────────────────────────────────────────────────

public record DisputeReconcileItemCommand(Guid ItemId, DisputeReconcileItemRequest Request)
    : IRequest<Result<ReconcileItemResponse>>;

public class DisputeReconcileItemHandler : IRequestHandler<DisputeReconcileItemCommand, Result<ReconcileItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public DisputeReconcileItemHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<ReconcileItemResponse>> Handle(DisputeReconcileItemCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_reconcile_items WHERE id=@id AND tenant_id=@t",
            new { id = cmd.ItemId.ToString(), t = _tenant.TenantId });

        if (row == null)
            return Result<ReconcileItemResponse>.Failure("BHYT_RECONCILE_ITEM_NOT_FOUND", "Khong tim thay item doi soat");

        await conn.ExecuteAsync(
            @"UPDATE diab_his_int_bhyt_reconcile_items
              SET status='DISPUTED', dispute_reason=@r, dispute_evidence_path=@ep,
                  disputed_at=NOW(), disputed_by=@db, updated_at=NOW()
              WHERE id=@id",
            new { id = cmd.ItemId.ToString(), r = cmd.Request.Reason,
                  ep = cmd.Request.EvidenceFilePath, db = _user.UserId?.ToString() });

        var updated = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_reconcile_items WHERE id=@id", new { id = cmd.ItemId.ToString() });

        return Result<ReconcileItemResponse>.Success(MapReconcileItem(updated));
    }

    internal static ReconcileItemResponse MapReconcileItem(dynamic row) => new(
        Id: Guid.Parse((string)row.id),
        ExportId: (int)row.export_id,
        ExportItemId: (int?)row.export_item_id,
        TableNo: (int)row.table_no,
        MaLienKet: (string)row.ma_lien_ket,
        RequestAmount: (decimal)row.request_amount,
        ApprovedAmount: (decimal)row.approved_amount,
        RejectedAmount: (decimal)row.rejected_amount,
        RejectionCode: (string?)row.rejection_code,
        RejectionReason: (string?)row.rejection_reason,
        Status: (string)row.status,
        DisputeReason: (string?)row.dispute_reason,
        DisputeEvidencePath: (string?)row.dispute_evidence_path,
        UpdatedAt: (DateTime)row.updated_at);
}

// ── Accept Item ───────────────────────────────────────────────────────────────

public record AcceptReconcileItemCommand(Guid ItemId, AcceptReconcileItemRequest? Request)
    : IRequest<Result<ReconcileItemResponse>>;

public class AcceptReconcileItemHandler : IRequestHandler<AcceptReconcileItemCommand, Result<ReconcileItemResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public AcceptReconcileItemHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<ReconcileItemResponse>> Handle(AcceptReconcileItemCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_reconcile_items WHERE id=@id AND tenant_id=@t",
            new { id = cmd.ItemId.ToString(), t = _tenant.TenantId });

        if (row == null)
            return Result<ReconcileItemResponse>.Failure("BHYT_RECONCILE_ITEM_NOT_FOUND", "Khong tim thay item doi soat");

        await conn.ExecuteAsync(
            @"UPDATE diab_his_int_bhyt_reconcile_items
              SET status='ACCEPTED', note=@n, accepted_at=NOW(), accepted_by=@ab, updated_at=NOW()
              WHERE id=@id",
            new { id = cmd.ItemId.ToString(), n = cmd.Request?.Note, ab = _user.UserId?.ToString() });

        var updated = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_reconcile_items WHERE id=@id", new { id = cmd.ItemId.ToString() });

        return Result<ReconcileItemResponse>.Success(DisputeReconcileItemHandler.MapReconcileItem(updated));
    }
}
