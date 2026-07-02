using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Files;

// Generic file upload
public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Category)
    : IRequest<Result<FileUploadResponse>>;

public record GetSignedUrlQuery(Guid FileId)
    : IRequest<Result<FileUploadResponse>>;

public record DeleteFileCommand(Guid FileId)
    : IRequest<Result<bool>>;

// CLS uploads
public record UploadClsCommand(
    Guid PatientId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DocType,
    Guid? EncounterId,
    string? Note)
    : IRequest<Result<ClsUploadResponse>>;

public record ListClsUploadsQuery(
    Guid PatientId,
    int Page,
    int PageSize,
    string? DocType)
    : IRequest<PagedResult<ClsUploadResponse>>;

public record GetClsUploadQuery(Guid PatientId, Guid Id)
    : IRequest<Result<ClsUploadResponse>>;

public record DeleteClsUploadCommand(Guid PatientId, Guid Id)
    : IRequest<Result<bool>>;

public record ListEncounterClsUploadsQuery(Guid EncounterId)
    : IRequest<List<ClsUploadResponse>>;
