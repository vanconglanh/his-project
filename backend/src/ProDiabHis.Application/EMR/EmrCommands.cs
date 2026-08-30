using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.EMR;

// Requests
// §5.8.1 (QD4) — StructuredValues: gia tri form {key: value} theo structured_json cua template dang chon.
public record EmrSaveRequest(object ContentJson, string? ContentHtml, Guid? TemplateId, object? StructuredValues = null);
public record SignEmrRequest(string SignatureData, string CertificateId, string SignatureAlgorithm = "SHA256withRSA");
// §5.7.2 — StructuredJson: DINH NGHIA form (mang field). IsDefault: mau mac dinh theo speciality.
public record EmrTemplateRequest(string Name, object ContentJson, string Speciality, object? StructuredJson = null, bool IsDefault = false);

// Commands / Queries
public record GetEmrQuery(Guid EncounterId) : IRequest<Result<EmrContentResponse?>>;

public record SaveEmrDraftCommand(Guid EncounterId, EmrSaveRequest Request)
    : IRequest<Result<EmrContentResponse>>, IEncounterScopedCommand;

public record SignEmrCommand(Guid EncounterId, SignEmrRequest Request)
    : IRequest<Result<EmrContentResponse>>, IEncounterScopedCommand;

public record UnsignEmrCommand(Guid EncounterId, string Reason)
    : IRequest<Result<bool>>, IEncounterScopedCommand;

public record ExportEmrPdfCommand(Guid EncounterId)
    : IRequest<Result<byte[]>>;

public record GetEmrVersionsQuery(Guid EncounterId)
    : IRequest<Result<IReadOnlyList<EmrVersionMetaDto>>>;

public record GetEmrVersionDiffQuery(Guid EncounterId, Guid VersionId, Guid? CompareTo)
    : IRequest<Result<EmrVersionDiffDto>>;

// Template commands
// §5.7.3 — loc theo speciality + (tuy chon) goi benh nhan dang dung (PackageId qua bang noi).
public record ListEmrTemplatesQuery(string? Speciality, bool? IsSystem, Guid? PackageId = null)
    : IRequest<Result<IReadOnlyList<EmrTemplateResponse>>>;

public record GetEmrTemplateQuery(Guid TemplateId)
    : IRequest<Result<EmrTemplateResponse?>>;

public record CreateEmrTemplateCommand(EmrTemplateRequest Request)
    : IRequest<Result<EmrTemplateResponse>>;

public record UpdateEmrTemplateCommand(Guid TemplateId, EmrTemplateRequest Request)
    : IRequest<Result<bool>>;

public record DeleteEmrTemplateCommand(Guid TemplateId)
    : IRequest<Result<bool>>;
