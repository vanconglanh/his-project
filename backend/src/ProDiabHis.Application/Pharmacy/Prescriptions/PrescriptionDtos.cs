namespace ProDiabHis.Application.Pharmacy.Prescriptions;

// ── Request DTOs ──────────────────────────────────────────────────────────────
public record PrescriptionCreateRequest(
    Guid EncounterId,
    Guid PatientId,
    string? Note,
    IReadOnlyList<PrescriptionItemRequest>? Items);

public record PrescriptionUpdateRequest(string? Note);

public record PrescriptionItemRequest(
    string DrugId,
    string Dosage,
    string Frequency,
    string Route,
    int DurationDays,
    decimal Quantity,
    string? Instructions);

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
    int? DoctorId,
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
    DateTime UpdatedAt);

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
    IReadOnlyList<BatchDispensed>? BatchDispensed);

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

public record PrintHistoryItem(Guid Id, DateTime PrintedAt, int? PrintedBy, string? PrinterName);
