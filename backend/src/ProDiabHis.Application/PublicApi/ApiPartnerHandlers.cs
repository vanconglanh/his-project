using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// List API Partners
// ============================================================
public record ListApiPartnersQuery(int TenantId, string? Q, string? Status)
    : IRequest<List<ApiPartnerResponse>>;

public class ListApiPartnersHandler : IRequestHandler<ListApiPartnersQuery, List<ApiPartnerResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public ListApiPartnersHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<ApiPartnerResponse>> Handle(ListApiPartnersQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT BIN_TO_UUID(id) AS id, name, contact_email, api_key_prefix,
                     scopes, rate_limit_per_min, daily_quota, status, expires_at,
                     ip_whitelist, created_at
              FROM diab_his_api_partners
              WHERE tenant_id = @TenantId
                AND deleted_at IS NULL
                AND (@Q IS NULL OR name LIKE CONCAT('%',@Q,'%'))
                AND (@Status IS NULL OR status = @Status)
              ORDER BY created_at DESC",
            new { q.TenantId, Q = q.Q, Status = q.Status });

        return rows.Select(MapToResponse).ToList();
    }

    internal static ApiPartnerResponse MapToResponse(dynamic r)
    {
        var scopes = r.scopes != null ? JsonSerializer.Deserialize<List<string>>((string)r.scopes) ?? new() : new List<string>();
        var ipList = r.ip_whitelist != null ? JsonSerializer.Deserialize<List<string>>((string)r.ip_whitelist) ?? new() : new List<string>();
        return new ApiPartnerResponse(
            Guid.Parse((string)r.id), (string)r.name, (string?)r.contact_email,
            (string)r.api_key_prefix, scopes,
            (int)r.rate_limit_per_min, (int)r.daily_quota,
            (string)r.status, (DateTime?)r.expires_at,
            ipList, (DateTime)r.created_at);
    }
}

// ============================================================
// Get API Partner by ID
// ============================================================
public record GetApiPartnerQuery(Guid Id, int TenantId) : IRequest<ApiPartnerResponse?>;

public class GetApiPartnerHandler : IRequestHandler<GetApiPartnerQuery, ApiPartnerResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public GetApiPartnerHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ApiPartnerResponse?> Handle(GetApiPartnerQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT BIN_TO_UUID(id) AS id, name, contact_email, api_key_prefix,
                     scopes, rate_limit_per_min, daily_quota, status, expires_at,
                     ip_whitelist, created_at
              FROM diab_his_api_partners
              WHERE id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = q.Id.ToString(), q.TenantId });

        return r == null ? null : ListApiPartnersHandler.MapToResponse(r);
    }
}

// ============================================================
// Create API Partner
// ============================================================
public record CreateApiPartnerCommand(int TenantId, ApiPartnerCreateRequest Request, Guid CreatedBy)
    : IRequest<ApiPartnerCreatedResponse>;

public class CreateApiPartnerHandler : IRequestHandler<CreateApiPartnerCommand, ApiPartnerCreatedResponse>
{
    private readonly IDapperConnectionFactory _db;
    public CreateApiPartnerHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ApiPartnerCreatedResponse> Handle(CreateApiPartnerCommand cmd, CancellationToken cancellationToken)
    {
        var (plainKey, hash, prefix) = GenerateApiKey();
        var id = Guid.NewGuid();
        var scopesJson = JsonSerializer.Serialize(cmd.Request.Scopes);
        var ipJson = cmd.Request.IpWhitelist != null ? JsonSerializer.Serialize(cmd.Request.IpWhitelist) : null;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_api_partners
                (id, tenant_id, name, contact_email, api_key_hash, api_key_prefix,
                 scopes, rate_limit_per_min, daily_quota, status, expires_at, ip_whitelist, created_at, updated_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, @Name, @Email, @Hash, @Prefix,
                      @Scopes, @Rate, @Quota, 'ACTIVE', @ExpiresAt, @Ip, UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Id = id.ToString(), cmd.TenantId, Name = cmd.Request.Name, Email = cmd.Request.ContactEmail,
                Hash = hash, Prefix = prefix, Scopes = scopesJson,
                Rate = cmd.Request.RateLimitPerMin, Quota = cmd.Request.DailyQuota,
                ExpiresAt = cmd.Request.ExpiresAt, Ip = ipJson
            });

        return new ApiPartnerCreatedResponse(
            id, cmd.Request.Name, cmd.Request.ContactEmail, prefix, plainKey,
            cmd.Request.Scopes, cmd.Request.RateLimitPerMin, cmd.Request.DailyQuota,
            "ACTIVE", cmd.Request.ExpiresAt, cmd.Request.IpWhitelist ?? new(), DateTime.UtcNow);
    }

    internal static (string plain, string hash, string prefix) GenerateApiKey()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "")[..32];
        var plain = $"pdh_live_{random}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        var hash = Convert.ToHexString(hashBytes).ToLower();
        var suffix = plain[^4..];
        var prefix = $"pdh_live_****{suffix}";
        return (plain, hash, prefix);
    }
}

// ============================================================
// Update API Partner
// ============================================================
public record UpdateApiPartnerCommand(Guid Id, int TenantId, ApiPartnerUpdateRequest Request) : IRequest<ApiPartnerResponse?>;

public class UpdateApiPartnerHandler : IRequestHandler<UpdateApiPartnerCommand, ApiPartnerResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public UpdateApiPartnerHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ApiPartnerResponse?> Handle(UpdateApiPartnerCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var sets = new List<string> { "updated_at = UTC_TIMESTAMP()" };
        var param = new DynamicParameters();
        param.Add("Id", cmd.Id.ToString());
        param.Add("TenantId", cmd.TenantId);

        if (cmd.Request.Name != null) { sets.Add("name = @Name"); param.Add("Name", cmd.Request.Name); }
        if (cmd.Request.ContactEmail != null) { sets.Add("contact_email = @Email"); param.Add("Email", cmd.Request.ContactEmail); }
        if (cmd.Request.Scopes != null) { sets.Add("scopes = @Scopes"); param.Add("Scopes", JsonSerializer.Serialize(cmd.Request.Scopes)); }
        if (cmd.Request.RateLimitPerMin.HasValue) { sets.Add("rate_limit_per_min = @Rate"); param.Add("Rate", cmd.Request.RateLimitPerMin); }
        if (cmd.Request.DailyQuota.HasValue) { sets.Add("daily_quota = @Quota"); param.Add("Quota", cmd.Request.DailyQuota); }
        if (cmd.Request.Status != null) { sets.Add("status = @Status"); param.Add("Status", cmd.Request.Status); }
        if (cmd.Request.ExpiresAt.HasValue) { sets.Add("expires_at = @ExpiresAt"); param.Add("ExpiresAt", cmd.Request.ExpiresAt); }
        if (cmd.Request.IpWhitelist != null) { sets.Add("ip_whitelist = @Ip"); param.Add("Ip", JsonSerializer.Serialize(cmd.Request.IpWhitelist)); }

        await conn.ExecuteAsync(
            $"UPDATE diab_his_api_partners SET {string.Join(",", sets)} WHERE id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId AND deleted_at IS NULL",
            param);

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT BIN_TO_UUID(id) AS id, name, contact_email, api_key_prefix,
                     scopes, rate_limit_per_min, daily_quota, status, expires_at,
                     ip_whitelist, created_at
              FROM diab_his_api_partners WHERE id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId",
            new { Id = cmd.Id.ToString(), cmd.TenantId });

        return r == null ? null : ListApiPartnersHandler.MapToResponse(r);
    }
}

// ============================================================
// Delete API Partner
// ============================================================
public record DeleteApiPartnerCommand(Guid Id, int TenantId) : IRequest;

public class DeleteApiPartnerHandler : IRequestHandler<DeleteApiPartnerCommand>
{
    private readonly IDapperConnectionFactory _db;
    public DeleteApiPartnerHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(DeleteApiPartnerCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE diab_his_api_partners SET deleted_at = UTC_TIMESTAMP(), status = 'DISABLED' WHERE id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId",
            new { Id = cmd.Id.ToString(), cmd.TenantId });
    }
}

// ============================================================
// Regenerate Key
// ============================================================
public record RegenerateApiKeyCommand(Guid Id, int TenantId) : IRequest<ApiPartnerCreatedResponse>;

public class RegenerateApiKeyHandler : IRequestHandler<RegenerateApiKeyCommand, ApiPartnerCreatedResponse>
{
    private readonly IDapperConnectionFactory _db;
    public RegenerateApiKeyHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ApiPartnerCreatedResponse> Handle(RegenerateApiKeyCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT BIN_TO_UUID(id) AS id, name, contact_email, scopes, rate_limit_per_min, daily_quota, expires_at, ip_whitelist, created_at FROM diab_his_api_partners WHERE id = UUID_TO_BIN(@Id) AND tenant_id = @TenantId AND deleted_at IS NULL",
            new { Id = cmd.Id.ToString(), cmd.TenantId });

        if (existing == null) throw new Exception("PARTNER_NOT_FOUND");

        var (plain, hash, prefix) = CreateApiPartnerHandler.GenerateApiKey();

        await conn.ExecuteAsync(
            "UPDATE diab_his_api_partners SET api_key_hash = @Hash, api_key_prefix = @Prefix, updated_at = UTC_TIMESTAMP() WHERE id = UUID_TO_BIN(@Id)",
            new { Hash = hash, Prefix = prefix, Id = cmd.Id.ToString() });

        var scopes = JsonSerializer.Deserialize<List<string>>((string)existing.scopes) ?? new();
        var ipList = existing.ip_whitelist != null ? JsonSerializer.Deserialize<List<string>>((string)existing.ip_whitelist) ?? new() : new List<string>();

        return new ApiPartnerCreatedResponse(
            Guid.Parse((string)existing.id), (string)existing.name, (string?)existing.contact_email,
            prefix, plain, scopes, (int)existing.rate_limit_per_min, (int)existing.daily_quota,
            "ACTIVE", (DateTime?)existing.expires_at, ipList, (DateTime)existing.created_at);
    }
}

// ============================================================
// Usage Stats
// ============================================================
public record GetUsageStatsQuery(Guid PartnerId, int TenantId, DateOnly? From, DateOnly? To) : IRequest<ApiUsageStatsResponse>;

public class GetUsageStatsHandler : IRequestHandler<GetUsageStatsQuery, ApiUsageStatsResponse>
{
    private readonly IDapperConnectionFactory _db;
    public GetUsageStatsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<ApiUsageStatsResponse> Handle(GetUsageStatsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var fromDate = q.From?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.AddDays(-7);
        var toDate = q.To?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.UtcNow;

        var stats = await conn.QueryFirstAsync<(int total, int success, int error)>(
            @"SELECT COUNT(*) AS total,
                     SUM(CASE WHEN status_code < 400 THEN 1 ELSE 0 END) AS success,
                     SUM(CASE WHEN status_code >= 400 THEN 1 ELSE 0 END) AS error
              FROM diab_his_api_request_logs
              WHERE partner_id = UUID_TO_BIN(@PartnerId) AND tenant_id = @TenantId
                AND called_at BETWEEN @From AND @To",
            new { PartnerId = q.PartnerId.ToString(), q.TenantId, From = fromDate, To = toDate });

        var byEndpoint = await conn.QueryAsync<(string path, int count)>(
            @"SELECT path, COUNT(*) AS count FROM diab_his_api_request_logs
              WHERE partner_id = UUID_TO_BIN(@PartnerId) AND tenant_id = @TenantId AND called_at BETWEEN @From AND @To
              GROUP BY path ORDER BY count DESC LIMIT 20",
            new { PartnerId = q.PartnerId.ToString(), q.TenantId, From = fromDate, To = toDate });

        var byDay = await conn.QueryAsync<(DateTime day, int count)>(
            @"SELECT DATE(called_at) AS day, COUNT(*) AS count FROM diab_his_api_request_logs
              WHERE partner_id = UUID_TO_BIN(@PartnerId) AND tenant_id = @TenantId AND called_at BETWEEN @From AND @To
              GROUP BY DATE(called_at) ORDER BY day",
            new { PartnerId = q.PartnerId.ToString(), q.TenantId, From = fromDate, To = toDate });

        return new ApiUsageStatsResponse(
            stats.total, stats.success, stats.error,
            byEndpoint.Select(e => new EndpointStat(e.path, e.count)).ToList(),
            byDay.Select(d => new DayStat(DateOnly.FromDateTime(d.day), d.count)).ToList());
    }
}

// ============================================================
// Request Logs
// ============================================================
public record GetRequestLogsQuery(Guid PartnerId, int TenantId, int Page, int? StatusCode)
    : IRequest<(List<ApiRequestLogEntry> Items, int Total)>;

public class GetRequestLogsHandler : IRequestHandler<GetRequestLogsQuery, (List<ApiRequestLogEntry>, int)>
{
    private readonly IDapperConnectionFactory _db;
    public GetRequestLogsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<(List<ApiRequestLogEntry>, int)> Handle(GetRequestLogsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        int pageSize = 20;
        int offset = (q.Page - 1) * pageSize;

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_api_request_logs WHERE partner_id = UUID_TO_BIN(@PartnerId) AND tenant_id = @TenantId AND (@StatusCode IS NULL OR status_code = @StatusCode)",
            new { PartnerId = q.PartnerId.ToString(), q.TenantId, q.StatusCode });

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT BIN_TO_UUID(id) AS id, method, path, status_code, duration_ms, ip, called_at, error_code
              FROM diab_his_api_request_logs
              WHERE partner_id = UUID_TO_BIN(@PartnerId) AND tenant_id = @TenantId
                AND (@StatusCode IS NULL OR status_code = @StatusCode)
              ORDER BY called_at DESC LIMIT @PageSize OFFSET @Offset",
            new { PartnerId = q.PartnerId.ToString(), q.TenantId, q.StatusCode, PageSize = pageSize, Offset = offset });

        var items = rows.Select(r => new ApiRequestLogEntry(
            Guid.Parse((string)r.id), (string)r.method, (string)r.path,
            (int)r.status_code, (int)r.duration_ms, (string?)r.ip,
            (DateTime)r.called_at, (string?)r.error_code)).ToList();

        return (items, total);
    }
}
