namespace ProDiabHis.Application.Pharmacy.Prescriptions;

// ── Request DTOs ──────────────────────────────────────────────────────────────
/// <param name="IsTelehealthContext">
/// FR-803: bao FE/client dang ke don tu luong tu van tu xa (telehealth). Khi true, handler BAT BUOC
/// kiem tra EncounterId tra ve tu diab_his_enc_encounters phai co telehealth_session_id (khong null) -
/// neu khong se tra loi TELEHEALTH_ENCOUNTER_REQUIRED. Mac dinh false (ke don thuong tai phong kham).
/// </param>
public record PrescriptionCreateRequest(
    Guid EncounterId,
    Guid PatientId,
    string? Note,
    IReadOnlyList<PrescriptionItemRequest>? Items,
    bool IsTelehealthContext = false);

/// <param name="RowVersion">
/// BM-32: bat buoc gui kem gia tri row_version dang co o client (lay tu GET truoc do) de
/// enforce optimistic locking - neu don da bi nguoi khac sua truoc (row_version lech),
/// server tra 409 PRESCRIPTION_CONFLICT thay vi ghi de am tham.
/// </param>
public record PrescriptionUpdateRequest(string? Note, int RowVersion);

public record PrescriptionItemRequest(
    string DrugId,
    string Dosage,
    string Frequency,
    string Route,
    int DurationDays,
    decimal Quantity,
    string? Instructions,
    // BM-32: lieu dung theo tung buoi (sang/trua/chieu/toi) + cach dung rieng.
    // Optional de tuong thich nguoc voi client cu chi gui Dosage/Frequency dang chuoi.
    decimal? MorningDose = null,
    decimal? NoonDose = null,
    decimal? AfternoonDose = null,
    decimal? EveningDose = null,
    string? CachDung = null);

public record AddPrescriptionItemsRequest(IReadOnlyList<PrescriptionItemRequest> Items);

public record SignPrescriptionRequest(
    string SignatureData,
    string CertificateThumbprint,
    DateTime? SigningTime);

public record CancelPrescriptionRequest(string Reason);

// ── Response DTOs ─────────────────────────────────────────────────────────────
public record PrescriptionResponse(
    Guid Id,
    int TenantId,
    Guid EncounterId,
    Guid PatientId,
    PatientSummary? PatientSummary,
    // BUG FIX: truoc la "int? DoctorId" nhung cot that su la char(36) GUID
    // (diab_his_pha_prescriptions.doctor_id -> diab_his_sec_users.id) - sai kieu
    // khien khong bao gio gan duoc gia tri that (luon truyen null).
    Guid? DoctorId,
    string? DoctorName,
    string Status,
    DateTime PrescribedAt,
    DateTime? SignedAt,
    int? SignedBy,
    string? DtqgCode,
    string DtqgStatus,
    IReadOnlyList<PrescriptionItemResponse> Items,
    IReadOnlyList<DdiWarning> DdiWarnings,
    decimal TotalAmount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int RowVersion = 0);

public record PatientSummary(string FullName, string? Gender, DateOnly? Dob, string? BhytNo);

public record PrescriptionItemResponse(
    Guid Id,
    string DrugId,
    string DrugName,
    string? Strength,
    string? Unit,
    string Dosage,
    string Frequency,
    string Route,
    int DurationDays,
    decimal Quantity,
    string? Instructions,
    IReadOnlyList<BatchDispensed>? BatchDispensed,
    decimal? MorningDose = null,
    decimal? NoonDose = null,
    decimal? AfternoonDose = null,
    decimal? EveningDose = null,
    string? CachDung = null);

public record BatchDispensed(string BatchNo, decimal Quantity);

public record DdiWarning(
    int Drug1Id,
    string Drug1Name,
    int Drug2Id,
    string Drug2Name,
    string Severity,
    string Description,
    string EvidenceLevel);

public record DdiCheckResponse(
    Guid PrescriptionId,
    IReadOnlyList<DdiWarning> Warnings,
    bool HasContraindicated);

// BUG FIX: Dapper QueryAsync<T> khong map duoc record positional constructor trong truong
// hop nay (loi 500 GET /prescriptions/{id}/print-history) -> doi sang class + property setter,
// pattern Dapper luon ho tro on dinh (default parameterless ctor + set tung property).
public class PrintHistoryItem
{
    public Guid Id { get; set; }
    public DateTime PrintedAt { get; set; }
    public int? PrintedBy { get; set; }
    public string? PrinterName { get; set; }
}
