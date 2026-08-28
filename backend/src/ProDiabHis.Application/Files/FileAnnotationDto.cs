namespace ProDiabHis.Application.Files;

/// <summary>
/// Response cho 1 annotation set gắn với 1 file ảnh. Annotation là layer JSON
/// riêng, render đè lên ảnh gốc khi xem — KHÔNG sửa file ảnh gốc.
/// </summary>
public record FileAnnotationResponse(
    Guid Id,
    Guid FileId,
    Guid? PatientId,
    Guid? EncounterId,
    string AnnotationData,
    int Version,
    DateTime CreatedAt,
    Guid? CreatedBy,
    string? CreatedByName,
    DateTime UpdatedAt,
    Guid? UpdatedBy);
