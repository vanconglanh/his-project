namespace ProDiabHis.Application.Branches;

public record BranchDto(
    int Id,
    int TenantId,
    string Code,
    string Name,
    string? CskcbCode,
    string? Address,
    string? Phone,
    string? Email,
    string? WorkingHours,
    string Timezone,
    bool IsActive,
    bool IsDefault,
    int SortOrder,
    int UserCount,
    string Status,
    string? HospitalRank,
    string? KcbTuyen,
    string? BhytContractCode,
    DateTime? BhytContractValidFrom,
    DateTime? BhytContractValidTo,
    bool BhytEnabled,
    bool DtqgEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateBranchRequest(
    string Code,
    string Name,
    string? CskcbCode,
    string? Address,
    string? Phone,
    string? Email,
    string? WorkingHours,
    string? Timezone,
    bool IsActive = true,
    int SortOrder = 0,
    string? HospitalRank = null,
    string? KcbTuyen = null,
    string? BhytContractCode = null,
    DateTime? BhytContractValidFrom = null,
    DateTime? BhytContractValidTo = null,
    bool BhytEnabled = false,
    bool DtqgEnabled = false);

public record UpdateBranchRequest(
    string? Code,
    string? Name,
    string? CskcbCode,
    string? Address,
    string? Phone,
    string? Email,
    string? WorkingHours,
    string? Timezone,
    int? SortOrder,
    string? HospitalRank = null,
    string? KcbTuyen = null,
    string? BhytContractCode = null,
    DateTime? BhytContractValidFrom = null,
    DateTime? BhytContractValidTo = null,
    bool? BhytEnabled = null,
    bool? DtqgEnabled = null);

public record AssignUsersToBranchRequest(List<Guid> UserIds, bool? IsPrimary);

// BUG FIX: cung loi Dapper positional-record nhu PrintHistoryItem
// (loi 500 GET /branches/{id}/users) -> doi sang class + property setter.
public class UserBranchDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public record BranchContextResponse(
    int CurrentBranchId,
    List<BranchOptionDto> Branches,
    bool CanCrossView);

public record BranchOptionDto(int Id, string Code, string Name, bool IsDefault);

// ─── NV1: BHYT compliance theo chi nhanh (BR-100..108) ─────────────────────────

public record BranchBhytComplianceDto(
    int BranchId,
    string Name,
    bool HasCskcb,
    bool BhytEnabled,
    bool BhytContractValid,
    bool DtqgConnected,
    bool DtqgTokenValid,
    string? LastBhytExportPeriod);

// ─── NV2: Clone chi nhanh + checklist go-live (BR-110/111/112) ─────────────────

public record CloneBranchRequest(
    int SourceBranchId,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? Timezone,
    int? GroupId);

public record ReadinessItemDto(string Key, string Label, bool Passed, string Detail);

public record BranchReadinessDto(int BranchId, bool AllPassed, List<ReadinessItemDto> Items);
