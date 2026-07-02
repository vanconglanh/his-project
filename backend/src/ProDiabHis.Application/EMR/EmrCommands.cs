using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.EMR;

// Requests
public record EmrSaveRequest(object ContentJson, string? ContentHtml, Guid? TemplateId);
public record SignEmrRequest(string SignatureData, string CertificateId, string SignatureAlgorithm = "SHA256withRSA");
public record EmrTemplateRequest(string Name, object ContentJson, string Speciality);

// Commands / Queries
public record GetEmrQuery(Guid EncounterId) : IRequest<Result<EmrContentResponse?>>;

public record SaveEmrDraftCommand(Guid EncounterId, EmrSaveRequest Request)
    : IRequest<Result<EmrContentResponse>>;

public record SignEmrCommand(Guid EncounterId, SignEmrRequest Request)
    : IRequest<Result<EmrContentResponse>>;

public record UnsignEmrCommand(Guid EncounterId, string Reason)
    : IRequest<Result<bool>>;

public record ExportEmrPdfCommand(Guid EncounterId)
    : IRequest<Result<byte[]>>;

public record GetEmrVersionsQuery(Guid EncounterId)
    : IRequest<Result<IReadOnlyList<EmrVersionMetaDto>>>;

public record GetEmrVersionDiffQuery(Guid EncounterId, Guid VersionId, Guid? CompareTo)
    : IRequest<Result<EmrVersionDiffDto>>;

// Template commands
public record ListEmrTemplatesQuery(string? Speciality, bool? IsSystem)
    : IRequest<Result<IReadOnlyList<EmrTemplateResponse>>>;

public record CreateEmrTemplateCommand(EmrTemplateRequest Request)
    : IRequest<Result<EmrTemplateResponse>>;

public record UpdateEmrTemplateCommand(Guid TemplateId, EmrTemplateRequest Request)
    : IRequest<Result<bool>>;

public record DeleteEmrTemplateCommand(Guid TemplateId)
    : IRequest<Result<bool>>;
