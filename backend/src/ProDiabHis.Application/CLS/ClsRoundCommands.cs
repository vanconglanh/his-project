using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.CLS;

// ── Commands ──
public record CreateClsRoundCommand(Guid EncounterId, CreateClsRoundRequest Request)
    : IRequest<Result<ClsRoundResponse>>, IEncounterScopedCommand;

public record SubmitClsRoundCommand(Guid RoundId) : IRequest<Result<ClsRoundResponse>>;

public record PayClsRoundCommand(Guid RoundId, PayClsRoundRequest Request) : IRequest<Result<ClsRoundResponse>>;

public record WaiveClsRoundCommand(Guid RoundId, WaiveClsRoundRequest Request) : IRequest<Result<ClsRoundResponse>>;

public record CancelClsRoundCommand(Guid RoundId, string? Reason) : IRequest<Result<ClsRoundResponse>>;

// BM-18: dinh nghia dat trong ClsRoundHandlers.cs (canh handler cua no) - tham chieu o day
// de nguoi doc file nay thay day du danh sach command cua module cls-round.

// ── Queries ──
public record ListClsRoundsQuery(Guid EncounterId, string? Status) : IRequest<Result<ClsRoundListResponse>>;

public record GetClsRoundQuery(Guid RoundId) : IRequest<Result<ClsRoundResponse>>;
