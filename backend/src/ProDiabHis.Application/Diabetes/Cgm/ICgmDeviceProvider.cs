namespace ProDiabHis.Application.Diabetes.Cgm;

/// <summary>
/// FR-711 [P2]: Adapter chuẩn hoá kết nối thiết bị đo đường huyết liên tục (CGM — Continuous Glucose
/// Monitor: Dexcom, Abbott LibreView/FreeStyle Libre...) qua API của hãng.
///
/// Mỗi hãng CGM implement 1 provider riêng (vd <c>DexcomCgmProvider</c>) theo pattern adapter đã dùng
/// cho ký số CA (<c>IDigitalSignatureProvider</c> / <c>VnptSmartCaSignatureProvider</c>, xem FR-302/402)
/// và Docosan telehealth: dựng đúng SHAPE luồng OAuth2 authorization code + REST, KHÔNG bịa endpoint cụ
/// thể khi chưa có sandbox/tài khoản developer thật.
/// </summary>
public interface ICgmDeviceProvider
{
    /// <summary>Mã định danh provider dùng để lưu cột <c>provider</c> trong diab_his_dev_cgm_links (vd "Dexcom").</summary>
    string ProviderCode { get; }

    /// <summary>
    /// Liên kết tài khoản bệnh nhân trên nền tảng CGM theo luồng OAuth2 Authorization Code:
    /// bệnh nhân đã đăng nhập trên nền tảng CGM và đồng ý cấp quyền ở FE (Portal) → FE nhận về
    /// <paramref name="authCode"/> → BE đổi code lấy access_token/refresh_token.
    /// </summary>
    /// <param name="patientExternalId">
    /// Định danh bệnh nhân phía nền tảng CGM nếu đã biết trước (thường rỗng ở lần liên kết đầu — nền
    /// tảng CGM sẽ trả về trong <see cref="CgmLinkResult"/> sau khi đổi authCode thành công).
    /// </param>
    /// <param name="authCode">Authorization code do nền tảng CGM cấp sau khi bệnh nhân đồng ý (OAuth2 redirect).</param>
    Task<CgmLinkResult> LinkPatientAccountAsync(string patientExternalId, string authCode, CancellationToken ct = default);

    /// <summary>Lấy dữ liệu đo đường huyết trong khoảng thời gian [fromUtc, toUtc] cho tài khoản đã liên kết.</summary>
    /// <param name="linkedAccountId">external_account_id đã lưu ở diab_his_dev_cgm_links sau khi liên kết.</param>
    Task<IReadOnlyList<CgmReading>> FetchReadingsAsync(
        string linkedAccountId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}

/// <summary>Kết quả liên kết tài khoản bệnh nhân với nền tảng CGM (đổi authorization code lấy token).</summary>
/// <param name="Success">Liên kết thành công hay không.</param>
/// <param name="ExternalAccountId">ID/username tài khoản bệnh nhân phía nền tảng CGM (lưu external_account_id).</param>
/// <param name="AccessToken">Access token OAuth2 — caller chịu trách nhiệm mã hoá (AES-256-GCM) trước khi lưu DB.</param>
/// <param name="RefreshToken">Refresh token OAuth2 (nếu nền tảng hỗ trợ) — caller mã hoá trước khi lưu DB.</param>
/// <param name="ExpiresAt">Thời điểm access token hết hạn.</param>
/// <param name="ErrorCode">Mã lỗi (SCREAMING_SNAKE) khi Success=false.</param>
/// <param name="ErrorMessage">Thông báo lỗi (tiếng Việt) khi Success=false.</param>
public record CgmLinkResult(
    bool Success,
    string? ExternalAccountId,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? ErrorCode = null,
    string? ErrorMessage = null);

/// <summary>Một mẫu đo đường huyết trả về từ API của nền tảng CGM (đã chuẩn hoá).</summary>
/// <param name="Timestamp">Thời điểm đo (UTC, theo đồng hồ thiết bị).</param>
/// <param name="GlucoseValueMgDl">Giá trị đường huyết, đơn vị mg/dL.</param>
/// <param name="TrendDirection">Xu hướng đã chuẩn hoá: flat|rising|rising_rapidly|falling|falling_rapidly|not_computable|unknown.</param>
/// <param name="DeviceId">ID thiết bị (sensor/transmitter) — dùng làm 1 phần khoá idempotency khi ghi DB.</param>
public record CgmReading(
    DateTime Timestamp,
    decimal GlucoseValueMgDl,
    string? TrendDirection,
    string? DeviceId);
