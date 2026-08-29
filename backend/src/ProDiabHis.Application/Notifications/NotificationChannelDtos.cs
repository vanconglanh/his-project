namespace ProDiabHis.Application.Notifications;

/// <summary>
/// Body tao/cap nhat 1 kenh thong bao. <see cref="Config"/> chua cac cap key-value nhay cam
/// (api_key, secret, access_token, template_id...) -> se duoc ma hoa AES-256-GCM truoc khi luu.
/// </summary>
public record NotificationChannelRequest(
    string Channel,                       // "SMS" | "ZALO_ZNS"
    string Provider,                      // "ESMS" | "ZALO_OA"
    Dictionary<string, string> Config,
    bool IsActive);

/// <summary>
/// Thong tin kenh tra ve API. <see cref="ConfigMasked"/> da che cac gia tri nhay cam
/// (chi lo 4 ky tu cuoi) - KHONG bao gio tra config goc ra ngoai.
/// </summary>
public record NotificationChannelResponse(
    string Id,
    int TenantId,
    int? BranchId,
    string Channel,
    string Provider,
    Dictionary<string, string> ConfigMasked,
    bool IsActive,
    DateTime? LastTestedAt,
    bool LastTestOk,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Ket qua nut "Test ket noi".</summary>
public record NotificationChannelTestResult(bool Ok, string? Message);
