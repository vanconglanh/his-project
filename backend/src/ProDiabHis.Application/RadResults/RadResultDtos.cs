namespace ProDiabHis.Application.RadResults;

public static class RadResultStatus
{
    public const string Draft    = "DRAFT";
    public const string Verified = "VERIFIED";
    public const string Amended  = "AMENDED";
}

public record RadResultResponse(
    Guid     Id,
    Guid     RadOrderId,
    Guid     PatientId,
    Guid     EncounterId,
    string   Modality,
    string   Findings,
    string?  Impression,
    string   Conclusion,
    string?  Recommendations,
    DateTime PerformedAt,
    Guid?    PerformedBy,
    string   Status,
    DateTime? VerifiedAt,
    Guid?    VerifiedBy,
    int      DicomCount,
    string?  SignedPdfUrl,
    DateTime CreatedAt);

public record RadResultCreateRequest(
    Guid     RadOrderId,
    string   Findings,
    string?  Impression,
    string   Conclusion,
    string?  Recommendations,
    DateTime PerformedAt);

public record RadResultUpdateRequest(
    string?  Findings,
    string?  Impression,
    string?  Conclusion,
    string?  Recommendations,
    string?  AmendReason);
