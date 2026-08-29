using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Gui SMS qua nha cung cap eSMS (esms.vn) — API cong khai. Credential (ApiKey/SecretKey/Brandname)
/// KHONG hardcode, doc tu config kenh da ma hoa (<see cref="NotificationChannelConfig.Config"/>).
/// Config keys: api_key, secret_key, brand_name, sms_type (mac dinh "2"), endpoint (tuy chon override).
/// Tai lieu: https://developers.esms.vn (SendMultipleMessage_V4, GetBalance).
/// </summary>
public class SmsSender : IChannelSender
{
    public const string HttpClientName = "NotificationSms";
    private const string DefaultSendUrl = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";
    private const string BalanceUrlFmt = "https://rest.esms.vn/MainService.svc/json/GetBalance/{0}/{1}";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IHttpClientFactory httpFactory, ILogger<SmsSender> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public async Task<Result<NotificationSendResult>> SendAsync(
        NotificationChannelConfig config, string recipientPhone, string templateCode,
        IDictionary<string, string> templateData, CancellationToken ct = default)
    {
        var cfg = config.Config;
        if (!cfg.TryGetValue("api_key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey) ||
            !cfg.TryGetValue("secret_key", out var secretKey) || string.IsNullOrWhiteSpace(secretKey))
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CONFIG_INVALID", "Thiếu ApiKey/SecretKey của nhà cung cấp SMS.");

        cfg.TryGetValue("brand_name", out var brandName);
        cfg.TryGetValue("sms_type", out var smsType);
        cfg.TryGetValue("endpoint", out var endpoint);

        // Noi dung: uu tien key "message" da render san; neu khong, ghep tu template_data.
        var content = templateData.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg)
            ? msg
            : string.Join(" ", templateData.Values);

        var body = new
        {
            ApiKey = apiKey,
            SecretKey = secretKey,
            Content = content,
            Phone = recipientPhone,
            Brandname = brandName,
            SmsType = string.IsNullOrWhiteSpace(smsType) ? "2" : smsType,
            IsUnicode = "1"
        };

        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            using var httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(
                string.IsNullOrWhiteSpace(endpoint) ? DefaultSendUrl : endpoint, httpContent, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return Result<NotificationSendResult>.Failure("NOTIFICATION_SEND_FAILED",
                    $"Gửi SMS thất bại (HTTP {(int)resp.StatusCode}).", Truncate(json));

            var code = ExtractString(json, "CodeResult", "CodeResponse");
            // eSMS: CodeResult "100" = thanh cong
            if (code == "100")
                return Result<NotificationSendResult>.Success(
                    new NotificationSendResult(true, ExtractString(json, "SMSID"), Truncate(json)));

            return Result<NotificationSendResult>.Failure("NOTIFICATION_SEND_FAILED",
                $"Nhà cung cấp SMS trả về mã lỗi {code ?? "không xác định"}.", Truncate(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send loi toi {Phone}", recipientPhone);
            return Result<NotificationSendResult>.Failure("NOTIFICATION_CONNECTION_ERROR",
                "Không kết nối được tới nhà cung cấp SMS: " + ex.Message);
        }
    }

    public async Task<Result<bool>> TestConnectionAsync(NotificationChannelConfig config, CancellationToken ct = default)
    {
        var cfg = config.Config;
        if (!cfg.TryGetValue("api_key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey) ||
            !cfg.TryGetValue("secret_key", out var secretKey) || string.IsNullOrWhiteSpace(secretKey))
            return Result<bool>.Failure("NOTIFICATION_CONFIG_INVALID", "Thiếu ApiKey/SecretKey của nhà cung cấp SMS.");

        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            var url = string.Format(BalanceUrlFmt, Uri.EscapeDataString(apiKey), Uri.EscapeDataString(secretKey));
            using var resp = await client.GetAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return Result<bool>.Failure("NOTIFICATION_TEST_FAILED", $"Kiểm tra thất bại (HTTP {(int)resp.StatusCode}).");

            var code = ExtractString(json, "CodeResponse", "CodeResult");
            if (code == "100")
                return Result<bool>.Success(true);

            return Result<bool>.Failure("NOTIFICATION_TEST_FAILED",
                $"ApiKey/SecretKey không hợp lệ (mã {code ?? "không xác định"}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS test connection loi");
            return Result<bool>.Failure("NOTIFICATION_CONNECTION_ERROR",
                "Không kết nối được tới nhà cung cấp SMS: " + ex.Message);
        }
    }

    private static string? ExtractString(string json, params string[] keys)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var key in keys)
                if (doc.RootElement.TryGetProperty(key, out var v))
                    return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
            return null;
        }
        catch (JsonException) { return null; }
    }

    private static string Truncate(string s, int max = 500)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];
}
