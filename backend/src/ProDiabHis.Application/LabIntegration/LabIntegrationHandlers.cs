using System.Text;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabPartners;
using ProDiabHis.Application.LabResults;

namespace ProDiabHis.Application.LabIntegration;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════
public record SendToPartnerCommand(Guid LabOrderId, SendToPartnerRequest Req)
    : IRequest<Result<LabOutboundResponse>>;

public record ListOutboundQuery(
    string? Status, Guid? LabPartnerId, DateTime? FromDate, DateTime? ToDate, int Page, int PageSize)
    : IRequest<Result<(IReadOnlyList<LabOutboundResponse>, int)>>;

public record RetryOutboundCommand(Guid Id)
    : IRequest<Result<bool>>;

public record WebhookInboundCommand(
    string PartnerCode, string ApiKey, string Signature, byte[] RawBody,
    string? ContentType, object? ParsedPayload, string? RawHl7)
    : IRequest<Result<(Guid InboundId, DateTime ReceivedAt)>>;

public record ListInboundQuery(
    string? Status, Guid? LabPartnerId, DateTime? FromDate, DateTime? ToDate, int Page, int PageSize)
    : IRequest<Result<(IReadOnlyList<LabInboundResponse>, int)>>;

public record ReprocessInboundCommand(Guid Id)
    : IRequest<Result<bool>>;

public record GetInboundRawQuery(Guid Id)
    : IRequest<Result<(object? PayloadJson, string? RawHl7, object? Headers)>>;

public record GetIntegrationStatsQuery(int Days)
    : IRequest<Result<LabIntegrationStatsResponse>>;

// ═══════════════════════════════════════════════
// SEND TO PARTNER
// ═══════════════════════════════════════════════
public class SendToPartnerCommandHandler
    : IRequestHandler<SendToPartnerCommand, Result<LabOutboundResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IEncryptionService _enc;
    private readonly ILabPartnerClient _client;
    private readonly IBackgroundJobEnqueuer _jobs;
    private readonly ILogger<SendToPartnerCommandHandler> _logger;

    public SendToPartnerCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IEncryptionService enc, ILabPartnerClient client,
        IBackgroundJobEnqueuer jobs, ILogger<SendToPartnerCommandHandler> logger)
    { _db = db; _tenant = tenant; _user = user; _enc = enc; _client = client; _jobs = jobs; _logger = logger; }

    public async Task<Result<LabOutboundResponse>> Handle(SendToPartnerCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM diab_his_cli_lab_orders WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.LabOrderId.ToString(), TId = _tenant.TenantId });
        if (order is null)
            return Result<LabOutboundResponse>.Failure("LAB_ORDER_NOT_FOUND", "Không tìm thấy chỉ định XN");

        var partner = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM cli_lab_partners WHERE id=@PId AND tenant_id=@TId AND deleted_at IS NULL AND status='ACTIVE'",
            new { PId = cmd.Req.LabPartnerId.ToString(), TId = _tenant.TenantId });
        if (partner is null)
            return Result<LabOutboundResponse>.Failure("LAB_PARTNER_NOT_FOUND", "Đối tác lab không tồn tại hoặc chưa ACTIVE");

        var payload = new
        {
            lab_order_id = cmd.LabOrderId,
            test_code    = (string)order.test_code,
            priority     = cmd.Req.Priority,
            note         = cmd.Req.Note,
            sent_at      = DateTime.UtcNow
        };
        var payloadJson = JsonSerializer.Serialize(payload);

        var id  = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        await conn.ExecuteAsync(@"
            INSERT INTO cli_lab_outbound
                (id, tenant_id, lab_order_id, lab_partner_id, payload_json, status, retry_count, created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @OId, @PId, @Payload, 'PENDING', 0, @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId,
                OId = cmd.LabOrderId.ToString(), PId = cmd.Req.LabPartnerId.ToString(),
                Payload = payloadJson, Now = now, UserId = _user.UserId?.ToString()
            });

        // Enqueue background job de gui async
        _jobs.EnqueueSendOutbound(id, _tenant.TenantId);

        var row = await conn.QueryFirstAsync<dynamic>(
            @"SELECT o.*, p.name as partner_name FROM cli_lab_outbound o
              LEFT JOIN cli_lab_partners p ON p.id = o.lab_partner_id
              WHERE o.id=@Id", new { Id = id });

        return Result<LabOutboundResponse>.Success(MapOutbound(row));
    }

    private static LabOutboundResponse MapOutbound(dynamic r) => new(
        Guid.Parse((string)r.id),
        Guid.Parse((string)r.lab_order_id),
        Guid.Parse((string)r.lab_partner_id),
        (string?)r.partner_name,
        (string?)r.external_order_id,
        r.payload_json is not null ? JsonSerializer.Deserialize<object>((string)r.payload_json) : null,
        (string)r.status,
        (int)r.retry_count,
        (string?)r.error_message,
        (DateTime?)r.sent_at,
        (DateTime?)r.acked_at,
        (DateTime)r.created_at);
}

// ═══════════════════════════════════════════════
// LIST OUTBOUND
// ═══════════════════════════════════════════════
public class ListOutboundQueryHandler
    : IRequestHandler<ListOutboundQuery, Result<(IReadOnlyList<LabOutboundResponse>, int)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListOutboundQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<(IReadOnlyList<LabOutboundResponse>, int)>> Handle(
        ListOutboundQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE o.tenant_id=@TId";
        var p = new DynamicParameters();
        p.Add("TId", _tenant.TenantId);

        if (!string.IsNullOrEmpty(q.Status))  { where += " AND o.status=@St";           p.Add("St", q.Status); }
        if (q.LabPartnerId.HasValue)           { where += " AND o.lab_partner_id=@PId";  p.Add("PId", q.LabPartnerId.Value.ToString()); }
        if (q.FromDate.HasValue)               { where += " AND o.created_at>=@From";    p.Add("From", q.FromDate.Value); }
        if (q.ToDate.HasValue)                 { where += " AND o.created_at<=@To";      p.Add("To", q.ToDate.Value.AddDays(1)); }

        var offset = (q.Page - 1) * q.PageSize;
        p.Add("Limit", q.PageSize); p.Add("Offset", offset);

        var sql = $@"SELECT o.*, p.name AS partner_name
                     FROM cli_lab_outbound o
                     LEFT JOIN cli_lab_partners p ON p.id = o.lab_partner_id
                     {where} ORDER BY o.created_at DESC LIMIT @Limit OFFSET @Offset";

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM cli_lab_outbound o {where}", p);
        var rows = await conn.QueryAsync<dynamic>(sql, p);

        var items = rows.Select(r => new LabOutboundResponse(
            Guid.Parse((string)r.id),
            Guid.Parse((string)r.lab_order_id),
            Guid.Parse((string)r.lab_partner_id),
            (string?)r.partner_name,
            (string?)r.external_order_id,
            r.payload_json is not null ? JsonSerializer.Deserialize<object>((string)r.payload_json) : null,
            (string)r.status, (int)r.retry_count, (string?)r.error_message,
            (DateTime?)r.sent_at, (DateTime?)r.acked_at, (DateTime)r.created_at)).ToList();

        return Result<(IReadOnlyList<LabOutboundResponse>, int)>.Success((items.AsReadOnly(), total));
    }
}

// ═══════════════════════════════════════════════
// RETRY OUTBOUND
// ═══════════════════════════════════════════════
public class RetryOutboundCommandHandler
    : IRequestHandler<RetryOutboundCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBackgroundJobEnqueuer _jobs;

    public RetryOutboundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBackgroundJobEnqueuer jobs)
    { _db = db; _tenant = tenant; _jobs = jobs; }

    public async Task<Result<bool>> Handle(RetryOutboundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status, retry_count FROM cli_lab_outbound WHERE id=@Id AND tenant_id=@TId",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("LAB_OUTBOUND_NOT_FOUND", "Không tìm thấy lệnh gửi đi");
        if ((string)row.status != "FAILED")
            return Result<bool>.Failure("LAB_OUTBOUND_NOT_FAILED", "Chỉ có thể retry lệnh ở trạng thái FAILED");
        if ((int)row.retry_count >= 5)
            return Result<bool>.Failure("LAB_INTEGRATION_RETRY_EXCEEDED",
                "Đã vượt 5 lần retry. Cần admin force reset.");

        await conn.ExecuteAsync(
            "UPDATE cli_lab_outbound SET status='PENDING', retry_count=retry_count+1, updated_at=@Now WHERE id=@Id",
            new { Now = DateTime.UtcNow, Id = cmd.Id.ToString() });

        _jobs.EnqueueSendOutbound(cmd.Id.ToString(), _tenant.TenantId);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// WEBHOOK INBOUND
// ═══════════════════════════════════════════════
public class WebhookInboundCommandHandler
    : IRequestHandler<WebhookInboundCommand, Result<(Guid InboundId, DateTime ReceivedAt)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IHmacSignatureVerifier _hmac;
    private readonly IEncryptionService _enc;
    private readonly IBackgroundJobEnqueuer _jobs;
    private readonly ILogger<WebhookInboundCommandHandler> _logger;

    public WebhookInboundCommandHandler(IDapperConnectionFactory db, IHmacSignatureVerifier hmac,
        IEncryptionService enc, IBackgroundJobEnqueuer jobs, ILogger<WebhookInboundCommandHandler> logger)
    { _db = db; _hmac = hmac; _enc = enc; _jobs = jobs; _logger = logger; }

    public async Task<Result<(Guid InboundId, DateTime ReceivedAt)>> Handle(
        WebhookInboundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        // Lookup partner by code (khong co tenant scope vi la public endpoint)
        var partner = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM cli_lab_partners WHERE code=@Code AND deleted_at IS NULL AND status='ACTIVE'",
            new { Code = cmd.PartnerCode });

        if (partner is null)
            return Result<(Guid, DateTime)>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        // Verify Api Key
        string? storedApiKey = null;
        if (partner.api_key_encrypted is not null)
        {
            try { storedApiKey = _enc.Decrypt(Encoding.UTF8.GetString((byte[])partner.api_key_encrypted)); }
            catch { return Result<(Guid, DateTime)>.Failure("LAB_PARTNER_AUTH_INVALID", "Lỗi giải mã credentials"); }
        }

        if (storedApiKey != cmd.ApiKey)
            return Result<(Guid, DateTime)>.Failure("LAB_WEBHOOK_INVALID_SIGNATURE", "API key không hợp lệ");

        // Verify HMAC
        if (storedApiKey is not null && !_hmac.Verify(storedApiKey, cmd.RawBody, cmd.Signature))
            return Result<(Guid, DateTime)>.Failure("LAB_WEBHOOK_INVALID_SIGNATURE", "Chữ ký HMAC không hợp lệ");

        // Parse external_result_id tu payload
        string externalResultId;
        try
        {
            if (cmd.ParsedPayload is JsonElement je && je.TryGetProperty("external_result_id", out var erId))
                externalResultId = erId.GetString() ?? Guid.NewGuid().ToString();
            else
                externalResultId = Guid.NewGuid().ToString();
        }
        catch { externalResultId = Guid.NewGuid().ToString(); }

        var id        = Guid.NewGuid().ToString();
        var receivedAt = DateTime.UtcNow;
        var payloadStr = cmd.ParsedPayload is not null ? JsonSerializer.Serialize(cmd.ParsedPayload) : null;

        // Idempotent insert (UNIQUE lab_partner_id + external_result_id)
        try
        {
            await conn.ExecuteAsync(@"
                INSERT INTO cli_lab_inbound
                    (id, tenant_id, lab_partner_id, external_result_id, payload_json, raw_hl7_message,
                     status, received_at, created_at, updated_at)
                VALUES
                    (@Id, @TId, @PId, @ExtId, @Payload, @Hl7,
                     'RECEIVED', @Now, @Now, @Now)",
                new
                {
                    Id = id, TId = (int)partner.tenant_id,
                    PId = (string)partner.id, ExtId = externalResultId,
                    Payload = payloadStr, Hl7 = cmd.RawHl7, Now = receivedAt
                });
        }
        catch (Exception ex) when (ex.Message.Contains("Duplicate") || ex.Message.Contains("duplicate") || ex.Message.Contains("1062"))
        {
            // Duplicate — tra ve inbound_id cu (idempotent)
            var existing = await conn.QueryFirstAsync<dynamic>(
                "SELECT id, received_at FROM cli_lab_inbound WHERE lab_partner_id=@PId AND external_result_id=@ExtId",
                new { PId = (string)partner.id, ExtId = externalResultId });
            return Result<(Guid, DateTime)>.Success((Guid.Parse((string)existing.id), (DateTime)existing.received_at));
        }

        // Enqueue parse job
        _jobs.EnqueueProcessInbound(id);

        return Result<(Guid, DateTime)>.Success((Guid.Parse(id), receivedAt));
    }
}

// ═══════════════════════════════════════════════
// LIST INBOUND
// ═══════════════════════════════════════════════
public class ListInboundQueryHandler
    : IRequestHandler<ListInboundQuery, Result<(IReadOnlyList<LabInboundResponse>, int)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListInboundQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<(IReadOnlyList<LabInboundResponse>, int)>> Handle(
        ListInboundQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE i.tenant_id=@TId";
        var p = new DynamicParameters();
        p.Add("TId", _tenant.TenantId);

        if (!string.IsNullOrEmpty(q.Status))  { where += " AND i.status=@St";           p.Add("St", q.Status); }
        if (q.LabPartnerId.HasValue)           { where += " AND i.lab_partner_id=@PId";  p.Add("PId", q.LabPartnerId.Value.ToString()); }
        if (q.FromDate.HasValue)               { where += " AND i.received_at>=@From";   p.Add("From", q.FromDate.Value); }
        if (q.ToDate.HasValue)                 { where += " AND i.received_at<=@To";     p.Add("To", q.ToDate.Value.AddDays(1)); }

        var offset = (q.Page - 1) * q.PageSize;
        p.Add("Limit", q.PageSize); p.Add("Offset", offset);

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM cli_lab_inbound i {where}", p);
        var rows = await conn.QueryAsync<dynamic>(
            $@"SELECT i.*, p.name AS partner_name
               FROM cli_lab_inbound i
               LEFT JOIN cli_lab_partners p ON p.id = i.lab_partner_id
               {where} ORDER BY i.received_at DESC LIMIT @Limit OFFSET @Offset", p);

        var items = rows.Select(r => new LabInboundResponse(
            Guid.Parse((string)r.id),
            Guid.Parse((string)r.lab_partner_id),
            (string?)r.partner_name,
            (string)r.external_result_id,
            string.IsNullOrEmpty((string?)r.outbound_id) ? null : Guid.Parse((string)r.outbound_id),
            r.payload_json is not null ? JsonSerializer.Deserialize<object>((string)r.payload_json) : null,
            (string?)r.raw_hl7_message,
            (string)r.status,
            (DateTime?)r.processed_at,
            (DateTime)r.received_at,
            (int)r.processed_result_count,
            (string?)r.error_message)).ToList();

        return Result<(IReadOnlyList<LabInboundResponse>, int)>.Success((items.AsReadOnly(), total));
    }
}

// ═══════════════════════════════════════════════
// REPROCESS INBOUND
// ═══════════════════════════════════════════════
public class ReprocessInboundCommandHandler
    : IRequestHandler<ReprocessInboundCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBackgroundJobEnqueuer _jobs;

    public ReprocessInboundCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBackgroundJobEnqueuer jobs)
    { _db = db; _tenant = tenant; _jobs = jobs; }

    public async Task<Result<bool>> Handle(ReprocessInboundCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, status FROM cli_lab_inbound WHERE id=@Id AND tenant_id=@TId",
            new { Id = cmd.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<bool>.Failure("LAB_INBOUND_NOT_FOUND", "Không tìm thấy bản ghi inbound");

        await conn.ExecuteAsync(
            "UPDATE cli_lab_inbound SET status='RECEIVED', error_message=NULL, updated_at=@Now WHERE id=@Id",
            new { Now = DateTime.UtcNow, Id = cmd.Id.ToString() });

        _jobs.EnqueueProcessInbound(cmd.Id.ToString());
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// GET RAW PAYLOAD
// ═══════════════════════════════════════════════
public class GetInboundRawQueryHandler
    : IRequestHandler<GetInboundRawQuery, Result<(object?, string?, object?)>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetInboundRawQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<(object?, string?, object?)>> Handle(GetInboundRawQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT payload_json, raw_hl7_message, headers FROM cli_lab_inbound WHERE id=@Id AND tenant_id=@TId",
            new { Id = q.Id.ToString(), TId = _tenant.TenantId });

        if (row is null)
            return Result<(object?, string?, object?)>.Failure("LAB_INBOUND_NOT_FOUND", "Không tìm thấy bản ghi inbound");

        object? payload = row.payload_json is not null
            ? JsonSerializer.Deserialize<object>((string)row.payload_json) : null;
        object? headers = row.headers is not null
            ? JsonSerializer.Deserialize<object>((string)row.headers) : null;

        return Result<(object?, string?, object?)>.Success((payload, (string?)row.raw_hl7_message, headers));
    }
}

// ═══════════════════════════════════════════════
// STATS
// ═══════════════════════════════════════════════
public class GetIntegrationStatsQueryHandler
    : IRequestHandler<GetIntegrationStatsQuery, Result<LabIntegrationStatsResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetIntegrationStatsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<LabIntegrationStatsResponse>> Handle(
        GetIntegrationStatsQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var days  = Math.Min(q.Days, 30);
        var from  = DateTime.UtcNow.AddDays(-days).Date;
        var to    = DateTime.UtcNow.Date;
        var p     = new { TId = _tenant.TenantId, From = from, To = to.AddDays(1) };

        var outTotal   = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM cli_lab_outbound WHERE tenant_id=@TId AND created_at>=@From AND created_at<@To", p);
        var outFailed  = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM cli_lab_outbound WHERE tenant_id=@TId AND status='FAILED' AND created_at>=@From AND created_at<@To", p);
        var inTotal    = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM cli_lab_inbound WHERE tenant_id=@TId AND received_at>=@From AND received_at<@To", p);
        var inFailed   = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM cli_lab_inbound WHERE tenant_id=@TId AND status='FAILED' AND received_at>=@From AND received_at<@To", p);

        var byPartnerRows = await conn.QueryAsync<dynamic>(@"
            SELECT
                p.id AS partner_id, p.name AS partner_name,
                COUNT(DISTINCT o.id) AS outbound_sent,
                COUNT(DISTINCT i.id) AS inbound_received,
                AVG(TIMESTAMPDIFF(MINUTE, o.sent_at, i.received_at)) AS avg_turnaround
            FROM cli_lab_partners p
            LEFT JOIN cli_lab_outbound o ON o.lab_partner_id = p.id
                AND o.tenant_id = @TId AND o.created_at >= @From AND o.created_at < @To
            LEFT JOIN cli_lab_inbound i ON i.lab_partner_id = p.id
                AND i.tenant_id = @TId AND i.received_at >= @From AND i.received_at < @To
            WHERE p.tenant_id = @TId AND p.deleted_at IS NULL
            GROUP BY p.id, p.name", p);

        var byPartner = byPartnerRows.Select(r => new PartnerStats(
            Guid.Parse((string)r.partner_id),
            (string)r.partner_name,
            (int)(r.outbound_sent ?? 0),
            (int)(r.inbound_received ?? 0),
            (double)(r.avg_turnaround ?? 0.0))).ToList();

        return Result<LabIntegrationStatsResponse>.Success(new(
            DateOnly.FromDateTime(from), DateOnly.FromDateTime(to),
            outTotal, outFailed, inTotal, inFailed, byPartner));
    }
}
