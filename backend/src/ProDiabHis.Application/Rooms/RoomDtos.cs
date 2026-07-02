namespace ProDiabHis.Application.Rooms;

public record RoomAdminResponse(
    string Id,
    int? TenantId,
    string RoomCode,
    string Name,
    int MaxPerDay,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateRoomRequest(
    string RoomCode,
    string Name,
    int MaxPerDay = 40,
    bool IsActive = true);

public record UpdateRoomRequest(
    string? RoomCode,
    string? Name,
    int? MaxPerDay,
    bool? IsActive);
