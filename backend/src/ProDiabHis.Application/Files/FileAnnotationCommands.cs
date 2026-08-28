using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Files;

public record ListFileAnnotationsQuery(Guid FileId)
    : IRequest<Result<List<FileAnnotationResponse>>>;

public record CreateFileAnnotationCommand(
    Guid FileId,
    Guid? PatientId,
    Guid? EncounterId,
    string AnnotationData)
    : IRequest<Result<FileAnnotationResponse>>;

public record UpdateFileAnnotationCommand(
    Guid FileId,
    Guid Id,
    string AnnotationData)
    : IRequest<Result<FileAnnotationResponse>>;

public record DeleteFileAnnotationCommand(Guid FileId, Guid Id)
    : IRequest<Result<bool>>;
