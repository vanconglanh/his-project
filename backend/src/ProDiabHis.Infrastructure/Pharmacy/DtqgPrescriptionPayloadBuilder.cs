using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Dựng <see cref="DtqgPrescriptionData"/> từ schema canonical (<c>diab_his_pha_prescriptions</c>,
/// <c>diab_his_pha_prescription_items</c> JOIN <c>diab_his_pha_drugs</c>, <c>diab_his_pat_patients</c>,
/// <c>diab_his_pat_insurances</c>, <c>diab_his_enc_diagnoses</c>). Số thẻ BHYT giải mã AES-256-GCM.
/// Mọi query lọc theo <c>tenant_id</c> + <c>deleted_at IS NULL</c>.
/// </summary>
public class DtqgPrescriptionPayloadBuilder : IDtqgPrescriptionPayloadBuilder
{
    private readonly IDapperConnectionFactory _db;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<DtqgPrescriptionPayloadBuilder> _logger;

    public DtqgPrescriptionPayloadBuilder(
        IDapperConnectionFactory db,
        IEncryptionService encryption,
        ILogger<DtqgPrescriptionPayloadBuilder> logger)
    {
        _db = db;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<DtqgPrescriptionData?> BuildAsync(string prescriptionId, int tenantId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        var header = await conn.QueryFirstOrDefaultAsync<HeaderRow>(
            "SELECT encounter_id AS EncounterId, patient_id AS PatientId, diagnosis_icd10 AS DiagnosisIcd10, note AS Note " +
            "FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = prescriptionId, tenantId });
        if (header is null)
            return null;

        var patient = await conn.QueryFirstOrDefaultAsync<PatientRow>(
            "SELECT code AS Code, full_name AS FullName, gender AS Gender, date_of_birth AS DateOfBirth " +
            "FROM diab_his_pat_patients WHERE id = @pid AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { pid = header.PatientId, tenantId });
        if (patient is null)
            return null;

        var cardEnc = await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT card_no_enc FROM diab_his_pat_insurances " +
            "WHERE patient_id = @pid AND tenant_id = @tenantId AND type = 'BHYT' AND deleted_at IS NULL " +
            "ORDER BY valid_to DESC LIMIT 1",
            new { pid = header.PatientId, tenantId });
        var cardNo = DecryptSafe(cardEnc);

        DiagnosisRow? diag = null;
        if (!string.IsNullOrEmpty(header.EncounterId))
        {
            diag = await conn.QueryFirstOrDefaultAsync<DiagnosisRow>(
                "SELECT icd10_code AS Icd10Code, name AS Name FROM diab_his_enc_diagnoses " +
                "WHERE encounter_id = @eid AND tenant_id = @tenantId AND type = 'PRIMARY' AND deleted_at IS NULL LIMIT 1",
                new { eid = header.EncounterId, tenantId });
        }

        var items = (await conn.QueryAsync<ItemRow>(
            "SELECT i.drug_name AS DrugName, d.generic_name AS GenericName, i.drug_strength AS Strength, i.unit AS Unit, " +
            "i.quantity AS Quantity, i.dosage AS Dosage, i.frequency AS Frequency, i.route AS Route, i.duration_days AS DurationDays, " +
            "d.dtqg_drug_code AS DtqgDrugCode, i.note AS Note " +
            "FROM diab_his_pha_prescription_items i LEFT JOIN diab_his_pha_drugs d ON d.id = i.drug_id " +
            "WHERE i.prescription_id = @id AND i.tenant_id = @tenantId AND i.deleted_at IS NULL",
            new { id = prescriptionId, tenantId })).ToList();

        return MapPayload(header, patient, cardNo, diag, items);
    }

    /// <summary>Ánh xạ thuần (không I/O) từ dữ liệu DB sang payload — tách riêng để unit test.</summary>
    public static DtqgPrescriptionData MapPayload(
        HeaderRow header, PatientRow patient, string? cardNo, DiagnosisRow? diag, IReadOnlyList<ItemRow> items)
    {
        var drugs = items.Select((it, idx) => new DtqgDrugItem(
            idx + 1,
            it.DrugName ?? string.Empty,
            it.GenericName,
            it.Strength,
            it.Unit,
            it.Quantity,
            it.Dosage,
            it.Frequency,
            it.Route,
            it.DurationDays,
            it.DtqgDrugCode,
            it.Note)).ToList();

        return new DtqgPrescriptionData(
            new DtqgPatientInfo(patient.Code, patient.FullName ?? string.Empty, patient.Gender, patient.DateOfBirth?.Year, cardNo),
            diag?.Icd10Code ?? header.DiagnosisIcd10,
            diag?.Name,
            drugs,
            header.Note);
    }

    private string? DecryptSafe(string? enc)
    {
        if (string.IsNullOrWhiteSpace(enc))
            return null;
        try
        {
            return _encryption.Decrypt(enc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTQG: giai ma so the BHYT that bai");
            return null;
        }
    }

    // ── Row records (public để unit test MapPayload) ──────────────────────────
    public sealed class HeaderRow
    {
        public string? EncounterId { get; set; }
        public string? PatientId { get; set; }
        public string? DiagnosisIcd10 { get; set; }
        public string? Note { get; set; }
    }

    public sealed class PatientRow
    {
        public string? Code { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public sealed class DiagnosisRow
    {
        public string? Icd10Code { get; set; }
        public string? Name { get; set; }
    }

    public sealed class ItemRow
    {
        public string? DrugName { get; set; }
        public string? GenericName { get; set; }
        public string? Strength { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Route { get; set; }
        public int? DurationDays { get; set; }
        public string? DtqgDrugCode { get; set; }
        public string? Note { get; set; }
    }
}
