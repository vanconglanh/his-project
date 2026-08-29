using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Gui Zalo Notification Service (ZNS) qua Zalo Official Account — API chinh thuc.
/// Endpoint la URL chuan Zalo (hardcode duoc), nhung access_token doc tu config kenh da ma hoa
/// (KHONG hardcode). Config keys: access_token, template_id (mac dinh), oa_id (tuy chon).
/// Tai lieu: https://developers.zalo.me/docs/zalo-notification-service.
/// </summary>
public class ZaloZnsSender : IChannelSender
{
    public const string HttpClientName = "NotificationZaloZns";
    // URL chuan Zalo ZNS - hardcode theo tai lieu chinh thuc
    private const string SendTemplateUrl = "https://business.openapi.zalo.me/message/template";
    private const string QuotaUrl = "https://business.openapi.zalo.me/template/all?offset=0&limit=1";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ZaloZnsSender> _logger;

    public ZaloZnsSender(IHttpClientFactory httpFactory, ILogger<ZaloZnsSender> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.ZaloZns;

    public async Task<Result<NotificationSendResult>> SendAsync(
        NotificationChannelConfig config, string recipientPhone, string templateCode,
        IDictionary<string, string> templateData, CancellationToken ct = default)
    {
        var cfg = config.Config;
        if (!cfg.TryGetValue("access_token", out var accessToken) || string.IsNullOrWhiteSpace(accessToken))
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CONFIG_INVALID", "Thiếu access_token của Zalo OA.");

        // template_id: uu tien templateData -> config theo templateCode -> config mac dinh
        string? templateId = null;
        if (templateData.TryGetValue("template_id", out var tid) && !string.IsNullOrWhiteSpace(tid)) templateId = tid;
        else if (!string.IsNullOrWhiteSpace(templateCode) && cfg.TryGetValue($"template_{templateCode.ToLowerInvariant()}", out var mapped)) templateId = mapped;
        else cfg.TryGetValue("template_id", out templateId);

        if (string.IsNullOrWhiteSpace(templateId))
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CONFIG_INVALID", "Chưa cấu hình template_id cho ZNS.");

        // template_data: bo cac key dieu khien, con lai la tham so template.
        var znsData = templateData
            .Where(kv => kv.Key is not ("template_id" or "message"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var body = new
        {
            phone = NormalizePhone(recipientPhone),
            template_id = templateId,
            template_data = znsData,
            tracking_id = Guid.NewGuid().ToString("N")
        };

        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            using var req = new HttpRequestMessage(HttpMethod.Post, SendTemplateUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("access_token", accessToken);
            using var resp = await client.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            // Zalo tra HTTP 200 kem field "error" (0 = thanh cong)
            var error = ExtractInt(json, "error");
            if (resp.IsSuccessStatusCode && error == 0)
                return Result<NotificationSendResult>.Success(
                    new NotificationSendResult(true, ExtractString(json, "msg_id"), Truncate(json)));

            var errMsg = ExtractString(json, "message") ?? "không xác định";
            return Result<NotificationSendResult>.Failure("NOTIFICATION_SEND_FAILED",
                $"Zalo ZNS trả về lỗi (error={error?.ToString() ?? "?"}): {errMsg}", Truncate(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalo ZNS send loi toi {Phone}", recipientPhone);
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CONNECTION_ERROR",
                "Không kết nối được tới Zalo ZNS: " + ex.Message);
        }
    }

    public async Task<Result<bool>> TestConnectionAsync(NotificationChannelConfig config, CancellationToken ct = default)
    {
        var cfg = config.Config;
        if (!cfg.TryGetValue("access_token", out var accessToken) || string.IsNullOrWhiteSpace(accessToken))
            return Result<bool>.Failure("NOTIFICATION_CONFIG_INVALID", "Thiếu access_token của Zalo OA.");

        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            using var req = new HttpRequestMessage(HttpMethod.Get, QuotaUrl);
            req.Headers.Add("access_token", accessToken);
            using var resp = await client.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            var error = ExtractInt(json, "error");
            if (resp.IsSuccessStatusCode && error == 0)
                return Result<bool>.Success(true);

            var errMsg = ExtractString(json, "message") ?? "không xác định";
            return Result<bool>.Failure("NOTIFICATION_TEST_FAILED",
                $"access_token Zalo OA không hợp lệ (error={error?.ToString() ?? "?"}): {errMsg}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalo ZNS test connection loi");
            return Result<bool>.Failure("NOTIFICATION_CONNECTION_ERROR",
                "Không kết nối được tới Zalo ZNS: " + ex.Message);
        }
    }

    /// <summary>Zalo yeu cau so dang 84xxxxxxxxx. Chuyen 0xxxxxxxxx -> 84xxxxxxxxx.</summary>
    private static string NormalizePhone(string phone)
    {
        var p = (phone ?? "").Trim().Replace(" ", "").Replace("+", "");
        if (p.StartsWith('0')) return "84" + p[1..];
        return p;
    }

    private static string? ExtractString(string json, params string[] keys)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return Find(doc.RootElement, keys, JsonValueKind.String)?.GetString();
        }
        catch (JsonException) { return null; }
    }

    private static int? ExtractInt(string json, string key)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
                return v.GetInt32();
            return null;
        }
        catch (JsonException) { return null; }
    }

    private static JsonElement? Find(JsonElement root, string[] keys, JsonValueKind kind)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var key in keys)
            if (root.TryGetProperty(key, out var v) && v.ValueKind == kind)
                return v;
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            return Find(data, keys, kind);
        return null;
    }

    private static string Truncate(string s, int max = 500)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];
}
