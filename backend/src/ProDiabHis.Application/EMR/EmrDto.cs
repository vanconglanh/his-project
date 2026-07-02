namespace ProDiabHis.Application.EMR;

public record EmrContentResponse(
    Guid Id,
    Guid EncounterId,
    object ContentJson,
    string? ContentHtml,
    Guid? TemplateId,
    DateTime? SignedAt,
    Guid? SignedBy,
    string? SignedByName,
    SignatureCertDto? SignatureCertificate,
    int Version,
    DateTime UpdatedAt,
    Guid? UpdatedBy);

public record SignatureCertDto(string? Serial, string? Subject, string? Algorithm);

public record EmrVersionMetaDto(
    Guid VersionId,
    int Version,
    DateTime SavedAt,
    Guid? SavedBy,
    string? SavedByName,
    bool IsSigned,
    int BytesSize);

public record EmrTemplateResponse(
    Guid Id,
    int? TenantId,
    string Name,
    object ContentJson,
    string Speciality,
    bool IsSystem,
    Guid? CreatedBy,
    DateTime CreatedAt);

public record EmrVersionDiffDto(IReadOnlyList<object> Ops);
