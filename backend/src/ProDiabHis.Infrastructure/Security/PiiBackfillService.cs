using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Backfill PII: chuyen du lieu cu plaintext (phone, street, reception_note) sang cot *_enc
/// va sinh blind index (phone_bidx, id_number_bidx, card_no_bidx).
///
/// AES-256-GCM khong lam duoc bang SQL thuan -> phai chay bang code C# (job mot lan).
/// Idempotent theo 2 lop:
///   1) Chi xu ly ban ghi co cot *_enc con NULL/chua mang tien to "enc:v1:"
///   2) IPiiProtector.Protect() tu bo qua gia tri da ma hoa
/// Sau khi ghi cot *_enc thanh cong -> XOA plaintext o cot cu (phone/street/reception_note = NULL).
/// Chay lai nhieu lan an toan.
/// </summary>
public class PiiBackfillService : IPiiBackfillService
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPiiProtector _pii;
    private readonly IAuditService _audit;
    private readonly ILogger<PiiBackfillService> _logger;

    public PiiBackfillService(
        IDapperConnectionFactory db,
        IPiiProtector pii,
        IAuditService audit,
        ILogger<PiiBackfillService> logger)
    {
        _db = db; _pii = pii; _audit = audit; _logger = logger;
    }

    public async Task<PiiBackfillResult> RunAsync(int tenantId, int batchSize, bool dryRun, CancellationToken ct = default)
    {
        if (batchSize <= 0 || batchSize > 5000) batchSize = 500;

        var errors = new List<string>();
        int scanned = 0, encrypted = 0, indexed = 0, insIndexed = 0;

        using var conn = _db.CreateConnection();

        // ---------- Benh nhan ----------
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var rows = (await conn.QueryAsync<PatientBackfillRow>(
                @"SELECT id AS Id, phone AS Phone, phone_enc AS PhoneEnc, phone_bidx AS PhoneBidx,
                         street AS Street, street_enc AS StreetEnc,
                         reception_note AS ReceptionNote, reception_note_enc AS ReceptionNoteEnc,
                         id_number_enc AS IdNumberEnc, id_number_bidx AS IdNumberBidx
                  FROM diab_his_pat_patients
                  WHERE tenant_id = @tenantId
                    AND (
                          (phone IS NOT NULL AND phone <> '' AND phone_enc IS NULL)
                       OR (street IS NOT NULL AND street <> '' AND street_enc IS NULL)
                       OR (reception_note IS NOT NULL AND reception_note <> '' AND reception_note_enc IS NULL)
                       OR (phone IS NOT NULL AND phone <> '' AND phone_bidx IS NULL)
                    )
                  LIMIT @batchSize",
                new { tenantId, batchSize })).ToList();

            if (rows.Count == 0) break;
            scanned += rows.Count;

            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var phoneEnc = r.PhoneEnc ?? _pii.Protect(r.Phone);
                    var streetEnc = r.StreetEnc ?? _pii.Protect(r.Street);
                    var noteEnc = r.ReceptionNoteEnc ?? _pii.Protect(r.ReceptionNote);
                    var phoneBidx = r.PhoneBidx ?? _pii.BlindIndex(r.Phone, PiiField.Phone);
                    var phoneMasked = MaskPhone(r.Phone);

                    if (dryRun) { encrypted++; continue; }

                    await conn.ExecuteAsync(
                        @"UPDATE diab_his_pat_patients
                          SET phone_enc = @phoneEnc,
                              phone_masked = COALESCE(phone_masked, @phoneMasked),
                              phone_bidx = @phoneBidx,
                              street_enc = @streetEnc,
                              reception_note_enc = @noteEnc,
                              phone = NULL, street = NULL, reception_note = NULL
                          WHERE id = @id AND tenant_id = @tenantId",
                        new { id = r.Id, tenantId, phoneEnc, phoneMasked, phoneBidx, streetEnc, noteEnc });

                    encrypted++;
                    if (phoneBidx != null) indexed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PiiBackfill: loi xu ly benh nhan {PatientId}", r.Id);
                    errors.Add($"patient:{r.Id}:{ex.Message}");
                }
            }

            if (dryRun) break; // dry-run khong update -> tranh vong lap vo tan
        }

        // ---------- Blind index CMND (du lieu da ma hoa san, chi thieu bidx) ----------
        // Khong the tinh bidx tu ciphertext neu khong giai ma -> giai ma tung ban ghi.
        var idRows = (await conn.QueryAsync<(string Id, string IdNumberEnc)>(
            @"SELECT id, id_number_enc FROM diab_his_pat_patients
              WHERE tenant_id = @tenantId AND id_number_enc IS NOT NULL AND id_number_bidx IS NULL
              LIMIT 100000",
            new { tenantId })).ToList();

        foreach (var (id, enc) in idRows)
        {
            try
            {
                var plain = _pii.Unprotect(enc);
                var bidx = _pii.BlindIndex(plain, PiiField.IdNumber);
                if (bidx == null) continue;
                if (dryRun) { indexed++; continue; }

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pat_patients SET id_number_bidx = @bidx WHERE id = @id AND tenant_id = @tenantId",
                    new { bidx, id, tenantId });
                indexed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PiiBackfill: loi blind index CMND benh nhan {PatientId}", id);
                errors.Add($"patient-idnumber:{id}:{ex.Message}");
            }
        }

        // ---------- Blind index so the BHYT ----------
        var cardRows = (await conn.QueryAsync<(string Id, string CardNoEnc)>(
            @"SELECT id, card_no_enc FROM diab_his_pat_insurances
              WHERE tenant_id = @tenantId AND card_no_enc IS NOT NULL AND card_no_bidx IS NULL
              LIMIT 100000",
            new { tenantId })).ToList();

        foreach (var (id, enc) in cardRows)
        {
            try
            {
                var plain = _pii.Unprotect(enc);
                var bidx = _pii.BlindIndex(plain, PiiField.InsuranceCardNo);
                if (bidx == null) continue;
                if (dryRun) { insIndexed++; continue; }

                await conn.ExecuteAsync(
                    "UPDATE diab_his_pat_insurances SET card_no_bidx = @bidx WHERE id = @id AND tenant_id = @tenantId",
                    new { bidx, id, tenantId });
                insIndexed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PiiBackfill: loi blind index the BHYT {InsuranceId}", id);
                errors.Add($"insurance:{id}:{ex.Message}");
            }
        }

        // Audit: day la thao tac giai ma / xu ly hang loat du lieu nhay cam -> BAT BUOC ghi log
        await _audit.LogAsync(
            dryRun ? "PII_BACKFILL_DRYRUN" : "PII_BACKFILL",
            "Patient", null, AuditSeverity.WARN, false, null,
            new { tenantId, scanned, encrypted, indexed, insIndexed, errorCount = errors.Count }, ct);

        _logger.LogWarning(
            "PiiBackfill hoan tat tenant={TenantId} dryRun={DryRun} scanned={Scanned} encrypted={Enc} bidx={Bidx} insBidx={InsBidx} errors={Err}",
            tenantId, dryRun, scanned, encrypted, indexed, insIndexed, errors.Count);

        return new PiiBackfillResult(scanned, encrypted, indexed, insIndexed, errors);
    }

    private static string? MaskPhone(string? plain)
    {
        if (string.IsNullOrEmpty(plain) || plain.Length <= 5) return plain;
        return plain[..2] + new string('*', plain.Length - 5) + plain[^3..];
    }

    private class PatientBackfillRow
    {
        public string Id { get; set; } = "";
        public string? Phone { get; set; }
        public string? PhoneEnc { get; set; }
        public string? PhoneBidx { get; set; }
        public string? Street { get; set; }
        public string? StreetEnc { get; set; }
        public string? ReceptionNote { get; set; }
        public string? ReceptionNoteEnc { get; set; }
        public string? IdNumberEnc { get; set; }
        public string? IdNumberBidx { get; set; }
    }
}
