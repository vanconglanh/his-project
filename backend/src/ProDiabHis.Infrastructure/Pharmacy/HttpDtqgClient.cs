using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// HTTP client thật gọi cổng Đơn thuốc Quốc gia (donthuocquocgia.vn) theo TT 27/2021/TT-BYT.
/// Bật qua cấu hình <c>Dtqg:Enabled=true</c>; nếu tắt hệ thống dùng <see cref="MockDtqgClient"/>.
///
/// Dùng named <see cref="HttpClient"/> "Dtqg" (BaseAddress/Timeout/Authorization cấu hình khi đăng ký DI),
/// mirror pattern của <c>LabPartnerHttpClient</c>. Đường dẫn + mapping trường lấy từ <see cref="DtqgOptions"/>;
/// khi có tài liệu API chính thức của ĐTQG cần chỉnh lại cho khớp.
/// </summary>
public class HttpDtqgClient : IDtqgClient
{
    public const string ClientName = "Dtqg";

    private readonly IHttpClientFactory _httpFactory;
    private readonly DtqgOptions _opt;
    private readonly IDtqgCredentialProvider _credentials;
    private readonly ILogger<HttpDtqgClient> _logger;

    public HttpDtqgClient(IHttpClientFactory httpFactory, DtqgOptions opt, IDtqgCredentialProvider credentials, ILogger<HttpDtqgClient> logger)
    {
        _httpFactory = httpFactory;
        _opt = opt;
        _credentials = credentials;
        _logger = logger;
    }

    public async Task<DtqgSubmitResult> SubmitPrescriptionAsync(DtqgSubmitPayload payload, CancellationToken ct = default)
    {
        try
        {
            var creds = await _credentials.GetForCurrentTenantAsync(ct);
            var client = _httpFactory.CreateClient(ClientName);
            ApplyTenantAuth(client, creds?.Token);
            var body = new
            {
                co_so_kham_chua_benh_id = !string.IsNullOrWhiteSpace(payload.CskcbId) ? payload.CskcbId : creds?.CskcbId,
                ma_doi_tac = !string.IsNullOrWhiteSpace(payload.PartnerCode) ? payload.PartnerCode : creds?.PartnerCode,
                don_thuoc = payload.PrescriptionData,
            };
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(_opt.SubmitPath, content, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("DTQG submit HTTP {Status} cho don thuoc {Id}", (int)resp.StatusCode, payload.PrescriptionId);
                return new DtqgSubmitResult(false, null, $"HTTP_{(int)resp.StatusCode}", Truncate(json));
            }

            var ma = ExtractString(json, "ma_don_thuoc", "maDonThuoc");
            if (string.IsNullOrWhiteSpace(ma))
                return new DtqgSubmitResult(false, null, "NO_MA_DON_THUOC", "Phan hoi DTQG khong co ma_don_thuoc");

            return new DtqgSubmitResult(true, ma, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTQG submit loi cho don thuoc {Id}", payload.PrescriptionId);
            return new DtqgSubmitResult(false, null, "DTQG_CONNECTION_ERROR", ex.Message);
        }
    }

    public async Task<DtqgStatusResult> GetStatusAsync(string maDonThuoc, CancellationToken ct = default)
    {
        try
        {
            var creds = await _credentials.GetForCurrentTenantAsync(ct);
            var client = _httpFactory.CreateClient(ClientName);
            ApplyTenantAuth(client, creds?.Token);
            var path = _opt.StatusPath.Replace("{ma}", Uri.EscapeDataString(maDonThuoc));
            using var resp = await client.GetAsync(path, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return new DtqgStatusResult("UNKNOWN", maDonThuoc, $"HTTP_{(int)resp.StatusCode}");

            var status = ExtractString(json, "trang_thai", "status") ?? "UNKNOWN";
            return new DtqgStatusResult(status, maDonThuoc, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTQG status loi cho {Ma}", maDonThuoc);
            return new DtqgStatusResult("UNKNOWN", maDonThuoc, "DTQG_CONNECTION_ERROR");
        }
    }

    public async Task<bool> CancelAsync(string maDonThuoc, string reason, CancellationToken ct = default)
    {
        try
        {
            var creds = await _credentials.GetForCurrentTenantAsync(ct);
            var client = _httpFactory.CreateClient(ClientName);
            ApplyTenantAuth(client, creds?.Token);
            var path = _opt.CancelPath.Replace("{ma}", Uri.EscapeDataString(maDonThuoc));
            using var content = new StringContent(JsonSerializer.Serialize(new { ly_do = reason }), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(path, content, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTQG cancel loi cho {Ma}", maDonThuoc);
            return false;
        }
    }

    public async Task<DtqgPingResult> PingAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var creds = await _credentials.GetForCurrentTenantAsync(ct);
            var client = _httpFactory.CreateClient(ClientName);
            ApplyTenantAuth(client, creds?.Token);
            using var resp = await client.GetAsync(_opt.PingPath, ct);
            sw.Stop();
            var text = await resp.Content.ReadAsStringAsync(ct);
            return new DtqgPingResult(resp.IsSuccessStatusCode, (int)sw.ElapsedMilliseconds, Truncate(text));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "DTQG ping loi");
            return new DtqgPingResult(false, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    /// <summary>Rút chuỗi theo tên trường (thử nhiều tên), hỗ trợ cả khi bọc trong "data".</summary>
    private static string? ExtractString(string json, params string[] keys)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FindProperty(doc.RootElement, keys);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindProperty(JsonElement root, string[] keys)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var key in keys)
        {
            if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            return FindProperty(data, keys);

        return null;
    }

    private static string Truncate(string s, int max = 500)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

    /// <summary>Đặt Bearer token per-tenant (nếu có); nếu không, giữ token cấu hình mặc định của named client.</summary>
    private static void ApplyTenantAuth(HttpClient client, string? tenantToken)
    {
        if (!string.IsNullOrWhiteSpace(tenantToken))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tenantToken);
    }
}
