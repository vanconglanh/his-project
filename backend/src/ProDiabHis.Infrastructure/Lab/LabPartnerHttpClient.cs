using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.LabPartners;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>HTTP REST client gui chi dinh XN toi doi tac, timeout 5s</summary>
public class LabPartnerHttpClient : ILabPartnerClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LabPartnerHttpClient> _logger;

    public LabPartnerHttpClient(IHttpClientFactory httpFactory, ILogger<LabPartnerHttpClient> logger)
    { _httpFactory = httpFactory; _logger = logger; }

    public async Task<TestConnectionResponse> TestConnectionAsync(
        string endpointUrl, string authType, string? apiKey, string? bearerToken,
        CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("LabPartner");
        client.Timeout = TimeSpan.FromSeconds(5);

        BuildAuth(client, authType, apiKey, bearerToken);

        var sw = Stopwatch.StartNew();
        try
        {
            var pingUrl = endpointUrl.TrimEnd('/') + "/health";
            var resp    = await client.GetAsync(pingUrl, ct);
            sw.Stop();
            var ok  = resp.IsSuccessStatusCode;
            return new(ok, (int)sw.ElapsedMilliseconds,
                ok ? "Connection OK" : $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }
        catch (TaskCanceledException)
        {
            return new(false, 5000, "Connection timeout (5s)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TestConnection failed: {Url}", endpointUrl);
            return new(false, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<LabPartnerSendResult> SendOrderAsync(
        string endpointUrl, string authType, string? apiKey, string? bearerToken,
        object payload, CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("LabPartner");
        client.Timeout = TimeSpan.FromSeconds(30);
        BuildAuth(client, authType, apiKey, bearerToken);

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var url  = endpointUrl.TrimEnd('/') + "/orders";
            var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return new(false, null, $"HTTP {(int)resp.StatusCode}: {body}");

            // Parse external_order_id tu response
            string? extId = null;
            try
            {
                var root = JsonSerializer.Deserialize<JsonElement>(body);
                if (root.TryGetProperty("order_id", out var oid)) extId = oid.GetString();
                else if (root.TryGetProperty("external_order_id", out var eoid)) extId = eoid.GetString();
            }
            catch { /* ignore */ }

            return new(true, extId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendOrder failed");
            return new(false, null, ex.Message);
        }
    }

    private static void BuildAuth(HttpClient client, string authType, string? apiKey, string? bearerToken)
    {
        client.DefaultRequestHeaders.Authorization = null;
        if (authType == "API_KEY" && apiKey is not null)
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        else if (authType == "BEARER" && bearerToken is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
    }
}
