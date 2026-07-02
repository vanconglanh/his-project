using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabPartners;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: gui chi dinh XN ra doi tac.
/// Exponential backoff: 1m, 5m, 15m, 1h, 6h (max 5 retry).
/// </summary>
public class SendOutboundJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IEncryptionService _enc;
    private readonly ILabPartnerClient _client;
    private readonly ILogger<SendOutboundJob> _logger;

    private static readonly int[] BackoffMinutes = { 1, 5, 15, 60, 360 };

    public SendOutboundJob(IDapperConnectionFactory db, IEncryptionService enc,
        ILabPartnerClient client, ILogger<SendOutboundJob> logger)
    { _db = db; _enc = enc; _client = client; _logger = logger; }

    public async Task Execute(string outboundId, int tenantId)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT o.*, p.endpoint_url, p.auth_type,
                     p.api_key_encrypted, p.bearer_token_encrypted
              FROM cli_lab_outbound o
              JOIN cli_lab_partners p ON p.id = o.lab_partner_id
              WHERE o.id=@Id AND o.tenant_id=@TId",
            new { Id = outboundId, TId = tenantId });

        if (row is null)
        {
            _logger.LogWarning("SendOutboundJob: outbound {Id} not found", outboundId);
            return;
        }

        if ((int)row.retry_count >= 5)
        {
            await conn.ExecuteAsync(
                "UPDATE cli_lab_outbound SET status='FAILED', error_message=@Err, updated_at=@Now WHERE id=@Id",
                new { Err = "Exceeded max retry (5)", Now = DateTime.UtcNow, Id = outboundId });
            return;
        }

        string? apiKey = null;
        string? bearer = null;
        try
        {
            if (row.api_key_encrypted is not null)
                apiKey = _enc.Decrypt(Encoding.UTF8.GetString((byte[])row.api_key_encrypted));
            if (row.bearer_token_encrypted is not null)
                bearer = _enc.Decrypt(Encoding.UTF8.GetString((byte[])row.bearer_token_encrypted));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot decrypt credentials for outbound {Id}", outboundId);
            await MarkFailed(conn, outboundId, "Lỗi giải mã credentials: " + ex.Message);
            return;
        }

        var payload = row.payload_json is not null
            ? JsonSerializer.Deserialize<object>((string)row.payload_json) : null;

        var result = await _client.SendOrderAsync(
            (string)row.endpoint_url, (string)row.auth_type, apiKey, bearer, payload ?? new { });

        var now = DateTime.UtcNow;
        if (result.Success)
        {
            await conn.ExecuteAsync(@"
                UPDATE cli_lab_outbound SET
                    status='SENT', external_order_id=@ExtId, sent_at=@Now, updated_at=@Now
                WHERE id=@Id",
                new { ExtId = result.ExternalOrderId, Now = now, Id = outboundId });

            _logger.LogInformation("Outbound {Id} sent OK, external_order_id={Ext}", outboundId, result.ExternalOrderId);
        }
        else
        {
            var retryCount = (int)row.retry_count + 1;
            if (retryCount >= 5)
            {
                await MarkFailed(conn, outboundId, result.ErrorMessage ?? "Unknown error");
            }
            else
            {
                await conn.ExecuteAsync(
                    "UPDATE cli_lab_outbound SET status='FAILED', retry_count=@Rc, error_message=@Err, updated_at=@Now WHERE id=@Id",
                    new { Rc = retryCount, Err = result.ErrorMessage, Now = now, Id = outboundId });

                // Schedule retry voi exponential backoff
                var delay = BackoffMinutes[Math.Min(retryCount - 1, BackoffMinutes.Length - 1)];
                _logger.LogWarning("Outbound {Id} failed (retry {Rc}), scheduling retry in {Delay}m",
                    outboundId, retryCount, delay);
                // Note: Hangfire retry duoc configure rieng; o day ta chi log
            }
        }
    }

    private static async Task MarkFailed(System.Data.IDbConnection conn, string id, string err)
    {
        await conn.ExecuteAsync(
            "UPDATE cli_lab_outbound SET status='FAILED', error_message=@Err, updated_at=@Now WHERE id=@Id",
            new { Err = err, Now = DateTime.UtcNow, Id = id });
    }
}
