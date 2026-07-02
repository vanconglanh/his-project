using System.Data;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities.Bhyt;

namespace ProDiabHis.Application.Bhyt;

// ── Create Export ─────────────────────────────────────────────────────────────

public record CreateBhytExportCommand(CreateBhytExportRequest Request) : IRequest<Result<BhytExportResponse>>;

public class CreateBhytExportHandler : IRequestHandler<CreateBhytExportCommand, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public CreateBhytExportHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db; _tenant = tenant; _user = user;
    }

    public async Task<Result<BhytExportResponse>> Handle(CreateBhytExportCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        if (!System.Text.RegularExpressions.Regex.IsMatch(req.PeriodMonth, @"^\d{4}-\d{2}$"))
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_INVALID_PERIOD", "period_month phai theo dinh dang YYYY-MM");

        using var conn = (IDbConnection)_db.CreateConnection();

        var existing = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT id FROM diab_his_int_bhyt_exports WHERE tenant_id=@t AND period_month=@p AND deleted_at IS NULL",
            new { t = _tenant.TenantId, p = req.PeriodMonth });

        if (existing != 0)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_CONFLICT",
                $"Ky {req.PeriodMonth} da co export dang xu ly (id={existing})");

        var scopeJson = req.ScopeFilter?.ToJsonString();
        var userId = _user.UserId?.ToString();

        var id = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO diab_his_int_bhyt_exports
              (tenant_id, period_month, scope_filter_json, note, status, encounter_count,
               total_requested_amount, total_approved_amount, total_rejected_amount,
               created_at, created_by, updated_at, updated_by)
              VALUES (@t, @p, @sf, @n, 'DRAFT', 0, 0, 0, 0, NOW(), @cb, NOW(), @cb);
              SELECT LAST_INSERT_ID();",
            new { t = _tenant.TenantId, p = req.PeriodMonth, sf = scopeJson, n = req.Note, cb = userId });

        var row = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id", new { id });

        return Result<BhytExportResponse>.Success(MapToResponse(row));
    }

    private static Guid? ParseGuid(string? s) =>
        s != null && Guid.TryParse(s, out var g) ? g : (Guid?)null;

    internal static BhytExportResponse MapToResponse(dynamic row)
    {
        return new BhytExportResponse(
            Id: (int)row.id,
            TenantId: (int)row.tenant_id,
            PeriodMonth: (string)row.period_month,
            ScopeFilterJson: row.scope_filter_json != null
                ? JsonSerializer.Deserialize<object>((string)row.scope_filter_json)
                : null,
            Status: (string)row.status,
            EncounterCount: (int)(row.encounter_count ?? 0),
            TotalRequestedAmount: (decimal)(row.total_requested_amount ?? 0m),
            TotalApprovedAmount: (decimal)(row.total_approved_amount ?? 0m),
            TotalRejectedAmount: (decimal)(row.total_rejected_amount ?? 0m),
            GeneratedAt: (DateTime?)row.generated_at,
            ValidatedAt: (DateTime?)row.validated_at,
            SignedAt: (DateTime?)row.signed_at,
            SubmittedAt: (DateTime?)row.submitted_at,
            ResponseAt: (DateTime?)row.response_at,
            ResponseMessage: (string?)row.response_message,
            XmlFilePath: (string?)row.xml_file_path,
            BhytReference: (string?)row.bhyt_reference,
            CreatedAt: (DateTime)row.created_at,
            CreatedBy: ParseGuid((string?)row.created_by),
            UpdatedAt: (DateTime)row.updated_at,
            UpdatedBy: ParseGuid((string?)row.updated_by));
    }
}

// ── Delete Export ─────────────────────────────────────────────────────────────

public record DeleteBhytExportCommand(int Id) : IRequest<Result>;

public class DeleteBhytExportHandler : IRequestHandler<DeleteBhytExportCommand, Result>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public DeleteBhytExportHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result> Handle(DeleteBhytExportCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.Id, t = _tenant.TenantId });

        if (row == null)
            return Result.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        string status = (string)row.status;
        if (BhytExportStatus.IsLocked(status))
            return Result.Failure("BHYT_PERIOD_LOCKED", "Ky export da SUBMITTED, khong the xoa");

        if (status != BhytExportStatus.Draft)
            return Result.Failure("BHYT_PERIOD_LOCKED", "Chi co the xoa ky export o trang thai DRAFT");

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_bhyt_exports SET deleted_at=NOW(), updated_at=NOW() WHERE id=@id",
            new { id = cmd.Id });

        return Result.Success();
    }
}

// ── Generate XML ──────────────────────────────────────────────────────────────

public record GenerateBhytXmlCommand(int ExportId) : IRequest<Result<BhytExportResponse>>;

public class GenerateBhytXmlHandler : IRequestHandler<GenerateBhytXmlCommand, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBackgroundJobEnqueuer _jobs;
    private readonly ILogger<GenerateBhytXmlHandler> _logger;

    public GenerateBhytXmlHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        IBackgroundJobEnqueuer jobs, ILogger<GenerateBhytXmlHandler> logger)
    {
        _db = db; _tenant = tenant; _jobs = jobs; _logger = logger;
    }

    public async Task<Result<BhytExportResponse>> Handle(GenerateBhytXmlCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        string status = (string)row.status;
        if (BhytExportStatus.IsLocked(status))
            return Result<BhytExportResponse>.Failure("BHYT_PERIOD_LOCKED", "Ky export da bi khoa, khong the generate lai");

        _jobs.EnqueueBhytGenerateXml(cmd.ExportId, _tenant.TenantId, (string)row.period_month, (string?)row.scope_filter_json);
        _logger.LogInformation("BhytGenerateXml job enqueued for exportId={Id}", cmd.ExportId);

        return Result<BhytExportResponse>.Success(CreateBhytExportHandler.MapToResponse(row));
    }
}

// ── Regenerate XML ────────────────────────────────────────────────────────────

public record RegenerateBhytXmlCommand(int ExportId) : IRequest<Result<BhytExportResponse>>;

public class RegenerateBhytXmlHandler : IRequestHandler<RegenerateBhytXmlCommand, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBackgroundJobEnqueuer _jobs;

    public RegenerateBhytXmlHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBackgroundJobEnqueuer jobs)
    {
        _db = db; _tenant = tenant; _jobs = jobs;
    }

    public async Task<Result<BhytExportResponse>> Handle(RegenerateBhytXmlCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        if (BhytExportStatus.IsLocked((string)row.status))
            return Result<BhytExportResponse>.Failure("BHYT_PERIOD_LOCKED", "Ky export da SUBMITTED, khong the regenerate");

        await conn.ExecuteAsync(
            "DELETE FROM diab_his_int_bhyt_export_items WHERE export_id=@id", new { id = cmd.ExportId });

        _jobs.EnqueueBhytGenerateXml(cmd.ExportId, _tenant.TenantId, (string)row.period_month, (string?)row.scope_filter_json);

        return Result<BhytExportResponse>.Success(CreateBhytExportHandler.MapToResponse(row));
    }
}

// ── Validate XSD ──────────────────────────────────────────────────────────────

public record ValidateBhytXmlCommand(int ExportId) : IRequest<Result<BhytValidationResultResponse>>;

public class ValidateBhytXmlHandler : IRequestHandler<ValidateBhytXmlCommand, Result<BhytValidationResultResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBhytXsdValidator _validator;

    public ValidateBhytXmlHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBhytXsdValidator validator)
    {
        _db = db; _tenant = tenant; _validator = validator;
    }

    public async Task<Result<BhytValidationResultResponse>> Handle(ValidateBhytXmlCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytValidationResultResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        var vr = await _validator.ValidateAsync(cmd.ExportId, ct);

        if (vr.Valid)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_bhyt_exports SET status='VALIDATED', validated_at=NOW(), updated_at=NOW() WHERE id=@id",
                new { id = cmd.ExportId });
        }

        var response = new BhytValidationResultResponse(vr.Valid, vr.Errors);

        return vr.Valid
            ? Result<BhytValidationResultResponse>.Success(response)
            : Result<BhytValidationResultResponse>.Failure("BHYT_XSD_VALIDATION_FAILED",
                $"{vr.Errors.Count} loi XSD duoc phat hien", vr.Errors);
    }
}

// ── Sign XML ──────────────────────────────────────────────────────────────────

public record SignBhytXmlCommand(int ExportId, SignBhytExportRequest? Request) : IRequest<Result<BhytExportResponse>>;

public class SignBhytXmlHandler : IRequestHandler<SignBhytXmlCommand, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBhytSigner _signer;

    public SignBhytXmlHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBhytSigner signer)
    {
        _db = db; _tenant = tenant; _signer = signer;
    }

    public async Task<Result<BhytExportResponse>> Handle(SignBhytXmlCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        var sr = await _signer.SignAsync(cmd.ExportId, cmd.Request?.CertThumbprint, cmd.Request?.Pin, ct);

        if (!sr.Success)
            return Result<BhytExportResponse>.Failure("BHYT_SIGN_FAILED", sr.ErrorMessage ?? "Ky so that bai");

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_bhyt_exports SET status='SIGNED', signed_at=NOW(), xml_file_path=@fp, updated_at=NOW() WHERE id=@id",
            new { id = cmd.ExportId, fp = sr.SignedFilePath });

        var updated = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id", new { id = cmd.ExportId });

        return Result<BhytExportResponse>.Success(CreateBhytExportHandler.MapToResponse(updated));
    }
}

// ── Submit to BHYT ────────────────────────────────────────────────────────────

public record SubmitBhytExportCommand(int ExportId) : IRequest<Result<BhytExportResponse>>;

public class SubmitBhytExportHandler : IRequestHandler<SubmitBhytExportCommand, Result<BhytExportResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBhytSubmissionClient _client;

    public SubmitBhytExportHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBhytSubmissionClient client)
    {
        _db = db; _tenant = tenant; _client = client;
    }

    public async Task<Result<BhytExportResponse>> Handle(SubmitBhytExportCommand cmd, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id AND tenant_id=@t AND deleted_at IS NULL",
            new { id = cmd.ExportId, t = _tenant.TenantId });

        if (row == null)
            return Result<BhytExportResponse>.Failure("BHYT_EXPORT_NOT_FOUND", "Khong tim thay ky export BHYT");

        if ((string)row.status != BhytExportStatus.Signed)
            return Result<BhytExportResponse>.Failure("BHYT_SUBMIT_FAILED", "Chi co the submit ky export o trang thai SIGNED");

        var result = await _client.SubmitAsync(cmd.ExportId, _tenant.TenantId, ct);

        if (!result.Success)
            return Result<BhytExportResponse>.Failure("BHYT_SUBMIT_FAILED", result.ErrorMessage ?? "Submit that bai");

        await conn.ExecuteAsync(
            @"UPDATE diab_his_int_bhyt_exports
              SET status='SUBMITTED', submitted_at=NOW(), bhyt_reference=@ref, updated_at=NOW()
              WHERE id=@id",
            new { id = cmd.ExportId, @ref = result.Reference });

        var updated = await conn.QueryFirstAsync<dynamic>(
            "SELECT * FROM diab_his_int_bhyt_exports WHERE id=@id", new { id = cmd.ExportId });

        return Result<BhytExportResponse>.Success(CreateBhytExportHandler.MapToResponse(updated));
    }
}
