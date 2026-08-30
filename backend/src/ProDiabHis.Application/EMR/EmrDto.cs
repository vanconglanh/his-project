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
    Guid? UpdatedBy,
    // §5.8 — gia tri form dang soan + schema snapshot cua ban ghi (FE render theo schemaSnapshot, KHONG goi lai template)
    object? StructuredValues = null,
    object? SchemaSnapshot = null);

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
    DateTime CreatedAt,
    // §5.7.2 — dinh nghia form (mang field) + co phai mau mac dinh
    object? StructuredJson = null,
    bool IsDefault = false);

public record EmrVersionDiffDto(IReadOnlyList<object> Ops);
