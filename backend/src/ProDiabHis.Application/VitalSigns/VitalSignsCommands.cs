using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.VitalSigns;

public record CreateVitalSignsCommand(Guid EncounterId, VitalSignsRequest Request)
    : IRequest<Result<VitalSignsResponse>>;

public record BatchCreateVitalSignsCommand(Guid EncounterId, IReadOnlyList<VitalSignsRequest> Records)
    : IRequest<Result<IReadOnlyList<VitalSignsResponse>>>;

public record UpdateVitalSignsCommand(Guid VitalSignId, VitalSignsRequest Request)
    : IRequest<Result<VitalSignsResponse>>;

public record DeleteVitalSignsCommand(Guid VitalSignId)
    : IRequest<Result<bool>>;

public record ListVitalSignsByEncounterQuery(Guid EncounterId)
    : IRequest<Result<IReadOnlyList<VitalSignsResponse>>>;

public record GetLatestVitalSignsQuery(Guid EncounterId)
    : IRequest<Result<VitalSignsResponse?>>;

public record GetVitalSignsHistoryQuery(
    Guid PatientId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Metric)
    : IRequest<Result<IReadOnlyList<VitalSignsResponse>>>;
