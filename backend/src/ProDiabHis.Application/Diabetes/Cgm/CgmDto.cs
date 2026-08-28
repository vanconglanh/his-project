namespace ProDiabHis.Application.Diabetes.Cgm;

/// <summary>Request Portal - bệnh nhân tự liên kết tài khoản CGM (OAuth2 authorization code).</summary>
/// <param name="Provider">Mã nhà cung cấp CGM: "Dexcom" (hiện chỉ hỗ trợ Dexcom, xem CgmProvider:Type).</param>
/// <param name="AuthCode">Authorization code do nền tảng CGM cấp sau khi bệnh nhân đồng ý cấp quyền.</param>
public record CgmLinkRequest(string Provider, string AuthCode);

/// <summary>Response sau khi liên kết tài khoản CGM thành công.</summary>
public record CgmLinkResponse(bool Success, string Provider, string? ExternalAccountId, DateTime? TokenExpiresAt);

/// <summary>Trạng thái liên kết CGM của 1 bệnh nhân — dùng cho bác sĩ xem (GET /patients/{id}/cgm-status).</summary>
/// <param name="Linked">Bệnh nhân đã liên kết ít nhất 1 nhà cung cấp CGM đang ACTIVE.</param>
/// <param name="Provider">Nhà cung cấp CGM đã liên kết (null nếu chưa liên kết).</param>
/// <param name="Status">ACTIVE|REVOKED|EXPIRED|ERROR|null.</param>
/// <param name="LinkedAt">Thời điểm liên kết.</param>
/// <param name="LastSyncedAt">Lần đồng bộ dữ liệu CGM gần nhất.</param>
/// <param name="LastSyncError">Lỗi đồng bộ gần nhất (nếu có).</param>
public record CgmStatusResponse(
    bool Linked,
    string? Provider,
    string? Status,
    DateTime? LinkedAt,
    DateTime? LastSyncedAt,
    string? LastSyncError);
