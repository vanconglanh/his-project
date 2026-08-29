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
    int SortOrder = 0);

public record UpdateBranchRequest(
    string? Code,
    string? Name,
    string? CskcbCode,
    string? Address,
    string? Phone,
    string? Email,
    string? WorkingHours,
    string? Timezone,
    int? SortOrder);

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
