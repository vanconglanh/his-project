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

// ═══════════════════════════════════════════════
// FR-711: Dong bo (push) du lieu do lien tuc CGM tu thiet bi/portal — bo sung POST /cgm/sync
// canh voi CgmReadingsSyncJob (pull dinh ky). Dung khi thiet bi/app cua benh nhan chu dong day
// du lieu ve (webhook/portal) thay vi cho job poll theo lich.
// ═══════════════════════════════════════════════

/// <summary>1 ban ghi do duong huyet trong batch dong bo tu thiet bi/portal.</summary>
/// <param name="Timestamp">Thời điểm đo (UTC, theo đồng hồ thiết bị).</param>
/// <param name="GlucoseValueMgDl">Giá trị đường huyết, đơn vị mg/dL.</param>
/// <param name="TrendDirection">Xu hướng: flat|rising|rising_rapidly|falling|falling_rapidly|not_computable|unknown.</param>
/// <param name="DeviceId">ID thiết bị (sensor/transmitter) — 1 phần khóa idempotency khi ghi DB.</param>
public record CgmSyncReadingItem(DateTime Timestamp, decimal GlucoseValueMgDl, string? TrendDirection, string? DeviceId);

/// <summary>Request đồng bộ batch dữ liệu CGM (POST /api/v1/portal/cgm/sync).</summary>
/// <param name="Provider">Mã nhà cung cấp CGM đã liên kết (vd "Dexcom") — phải khớp liên kết ACTIVE của bệnh nhân.</param>
/// <param name="Readings">Danh sách bản ghi đo trong batch.</param>
public record CgmSyncRequest(string Provider, IReadOnlyList<CgmSyncReadingItem> Readings);

/// <summary>Kết quả đồng bộ batch — tổng số nhận, số bản ghi mới (sau idempotency), số bị bỏ qua.</summary>
public record CgmSyncResponse(int Received, int Inserted, int Skipped, DateTime SyncedAt);
