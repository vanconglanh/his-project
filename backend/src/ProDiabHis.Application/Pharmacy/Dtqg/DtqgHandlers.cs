using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.Dtqg;

// ─── Commands & Queries ───────────────────────────────────────────────────────
public record SubmitDtqgCommand(Guid PrescriptionId) : IRequest<Result<DtqgSubmissionResponse>>;
public record GetDtqgStatusQuery(Guid PrescriptionId) : IRequest<Result<DtqgSubmissionResponse>>;
public record RetryDtqgCommand(Guid PrescriptionId) : IRequest<Result<DtqgSubmissionResponse>>;
public record ListDtqgSubmissionsQuery(string? Status, DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize)
    : IRequest<Result<PagedResult<DtqgSubmissionResponse>>>;
public record CancelOnPortalCommand(Guid SubmissionId, string Reason) : IRequest<Result<DtqgSubmissionResponse>>;
public record GetDtqgCredentialsQuery : IRequest<Result<DtqgCredentialsResponse>>;
public record UpsertDtqgCredentialsCommand(DtqgCredentialsRequest Request) : IRequest<Result<DtqgCredentialsResponse>>;
public record TestDtqgCredentialsCommand : IRequest<Result<DtqgTestResult>>;

// ─── Handlers ─────────────────────────────────────────────────────────────────
public class SubmitDtqgHandler : IRequestHandler<SubmitDtqgCommand, Result<DtqgSubmissionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgClient _dtqgClient;
    private readonly IEncryptionService _encryption;

    public SubmitDtqgHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDtqgClient dtqgClient, IEncryptionService encryption)
    {
        _db = db;
        _currentUser = currentUser;
        _dtqgClient = dtqgClient;
        _encryption = encryption;
    }

    public async Task<Result<DtqgSubmissionResponse>> Handle(SubmitDtqgCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pres = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT ID, status, dtqg_code, dtqg_status FROM pha_prescriptions WHERE ID = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.PrescriptionId.ToString(), tenantId });

        if (pres == null)
            return Result<DtqgSubmissionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        string presStatus = pres.status;
        if (presStatus != "SIGNED")
            return Result<DtqgSubmissionResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Chi co the gui DTQG don thuoc da ky so.");

        string? existingCode = pres.dtqg_code;
        if (!string.IsNullOrEmpty(existingCode))
            return Result<DtqgSubmissionResponse>.Failure("DTQG_PRESCRIPTION_ALREADY_SUBMITTED", "Don thuoc da duoc gui DTQG.");

        // Get credentials
        var creds = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT cskcb_id, partner_code, token_encrypted FROM diab_his_int_dtqg_credentials WHERE tenant_id = @tenantId AND deleted_at IS NULL",
            new { tenantId });

        if (creds == null)
            return Result<DtqgSubmissionResponse>.Failure("DTQG_CSKCB_NOT_REGISTERED", "Phong kham chua dang ky tich hop DTQG.");

        // Create submission record
        var submissionId = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_int_dtqg_submissions (id, tenant_id, prescription_id, status, retry_count, created_at, updated_at)
              VALUES (@id, @tenantId, @presId, 'PENDING', 0, NOW(), NOW())",
            new { id = submissionId, tenantId, presId = (int)pres.ID });

        // Submit to DTQG
        var payload = new DtqgSubmitPayload(tenantId, (int)pres.ID, creds.cskcb_id, creds.partner_code, new { });
        var submitResult = await _dtqgClient.SubmitPrescriptionAsync(payload, ct);

        if (submitResult.Success && submitResult.MaDonThuoc?.Length == 14)
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_int_dtqg_submissions
                  SET status = 'ACCEPTED', ma_don_thuoc = @maDonThuoc, submitted_at = NOW(), accepted_at = NOW(), updated_at = NOW()
                  WHERE id = @id",
                new { maDonThuoc = submitResult.MaDonThuoc, id = submissionId });

            await conn.ExecuteAsync(
                "UPDATE pha_prescriptions SET dtqg_code = @code, dtqg_status = 'ACCEPTED', status = 'SUBMITTED_DTQG', UPDATED_AT = NOW() WHERE ID = @presId AND tenant_id = @tenantId",
                new { code = submitResult.MaDonThuoc, presId = (int)pres.ID, tenantId });

            var sub = await GetSubmissionById(conn, submissionId);
            return Result<DtqgSubmissionResponse>.Success(sub);
        }
        else
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_int_dtqg_submissions
                  SET status = 'REJECTED', error_code = @errCode, error_message = @errMsg, submitted_at = NOW(), updated_at = NOW()
                  WHERE id = @id",
                new { errCode = submitResult.ErrorCode, errMsg = submitResult.ErrorMessage, id = submissionId });

            return Result<DtqgSubmissionResponse>.Failure("DTQG_SUBMIT_FAILED", submitResult.ErrorMessage ?? "Gui DTQG that bai.");
        }
    }

    private static async Task<DtqgSubmissionResponse> GetSubmissionById(System.Data.IDbConnection conn, string id)
    {
        var row = await conn.QueryFirstAsync<dynamic>(
            @"SELECT id, tenant_id, prescription_id, ma_don_thuoc, qr_payload,
                     status, error_code, error_message, submitted_at, accepted_at, retry_count, last_retry_at
              FROM diab_his_int_dtqg_submissions WHERE id = @id", new { id });

        return MapSubmission(row);
    }

    internal static DtqgSubmissionResponse MapSubmission(dynamic row) =>
        new(Guid.TryParse((string?)row.id, out var g) ? g : Guid.Empty,
            (int)row.prescription_id,
            (string?)row.ma_don_thuoc,
            (string?)row.qr_payload, null,
            (string)row.status,
            (string?)row.error_code, (string?)row.error_message,
            (DateTime?)row.submitted_at, (DateTime?)row.accepted_at,
            (int)row.retry_count, (DateTime?)row.last_retry_at);
}

public class GetDtqgStatusHandler : IRequestHandler<GetDtqgStatusQuery, Result<DtqgSubmissionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDtqgStatusHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<DtqgSubmissionResponse>> Handle(GetDtqgStatusQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var presId = await conn.ExecuteScalarAsync<int?>(
            "SELECT ID FROM pha_prescriptions WHERE ID = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.PrescriptionId.ToString(), tenantId });

        if (!presId.HasValue)
            return Result<DtqgSubmissionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, tenant_id, prescription_id, ma_don_thuoc, qr_payload,
                     status, error_code, error_message, submitted_at, accepted_at, retry_count, last_retry_at
              FROM diab_his_int_dtqg_submissions
              WHERE prescription_id = @presId AND tenant_id = @tenantId
              ORDER BY created_at DESC LIMIT 1",
            new { presId, tenantId });

        if (row == null)
            return Result<DtqgSubmissionResponse>.Failure("DTQG_SUBMIT_FAILED", "Chua co thong tin gui DTQG cho don thuoc nay.");

        return Result<DtqgSubmissionResponse>.Success(SubmitDtqgHandler.MapSubmission(row));
    }
}

public class RetryDtqgHandler : IRequestHandler<RetryDtqgCommand, Result<DtqgSubmissionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgClient _dtqgClient;

    public RetryDtqgHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDtqgClient dtqgClient)
    {
        _db = db;
        _currentUser = currentUser;
        _dtqgClient = dtqgClient;
    }

    public async Task<Result<DtqgSubmissionResponse>> Handle(RetryDtqgCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT s.id, s.prescription_id, s.retry_count, s.status
              FROM diab_his_int_dtqg_submissions s
              JOIN pha_prescriptions p ON p.ID = s.prescription_id
              WHERE p.ID = @presId AND p.tenant_id = @tenantId
              ORDER BY s.created_at DESC LIMIT 1",
            new { presId = cmd.PrescriptionId.ToString(), tenantId });

        if (sub == null)
            return Result<DtqgSubmissionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay thong tin gui DTQG.");

        int retryCount = (int)sub.retry_count;
        if (retryCount >= 5)
            return Result<DtqgSubmissionResponse>.Failure("DTQG_RETRY_EXCEEDED", "Da vuot qua so lan thu lai cho phep (max 5).");

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_dtqg_submissions SET status = 'PENDING', retry_count = retry_count + 1, last_retry_at = NOW(), updated_at = NOW() WHERE id = @id",
            new { id = (string)sub.id });

        // Re-submit (simplified)
        var payload = new DtqgSubmitPayload(tenantId, (int)sub.prescription_id, "", "", new { });
        var result = await _dtqgClient.SubmitPrescriptionAsync(payload, ct);

        if (result.Success && result.MaDonThuoc?.Length == 14)
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_dtqg_submissions SET status = 'ACCEPTED', ma_don_thuoc = @code, accepted_at = NOW(), updated_at = NOW() WHERE id = @id",
                new { code = result.MaDonThuoc, id = (string)sub.id });
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE diab_his_int_dtqg_submissions SET status = 'REJECTED', error_code = @ec, error_message = @em, updated_at = NOW() WHERE id = @id",
                new { ec = result.ErrorCode, em = result.ErrorMessage, id = (string)sub.id });
        }

        var updated = await conn.QueryFirstAsync<dynamic>(
            @"SELECT id, tenant_id, prescription_id, ma_don_thuoc, qr_payload,
                     status, error_code, error_message, submitted_at, accepted_at, retry_count, last_retry_at
              FROM diab_his_int_dtqg_submissions WHERE id = @id", new { id = (string)sub.id });

        return Result<DtqgSubmissionResponse>.Success(SubmitDtqgHandler.MapSubmission(updated));
    }
}

public class ListDtqgSubmissionsHandler : IRequestHandler<ListDtqgSubmissionsQuery, Result<PagedResult<DtqgSubmissionResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListDtqgSubmissionsHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<DtqgSubmissionResponse>>> Handle(ListDtqgSubmissionsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "tenant_id = @tenantId", "deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);
        prm.Add("offset", offset);
        prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("status = @status"); prm.Add("status", q.Status); }
        if (q.FromDate.HasValue) { where.Add("DATE(submitted_at) >= @fromDate"); prm.Add("fromDate", q.FromDate.Value); }
        if (q.ToDate.HasValue) { where.Add("DATE(submitted_at) <= @toDate"); prm.Add("toDate", q.ToDate.Value); }

        var whereClause = string.Join(" AND ", where);
        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM diab_his_int_dtqg_submissions WHERE {whereClause}", prm);

        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT id, tenant_id, prescription_id, ma_don_thuoc, qr_payload,
                      status, error_code, error_message, submitted_at, accepted_at, retry_count, last_retry_at
               FROM diab_his_int_dtqg_submissions WHERE {whereClause}
               ORDER BY created_at DESC LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => SubmitDtqgHandler.MapSubmission(r)).Cast<DtqgSubmissionResponse>().ToList();
        return Result<PagedResult<DtqgSubmissionResponse>>.Success(
            new PagedResult<DtqgSubmissionResponse>(items, q.Page, q.PageSize, total));
    }
}

public class CancelOnPortalHandler : IRequestHandler<CancelOnPortalCommand, Result<DtqgSubmissionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgClient _dtqgClient;

    public CancelOnPortalHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDtqgClient dtqgClient)
    {
        _db = db;
        _currentUser = currentUser;
        _dtqgClient = dtqgClient;
    }

    public async Task<Result<DtqgSubmissionResponse>> Handle(CancelOnPortalCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var sub = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, prescription_id, ma_don_thuoc, status, retry_count, submitted_at, accepted_at, last_retry_at, qr_payload, error_code, error_message
              FROM diab_his_int_dtqg_submissions
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.SubmissionId.ToString(), tenantId });

        if (sub == null)
            return Result<DtqgSubmissionResponse>.Failure("DTQG_SUBMIT_FAILED", "Khong tim thay ban ghi gui DTQG.");

        if (!string.IsNullOrEmpty((string?)sub.ma_don_thuoc))
            await _dtqgClient.CancelAsync((string)sub.ma_don_thuoc, cmd.Reason, ct);

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_dtqg_submissions SET status = 'REJECTED', error_message = @msg, updated_at = NOW() WHERE id = @id",
            new { msg = $"CANCELLED: {cmd.Reason}", id = (string)sub.id });

        var updated = await conn.QueryFirstAsync<dynamic>(
            @"SELECT id, tenant_id, prescription_id, ma_don_thuoc, qr_payload,
                     status, error_code, error_message, submitted_at, accepted_at, retry_count, last_retry_at
              FROM diab_his_int_dtqg_submissions WHERE id = @id", new { id = (string)sub.id });

        return Result<DtqgSubmissionResponse>.Success(SubmitDtqgHandler.MapSubmission(updated));
    }
}

public class GetDtqgCredentialsHandler : IRequestHandler<GetDtqgCredentialsQuery, Result<DtqgCredentialsResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDtqgCredentialsHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<DtqgCredentialsResponse>> Handle(GetDtqgCredentialsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, tenant_id, cskcb_id, partner_code, token_encrypted, is_active, last_tested_at, last_test_ok FROM diab_his_int_dtqg_credentials WHERE tenant_id = @tenantId AND deleted_at IS NULL",
            new { tenantId });

        if (row == null)
            return Result<DtqgCredentialsResponse>.Success(new DtqgCredentialsResponse(Guid.Empty, tenantId, null, null, null, false, null, null));

        string? tok = (string?)row.token_encrypted;
        var masked = tok?.Length > 4 ? "****" + tok[^4..] : "****";

        return Result<DtqgCredentialsResponse>.Success(new DtqgCredentialsResponse(
            Guid.TryParse((string?)row.id, out var g) ? g : Guid.Empty,
            tenantId,
            (string?)row.cskcb_id, (string?)row.partner_code, masked,
            row.is_active == 1, (DateTime?)row.last_tested_at, row.last_test_ok == 1));
    }
}

public class UpsertDtqgCredentialsHandler : IRequestHandler<UpsertDtqgCredentialsCommand, Result<DtqgCredentialsResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;

    public UpsertDtqgCredentialsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IEncryptionService encryption)
    {
        _db = db;
        _currentUser = currentUser;
        _encryption = encryption;
    }

    public async Task<Result<DtqgCredentialsResponse>> Handle(UpsertDtqgCredentialsCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var tokenEncrypted = _encryption.Encrypt(cmd.Request.Token);

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_int_dtqg_credentials (id, tenant_id, cskcb_id, partner_code, token_encrypted, is_active, created_at, updated_at)
              VALUES (UUID(), @tenantId, @cskcbId, @partnerCode, @token, 1, NOW(), NOW())
              ON DUPLICATE KEY UPDATE cskcb_id = @cskcbId, partner_code = @partnerCode, token_encrypted = @token, is_active = 1, updated_at = NOW()",
            new { tenantId, cskcbId = cmd.Request.CskcbId, partnerCode = cmd.Request.PartnerCode, token = tokenEncrypted });

        return await new GetDtqgCredentialsHandler(_db, _currentUser).Handle(new GetDtqgCredentialsQuery(), ct);
    }
}

public class TestDtqgCredentialsHandler : IRequestHandler<TestDtqgCredentialsCommand, Result<DtqgTestResult>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgClient _dtqgClient;

    public TestDtqgCredentialsHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDtqgClient dtqgClient)
    {
        _db = db;
        _currentUser = currentUser;
        _dtqgClient = dtqgClient;
    }

    public async Task<Result<DtqgTestResult>> Handle(TestDtqgCredentialsCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pingResult = await _dtqgClient.PingAsync(ct);

        await conn.ExecuteAsync(
            "UPDATE diab_his_int_dtqg_credentials SET last_tested_at = NOW(), last_test_ok = @ok, updated_at = NOW() WHERE tenant_id = @tenantId",
            new { ok = pingResult.Ok ? 1 : 0, tenantId });

        if (!pingResult.Ok)
            return Result<DtqgTestResult>.Failure("DTQG_TOKEN_EXPIRED", "Token DTQG het han hoac khong hop le.");

        return Result<DtqgTestResult>.Success(new DtqgTestResult(pingResult.Ok, pingResult.LatencyMs, pingResult.PortalResponse));
    }
}
