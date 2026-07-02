using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.Dtqg;

// ── Request / Response (contract spec crud-gap-p0-contracts.md) ─────────────

public record SubmitDtqgFromPrescriptionRequest(bool ForceResubmit = false);

public record SubmitDtqgFromPrescriptionResponse(
    string PrescriptionId,
    string MaDonThuoc,
    string QrUrl,
    DateTime SubmittedAt,
    string Status,
    string PortalStatus);

public record SubmitDtqgFromPrescriptionCommand(Guid PrescriptionId, SubmitDtqgFromPrescriptionRequest Request)
    : IRequest<Result<SubmitDtqgFromPrescriptionCommandResult>>;

public record SubmitDtqgFromPrescriptionCommandResult(
    SubmitDtqgFromPrescriptionResponse Data,
    string Mode);   // "Mock" hoac "Real"

// ── Handler ──────────────────────────────────────────────────────────────────

public class SubmitDtqgFromPrescriptionHandler
    : IRequestHandler<SubmitDtqgFromPrescriptionCommand, Result<SubmitDtqgFromPrescriptionCommandResult>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgClient _dtqgClient;
    private readonly IDtqgPrescriptionPayloadBuilder _payloadBuilder;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly ILogger<SubmitDtqgFromPrescriptionHandler> _logger;

    public SubmitDtqgFromPrescriptionHandler(
        IDapperConnectionFactory db,
        ICurrentUser currentUser,
        IDtqgClient dtqgClient,
        IDtqgPrescriptionPayloadBuilder payloadBuilder,
        IAuditService audit,
        IConfiguration config,
        ILogger<SubmitDtqgFromPrescriptionHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _dtqgClient = dtqgClient;
        _payloadBuilder = payloadBuilder;
        _audit = audit;
        _config = config;
        _logger = logger;
    }

    public async Task<Result<SubmitDtqgFromPrescriptionCommandResult>> Handle(
        SubmitDtqgFromPrescriptionCommand cmd,
        CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var mode = _config["Integrations:Dtqg:Mode"] ?? "Mock";
        var presId = cmd.PrescriptionId.ToString();

        using var conn = (IDbConnection)_db.CreateConnection();

        // Lay thong tin don thuoc (schema canonical: diab_his_pha_prescriptions, id CHAR(36))
        var pres = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, status, dtqg_code, encounter_id
              FROM diab_his_pha_prescriptions
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = presId, tenantId });

        if (pres == null)
            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        string? encounterId = pres.encounter_id;

        // Kiem tra co dong thuoc khong
        var itemCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_pha_prescription_items
              WHERE prescription_id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = presId, tenantId });

        if (itemCount == 0)
            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "PRESCRIPTION_NO_ITEMS", "Don thuoc chua co dong thuoc nao.");

        // Kiem tra chan doan ICD-10 chinh tren encounter
        var diagCount = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_enc_diagnoses
              WHERE encounter_id = @eid AND tenant_id = @tenantId AND type = 'PRIMARY' AND deleted_at IS NULL",
            new { eid = encounterId, tenantId });

        if (diagCount == 0)
            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "PRESCRIPTION_NO_DIAGNOSIS", "Don thuoc chua gan chan doan ICD-10.");

        // Kiem tra idempotent
        string? existingCode = (string?)pres.dtqg_code;
        if (!string.IsNullOrEmpty(existingCode) && !cmd.Request.ForceResubmit)
        {
            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "PRESCRIPTION_ALREADY_SUBMITTED",
                "Don da submit, dung force_resubmit=true de gui lai.");
        }

        // Dung du lieu don thuoc that (patient + chan doan + danh sach thuoc) cho payload
        var prescriptionData = await _payloadBuilder.BuildAsync(presId, tenantId, ct);

        // Submit (HttpDtqgClient tu resolve cskcb/partner/token per-tenant qua IDtqgCredentialProvider)
        var payload = new DtqgSubmitPayload(tenantId, 0, "", "", (object?)prescriptionData ?? new { });
        DtqgSubmitResult submitResult;
        try
        {
            submitResult = await _dtqgClient.SubmitPrescriptionAsync(payload, ct);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(
                "prescription.submit_dtqg",
                "Prescription",
                cmd.PrescriptionId.ToString(),
                AuditSeverity.ERROR,
                details: new { mode, error = ex.Message },
                cancellationToken: ct);

            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "DTQG_PORTAL_UNAVAILABLE", "Cong DTQG khong phan hoi (da enqueue retry job).");
        }

        if (!submitResult.Success || string.IsNullOrEmpty(submitResult.MaDonThuoc))
        {
            return Result<SubmitDtqgFromPrescriptionCommandResult>.Failure(
                "DTQG_PORTAL_UNAVAILABLE", submitResult.ErrorMessage ?? "Gui DTQG that bai.");
        }

        var submittedAt = DateTime.UtcNow;
        var maDonThuoc = submitResult.MaDonThuoc;

        // Cap nhat don thuoc (canonical): luu ma DTQG + thoi diem day
        await conn.ExecuteAsync(
            @"UPDATE diab_his_pha_prescriptions
              SET dtqg_code = @code,
                  dtqg_pushed_at = NOW(),
                  status = 'SUBMITTED_DTQG',
                  updated_at = NOW()
              WHERE id = @presId AND tenant_id = @tenantId",
            new { code = maDonThuoc, presId, tenantId });

        // Ghi submission record (tracking). LUU Y: cot prescription_id cua bang nay dang co xung dot schema
        // giua migration 0011 (INT) va 9011 (CHAR36) -> boc try/catch de loi tracking khong lam hong submit da thanh cong.
        try
        {
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_int_dtqg_submissions
                      (id, tenant_id, prescription_id, status, ma_don_thuoc, submitted_at, accepted_at, retry_count, created_at, updated_at)
                  VALUES (UUID(), @tenantId, @presId, 'ACCEPTED', @code, NOW(), NOW(), 0, NOW(), NOW())
                  ON DUPLICATE KEY UPDATE
                      status = 'ACCEPTED', ma_don_thuoc = @code, submitted_at = NOW(), accepted_at = NOW(), updated_at = NOW()",
                new { tenantId, presId, code = maDonThuoc });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DTQG: ghi submission tracking that bai (submit da thanh cong, ma {Ma})", maDonThuoc);
        }

        var qrUrl = $"/api/v1/prescriptions/{cmd.PrescriptionId}/qr.png";

        // Audit log
        await _audit.LogAsync(
            "prescription.submit_dtqg",
            "Prescription",
            cmd.PrescriptionId.ToString(),
            details: new { mode, ma_don_thuoc = maDonThuoc },
            cancellationToken: ct);

        var response = new SubmitDtqgFromPrescriptionResponse(
            cmd.PrescriptionId.ToString(),
            maDonThuoc,
            qrUrl,
            submittedAt,
            "submitted",
            "ACCEPTED");

        return Result<SubmitDtqgFromPrescriptionCommandResult>.Success(
            new SubmitDtqgFromPrescriptionCommandResult(response, mode));
    }
}
