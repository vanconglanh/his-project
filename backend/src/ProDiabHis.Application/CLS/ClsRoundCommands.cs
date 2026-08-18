using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.CLS;

// ── Commands ──
public record CreateClsRoundCommand(Guid EncounterId, CreateClsRoundRequest Request)
    : IRequest<Result<ClsRoundResponse>>;

public record SubmitClsRoundCommand(Guid RoundId) : IRequest<Result<ClsRoundResponse>>;

public record PayClsRoundCommand(Guid RoundId, PayClsRoundRequest Request) : IRequest<Result<ClsRoundResponse>>;

public record WaiveClsRoundCommand(Guid RoundId, WaiveClsRoundRequest Request) : IRequest<Result<ClsRoundResponse>>;

public record CancelClsRoundCommand(Guid RoundId, string? Reason) : IRequest<Result<ClsRoundResponse>>;

// ── Queries ──
public record ListClsRoundsQuery(Guid EncounterId, string? Status) : IRequest<Result<ClsRoundListResponse>>;

public record GetClsRoundQuery(Guid RoundId) : IRequest<Result<ClsRoundResponse>>;
