using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Telehealth.Integration;

namespace ProDiabHis.Infrastructure.Integrations.Docosan;

/// <summary>
/// HTTP client thuc su goi REST API Docosan. Dung named <see cref="HttpClient"/> "Docosan"
/// (BaseAddress/Timeout cau hinh khi dang ky DI). Retry thu cong (khong dung Polly de tranh
/// them dependency) — 3 lan, backoff 1s/3s/9s, chi retry loi mang/5xx (khong retry 4xx).
/// KHONG log Authorization / x-api-key / appointment_link / access_token (xem muc 5.2 thiet ke).
/// </summary>
public class HttpDocosanClient : IDocosanClient
{
    public const string ClientName = "Docosan";

    private readonly IHttpClientFactory _httpFactory;
    private readonly DocosanOptions _opt;
    private readonly ILogger<HttpDocosanClient> _logger;

    public HttpDocosanClient(IHttpClientFactory httpFactory, DocosanOptions opt, ILogger<HttpDocosanClient> logger)
    { _httpFactory = httpFactory; _opt = opt; _logger = logger; }

    public async Task<bool> IsUserExistAsync(string phoneNumber, CancellationToken ct)
    {
        var client = CreateClient(null);
        var resp = await SendWithRetryAsync(() =>
            client.GetAsync($"api/is-exist-user?phone_number={Uri.EscapeDataString(phoneNumber)}", ct), ct);
        if (resp is null || !resp.IsSuccessStatusCode) return false;
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return TryGetBool(doc.RootElement, "data") ?? false;
    }

    public async Task<DocosanRegisterResultDto> RegisterInternalUserAsync(DocosanRegisterUserRequest req, CancellationToken ct)
    {
        var client = CreateClient(null);
        var form = new Dictionary<string, string>
        {
            ["type"] = req.Type,
            ["display_name"] = req.DisplayName,
            ["language"] = req.Language,
            ["phone_number"] = req.PhoneNumber,
            ["is_get_cares_order_info"] = req.IsGetCaresOrderInfo ? "1" : "0"
        };
        if (!string.IsNullOrWhiteSpace(req.Email)) form["email"] = req.Email;
        if (!string.IsNullOrWhiteSpace(req.Gender)) form["gender"] = req.Gender;

        try
        {
            using var content = new FormUrlEncodedContent(form);
            var resp = await SendWithRetryAsync(() => client.PostAsync("api/register-internal", content, ct), ct);
            if (resp is null)
                return new DocosanRegisterResultDto(false, null, null, "TELEHEALTH_PROVIDER_UNAVAILABLE", "Khong ket noi duoc Docosan");

            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Docosan register-internal HTTP {Status}", (int)resp.StatusCode);
                return new DocosanRegisterResultDto(false, null, null, $"HTTP_{(int)resp.StatusCode}", null);
            }

            using var doc = JsonDocument.Parse(json);
            var data = GetData(doc.RootElement);
            var token = TryGetString(data, "access_token");
            var userId = TryGetInt(data, "user_id") ?? TryGetInt(data, "id");
            if (string.IsNullOrWhiteSpace(token))
                return new DocosanRegisterResultDto(false, null, userId, "NO_ACCESS_TOKEN", "Phan hoi Docosan khong co access_token");

            return new DocosanRegisterResultDto(true, token, userId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi Docosan register-internal");
            return new DocosanRegisterResultDto(false, null, null, "DOCOSAN_CONNECTION_ERROR", ex.Message);
        }
    }

    public async Task<DocosanAppointmentDto> CreateOrderPartnerAsync(
        DocosanCreateBookingRequest req, string patientToken, CancellationToken ct)
    {
        var client = CreateClient(patientToken);
        var body = new
        {
            clinic_id = req.DocosanClinicId,
            doctor_id = req.DocosanDoctorId,
            appointment_at = req.ScheduledStart.ToString("yyyy-MM-dd HH:mm:ss"),
            symptom = req.Symptom,
            payment_info = new
            {
                services = new[] { new { id = req.DocosanServiceId, quantity = 1, service_type = "telemedicine" } }
            }
        };

        try
        {
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var resp = await SendWithRetryAsync(() => client.PostAsync("api/payment/create-order-partner", content, ct), ct);
            if (resp is null)
                return Fail("TELEHEALTH_PROVIDER_UNAVAILABLE");

            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Docosan create-order-partner HTTP {Status}", (int)resp.StatusCode);
                return Fail($"HTTP_{(int)resp.StatusCode}");
            }

            return ParseAppointment(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi Docosan create-order-partner");
            return Fail("DOCOSAN_CONNECTION_ERROR", ex.Message);
        }
    }

    public async Task<DocosanAppointmentDto> GetAppointmentDetailAsync(int appointmentId, string patientToken, CancellationToken ct)
    {
        var client = CreateClient(patientToken);
        try
        {
            var resp = await SendWithRetryAsync(() =>
                client.GetAsync($"api/patients/my-appointment-detail?id={appointmentId}", ct), ct);
            if (resp is null)
                return Fail("TELEHEALTH_PROVIDER_UNAVAILABLE");

            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return Fail($"HTTP_{(int)resp.StatusCode}");

            return ParseAppointment(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi Docosan appointment-detail cho {AptId}", appointmentId);
            return Fail("DOCOSAN_CONNECTION_ERROR", ex.Message);
        }
    }

    public async Task<DocosanCommonResultDto> CancelAppointmentAsync(int appointmentId, string? reason, string patientToken, CancellationToken ct)
    {
        var client = CreateClient(patientToken);
        try
        {
            var body = new { id = appointmentId, reason };
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var resp = await SendWithRetryAsync(() => client.PostAsync("api/patients/cancel-appointment", content, ct), ct);
            if (resp is null) return new DocosanCommonResultDto(false, null, "Khong ket noi duoc Docosan");
            return new DocosanCommonResultDto(resp.IsSuccessStatusCode, (int)resp.StatusCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi goi Docosan cancel-appointment cho {AptId}", appointmentId);
            return new DocosanCommonResultDto(false, null, ex.Message);
        }
    }

    // ── Helpers ─────────────────────────────────────────────
    private HttpClient CreateClient(string? patientToken)
    {
        var client = _httpFactory.CreateClient(ClientName);
        if (!client.DefaultRequestHeaders.Contains("x-api-key"))
            client.DefaultRequestHeaders.Add("x-api-key", _opt.ApiKey);
        if (!string.IsNullOrWhiteSpace(patientToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
        return client;
    }

    /// <summary>Retry toi da RetryCount lan, backoff 1s/3s/9s. Chi retry loi mang / timeout / 5xx.</summary>
    private async Task<HttpResponseMessage?> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < Math.Max(1, _opt.RetryCount); attempt++)
        {
            try
            {
                var resp = await send();
                if (resp.IsSuccessStatusCode || (int)resp.StatusCode < 500)
                    return resp;
                lastEx = null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastEx = ex;
            }

            if (attempt < _opt.RetryCount - 1)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(3, attempt)), ct);
        }

        if (lastEx is not null)
            _logger.LogWarning(lastEx, "Docosan: het luot retry");
        return null;
    }

    private static DocosanAppointmentDto Fail(string code, string? msg = null)
        => new(false, null, null, null, null, null, null, null, null, null, null, code, msg, null);

    private static DocosanAppointmentDto ParseAppointment(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var data = GetData(doc.RootElement);
        if (data.ValueKind != JsonValueKind.Object)
            return Fail("NO_DATA");

        int? appointmentId = TryGetInt(data, "id") ?? TryGetInt(data, "appointment_id");
        string? mode = TryGetString(data, "mode") ?? TryGetString(data, "apt_mode");
        string? status = TryGetString(data, "status");
        string? paymentStatus = TryGetString(data, "payment_status");
        bool? showJoinCall = TryGetBool(data, "show_join_call");

        int? teleId = null;
        if (data.TryGetProperty("teleMedicine", out var tele))
        {
            if (tele.ValueKind == JsonValueKind.Object)
                teleId = TryGetInt(tele, "id");
            // teleMedicine co the la mang rong [] -> giu null
        }

        DateTime? start = TryGetDateTime(data, "appointment_at") ?? TryGetDateTime(data, "scheduled_start");
        DateTime? end = TryGetDateTime(data, "scheduled_end");
        string? link = TryGetString(data, "appointment_link");

        return new DocosanAppointmentDto(
            true, appointmentId, teleId, TryGetInt(data, "patient_id"),
            mode, status, link, start, end, paymentStatus, showJoinCall, null, null, null);
    }

    private static JsonElement GetData(JsonElement root)
        => root.TryGetProperty("data", out var data) ? data : root;

    private static string? TryGetString(JsonElement el, string prop)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static int? TryGetInt(JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i2)) return i2;
        return null;
    }

    private static bool? TryGetBool(JsonElement el, string prop)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => v.GetInt32() != 0,
            _ => null
        };
    }

    private static DateTime? TryGetDateTime(JsonElement el, string prop)
    {
        var s = TryGetString(el, prop);
        return s is not null && DateTime.TryParse(s, out var dt) ? dt : null;
    }
}
