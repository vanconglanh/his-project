using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Diabetes.Cgm;

namespace ProDiabHis.Infrastructure.Integrations.Cgm;

/// <summary>
/// Cấu hình kết nối Dexcom Developer API. Đọc từ appsettings: "CgmProvider:Dexcom:*".
/// TODO: xác nhận endpoint chính xác khi có tài khoản Dexcom Developer (sandbox).
/// </summary>
public class DexcomCgmOptions
{
    public const string SectionName = "CgmProvider:Dexcom";

    /// <summary>Base URL Dexcom API. Dexcom công bố 2 môi trường: sandbox (api-sandbox.dexcom.com) và
    /// production (api.dexcom.com) — TODO: xác nhận domain/version chính xác khi đăng ký Dexcom Developer.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>OAuth2 client_id cấp khi đăng ký ứng dụng trên Dexcom Developer Portal.</summary>
    public string? ClientId { get; set; }

    /// <summary>OAuth2 client_secret cấp khi đăng ký ứng dụng trên Dexcom Developer Portal.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Redirect URI đã đăng ký với Dexcom cho luồng authorization code (Portal FE).</summary>
    public string? RedirectUri { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Adapter gọi Dexcom Developer API (theo pattern OAuth2 Authorization Code Flow công khai phổ biến của
/// Dexcom: FE đưa bệnh nhân qua trang đăng nhập Dexcom → nhận `code` qua redirect → BE đổi `code` lấy
/// access_token/refresh_token qua endpoint `/v2/oauth2/token` → dùng access_token gọi API dữ liệu EGV
/// "Estimated Glucose Value").
///
/// CHƯA có sandbox/tài khoản Dexcom Developer thật tại thời điểm implement — code chỉ dựng đúng SHAPE
/// (request/response DTO, luồng gọi HttpClient) theo chuẩn OAuth2 Authorization Code + REST, KHÔNG bịa
/// endpoint cụ thể ngoài những gì đã công bố công khai trong tài liệu Dexcom Developer. Mọi endpoint path
/// dưới đây là placeholder và PHẢI được xác nhận lại với Dexcom trước khi go-live (xem TODO rải rác).
///
/// Nếu ClientId/ClientSecret chưa được cấu hình (CgmProvider:Dexcom:ClientId/ClientSecret), mọi lời gọi
/// sẽ throw NotImplementedException để tránh gọi nhầm ra ngoài khi chưa sẵn sàng.
/// </summary>
public class DexcomCgmProvider : ICgmDeviceProvider
{
    public const string HttpClientName = "DexcomCgmClient";

    public string ProviderCode => "Dexcom";

    private readonly HttpClient _httpClient;
    private readonly DexcomCgmOptions _options;
    private readonly ILogger<DexcomCgmProvider> _logger;

    public DexcomCgmProvider(HttpClient httpClient, DexcomCgmOptions options, ILogger<DexcomCgmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new NotImplementedException(
                "DexcomCgmProvider chua duoc cau hinh (CgmProvider:Dexcom:ClientId/ClientSecret trong "
                + "appsettings). Day la adapter cho Dexcom Developer API that - chua co sandbox/tai khoan "
                + "developer nen chua the goi API thuc te. Neu dang o Development/Testing, hay dat "
                + "CgmProvider:Type=None.");
        }
    }

    public async Task<CgmLinkResult> LinkPatientAccountAsync(string patientExternalId, string authCode, CancellationToken ct = default)
    {
        EnsureConfigured();

        // TODO: xac nhan endpoint chinh xac voi Dexcom khi co tai khoan Dexcom Developer.
        // Pattern OAuth2 Authorization Code chuan cua Dexcom: POST /v2/oauth2/token
        // body (application/x-www-form-urlencoded): client_id, client_secret, code, grant_type=authorization_code, redirect_uri
        var path = "/v2/oauth2/token";
        _logger.LogInformation("[DEXCOM_CGM] LinkPatientAccountAsync path={Path}", path);

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["code"] = authCode,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri ?? string.Empty,
        };

        using var response = await _httpClient.PostAsync(path, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[DEXCOM_CGM] Doi authCode lay token that bai. Status={Status} Body={Body}", response.StatusCode, body);
            return new CgmLinkResult(false, null, null, null, null, "CGM_PROVIDER_ERROR",
                $"Dexcom tra ve loi: {response.StatusCode}");
        }

        var dto = await response.Content.ReadFromJsonAsync<DexcomTokenResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Dexcom tra ve response rong khi doi authCode.");

        // TODO: Dexcom OAuth2 token response khong tra ve "external account id" truc tiep — theo tai lieu
        // cong khai thuong phai goi them 1 endpoint kieu /v3/users/self (hoac tuong duong) de lay danh tinh
        // benh nhan. Chua xac nhan endpoint chinh xac -> tam thoi dung patientExternalId dau vao (neu co)
        // hoac rong, PHAI bo sung khi co sandbox.
        var externalAccountId = string.IsNullOrWhiteSpace(patientExternalId) ? "PENDING_DEXCOM_USER_LOOKUP" : patientExternalId;

        return new CgmLinkResult(
            Success: true,
            ExternalAccountId: externalAccountId,
            AccessToken: dto.AccessToken,
            RefreshToken: dto.RefreshToken,
            ExpiresAt: DateTime.UtcNow.AddSeconds(dto.ExpiresIn));
    }

    public async Task<IReadOnlyList<CgmReading>> FetchReadingsAsync(
        string linkedAccountId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        EnsureConfigured();

        // TODO: xac nhan endpoint chinh xac voi Dexcom khi co tai khoan Dexcom Developer.
        // Pattern du lieu do duong huyet lien tuc (EGV) cong khai cua Dexcom: GET /v3/users/self/egvs
        //   query: startDate, endDate (ISO 8601, khong co timezone offset theo tai lieu cong khai)
        //   Authorization: Bearer {access_token}
        var startDate = fromUtc.ToString("yyyy-MM-ddTHH:mm:ss");
        var endDate = toUtc.ToString("yyyy-MM-ddTHH:mm:ss");
        var path = $"/v3/users/self/egvs?startDate={Uri.EscapeDataString(startDate)}&endDate={Uri.EscapeDataString(endDate)}";

        _logger.LogInformation("[DEXCOM_CGM] FetchReadingsAsync linkedAccountId={AccountId} path={Path}", linkedAccountId, path);

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        // access_token cua benh nhan duoc caller gan vao DefaultRequestHeaders truoc khi goi (xem CgmSyncJob) —
        // giu HttpClient dung chung, khong luu token o day.
        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[DEXCOM_CGM] FetchReadings that bai. Status={Status} Body={Body}", response.StatusCode, body);
            return Array.Empty<CgmReading>();
        }

        var dto = await response.Content.ReadFromJsonAsync<DexcomEgvResponseDto>(cancellationToken: ct);
        if (dto?.Records is null || dto.Records.Count == 0)
            return Array.Empty<CgmReading>();

        return dto.Records
            .Select(r => new CgmReading(
                Timestamp: r.SystemTime,
                GlucoseValueMgDl: r.Value,
                TrendDirection: NormalizeTrend(r.Trend),
                DeviceId: r.TransmitterId))
            .ToList();
    }

    /// <summary>Chuẩn hoá trend Dexcom (doubleUp/singleUp/flat/singleDown/doubleDown/...) về whitelist chung.</summary>
    private static string? NormalizeTrend(string? dexcomTrend) => dexcomTrend?.ToLowerInvariant() switch
    {
        "doubleup" or "singleup" => "rising_rapidly",
        "fortyfiveup" => "rising",
        "flat" => "flat",
        "fortyfivedown" => "falling",
        "singledown" or "doubledown" => "falling_rapidly",
        "notcomputable" => "not_computable",
        null => null,
        _ => "unknown"
    };
}

// ── DTO shape theo pattern chuẩn OAuth2 + tài liệu công khai Dexcom (chưa xác nhận field name thật) ──
internal record DexcomTokenResponseDto(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string? TokenType);

internal record DexcomEgvResponseDto(
    [property: JsonPropertyName("records")] List<DexcomEgvRecordDto>? Records);

internal record DexcomEgvRecordDto(
    [property: JsonPropertyName("systemTime")] DateTime SystemTime,
    [property: JsonPropertyName("value")] decimal Value,
    [property: JsonPropertyName("trend")] string? Trend,
    [property: JsonPropertyName("transmitterId")] string? TransmitterId);
