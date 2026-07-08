using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.PublicApi;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProDiabHis.Infrastructure.Notifications;

/// <summary>
/// Web Push sender (RFC 8291 aes128gcm + RFC 8292 VAPID) per tenant, khong dung package ngoai.
/// Ma hoa/ky bang WebPushCrypto (da verify test vector RFC 8291).
/// </summary>
public class WebPushSenderImpl : IWebPushSender
{
    private readonly IVapidKeyService _vapidService;
    private readonly IHttpClientFactory _httpFactory;
    private readonly Application.Common.IDapperConnectionFactory _dbFactory;
    private readonly ILogger<WebPushSenderImpl> _logger;
    private readonly string _subject;

    public WebPushSenderImpl(
        IVapidKeyService vapidService,
        IHttpClientFactory httpFactory,
        Application.Common.IDapperConnectionFactory dbFactory,
        ILogger<WebPushSenderImpl> logger,
        IConfiguration configuration)
    {
        _vapidService = vapidService;
        _httpFactory = httpFactory;
        _dbFactory = dbFactory;
        _logger = logger;
        _subject = configuration["WebPush:Subject"] ?? "mailto:support@prodiab.vn";
    }

    private static string Trunc(string s) => s[..Math.Min(50, s.Length)];

    public async Task SendAsync(string endpoint, string p256dhKey, string authKey, int tenantId,
        WebPushPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var keyPair = await _vapidService.GetOrCreateKeyPairAsync(tenantId, cancellationToken);
            byte[] vapidPublicRaw = WebPushCrypto.SpkiToRawPublic(Convert.FromBase64String(keyPair.PublicKey));
            using var signingKey = ECDsa.Create();
            signingKey.ImportPkcs8PrivateKey(Convert.FromBase64String(keyPair.PrivateKey), out _);

            byte[] uaPublic = WebPushCrypto.B64UrlDecode(p256dhKey);
            byte[] authSecret = WebPushCrypto.B64UrlDecode(authKey);

            var jsonBody = JsonSerializer.Serialize(new
            {
                title = payload.Title,
                body = payload.Body,
                icon = payload.Icon,
                data = payload.Data
            });
            byte[] body = WebPushCrypto.EncryptPayload(uaPublic, authSecret, Encoding.UTF8.GetBytes(jsonBody));

            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.TryAddWithoutValidation("Authorization",
                WebPushCrypto.BuildVapidAuthHeader(endpoint, _subject, vapidPublicRaw, signingKey, DateTimeOffset.UtcNow));
            req.Headers.TryAddWithoutValidation("TTL", "86400");
            req.Content = new ByteArrayContent(body);
            req.Content.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");
            req.Content.Headers.TryAddWithoutValidation("Content-Encoding", "aes128gcm");

            var client = _httpFactory.CreateClient("WebPush");
            using var resp = await client.SendAsync(req, cancellationToken);

            if ((int)resp.StatusCode == 404 || (int)resp.StatusCode == 410)
            {
                // Subscription het han -> xoa de khong gui lai
                using var conn = _dbFactory.CreateConnection();
                await conn.ExecuteAsync(
                    "DELETE FROM diab_his_nti_web_push_subs WHERE endpoint = @Endpoint AND tenant_id = @TenantId",
                    new { Endpoint = endpoint, TenantId = tenantId });
                _logger.LogInformation("[WebPush] Subscription het han ({Status}), da xoa {Endpoint}", (int)resp.StatusCode, Trunc(endpoint));
            }
            else if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("[WebPush] {Status} khi gui toi {Endpoint}", (int)resp.StatusCode, Trunc(endpoint));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebPush send failed to {Endpoint}", Trunc(endpoint));
        }
    }

    public async Task SendToUserAsync(Guid userId, int tenantId, WebPushPayload payload,
        CancellationToken cancellationToken = default)
    {
        using var conn = _dbFactory.CreateConnection();
        var subs = await conn.QueryAsync<(string endpoint, string p256dh, string auth)>(
            @"SELECT endpoint, p256dh_key AS p256dh, auth_key AS auth
              FROM diab_his_nti_web_push_subs
              WHERE user_id = UUID_TO_BIN(@UserId) AND tenant_id = @TenantId",
            new { UserId = userId.ToString(), TenantId = tenantId });

        foreach (var sub in subs)
        {
            await SendAsync(sub.endpoint, sub.p256dh, sub.auth, tenantId, payload, cancellationToken);
        }
    }

    public async Task<bool> SendToPatientAsync(Guid patientId, int tenantId, WebPushPayload payload,
        CancellationToken cancellationToken = default)
    {
        using var conn = _dbFactory.CreateConnection();
        var subs = (await conn.QueryAsync<(string endpoint, string p256dh, string auth)>(
            @"SELECT endpoint, p256dh_key AS p256dh, auth_key AS auth
              FROM diab_his_nti_web_push_subs
              WHERE patient_id = @PatientId AND tenant_id = @TenantId",
            new { PatientId = patientId.ToString(), TenantId = tenantId })).ToList();

        if (subs.Count == 0) return false;

        foreach (var sub in subs)
        {
            await SendAsync(sub.endpoint, sub.p256dh, sub.auth, tenantId, payload, cancellationToken);
        }

        return true;
    }
}
