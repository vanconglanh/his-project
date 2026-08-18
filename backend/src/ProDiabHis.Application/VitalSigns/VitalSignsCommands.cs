using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.VitalSigns;

public record CreateVitalSignsCommand(Guid EncounterId, VitalSignsRequest Request)
    : IRequest<Result<VitalSignsResponse>>, IEncounterScopedCommand;

public record BatchCreateVitalSignsCommand(Guid EncounterId, IReadOnlyList<VitalSignsRequest> Records)
    : IRequest<Result<IReadOnlyList<VitalSignsResponse>>>, IEncounterScopedCommand;

public record UpdateVitalSignsCommand(Guid VitalSignId, VitalSignsRequest Request)
    : IRequest<Result<VitalSignsResponse>>, IEncounterChildScopedCommand
{
    public Guid ChildId => VitalSignId;
    public string ChildKind => EncounterChildKind.VitalSigns;
}

public record DeleteVitalSignsCommand(Guid VitalSignId)
    : IRequest<Result<bool>>, IEncounterChildScopedCommand
{
    public Guid ChildId => VitalSignId;
    public string ChildKind => EncounterChildKind.VitalSigns;
}

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
