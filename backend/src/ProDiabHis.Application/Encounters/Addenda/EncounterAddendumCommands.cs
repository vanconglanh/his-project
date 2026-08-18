using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Encounters.Addenda;

// ────────────────────────────────────────────────
// [G03] Ban dinh chinh benh an da khoa
// ────────────────────────────────────────────────

/// <param name="Section">DIAGNOSIS | CLINICAL_NOTE | PRESCRIPTION | VITAL_SIGN | CLS_ORDER | OTHER</param>
/// <param name="Operation">UPDATE | ADD | REMOVE</param>
/// <param name="ContentAfter">Noi dung sau dinh chinh (JSON tu do). contentBefore do SERVER tu snapshot.</param>
public record CreateAddendumRequest(
    string Section,
    string? Operation,
    string? TargetTable,
    string? TargetId,
    object? ContentAfter,
    string Reason,
    bool AcknowledgeBhytResubmit = false);

/// <summary>Tao ban dinh chinh — bypass guard vi day chinh la duong sua hop phap sau khi khoa.</summary>
public record CreateEncounterAddendumCommand(Guid EncounterId, CreateAddendumRequest Request)
    : IRequest<Result<EncounterAddendumResponse>>, IBypassEncounterLock;

public record ListEncounterAddendaQuery(Guid EncounterId, string? Section, int Page, int PageSize)
    : IRequest<Result<PagedResult<EncounterAddendumResponse>>>;

public record GetEncounterLockStateQuery(Guid EncounterId)
    : IRequest<Result<EncounterLockInfo>>;

public record AddendumActorDto(Guid? UserId, string? FullName);

public record EncounterAddendumResponse(
    Guid Id,
    Guid EncounterId,
    string Section,
    string Operation,
    string? TargetTable,
    string? TargetId,
    string? ContentBefore,
    string? ContentAfter,
    string Reason,
    DateTime CreatedAt,
    AddendumActorDto CreatedBy,
    bool BhytResubmitRequired,
    string? AuditLogId);
