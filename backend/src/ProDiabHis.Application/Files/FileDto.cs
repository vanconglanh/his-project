namespace ProDiabHis.Application.Files;

public record FileUploadResponse(
    Guid Id,
    string FileName,
    string? MimeType,
    long? FileSizeBytes,
    string SignedUrl,
    DateTime SignedUrlExpiresAt);

public record ClsUploadResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string DocType,
    Guid? FileId,
    string FileName,
    long? FileSizeBytes,
    string? MimeType,
    string? SignedUrl,
    DateTime UploadedAt,
    Guid? UploadedBy,
    string? UploadedByName,
    string? Note);
