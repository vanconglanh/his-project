using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using System.Text.Json;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Portal: Doc tuy chon kenh thong bao (mac dinh {push:true,email:true})
// ============================================================
public record GetPortalNotifyPreferencesQuery(Guid PatientId, int TenantId) : IRequest<PortalNotifyPreferencesResponse>;

public class GetPortalNotifyPreferencesHandler
    : IRequestHandler<GetPortalNotifyPreferencesQuery, PortalNotifyPreferencesResponse>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalNotifyPreferencesHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PortalNotifyPreferencesResponse> Handle(GetPortalNotifyPreferencesQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var json = await conn.ExecuteScalarAsync<string?>(
            "SELECT notify_prefs_json FROM diab_his_pat_portal_accounts WHERE patient_id = @PatientId AND tenant_id = @TenantId",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        return ParsePrefs(json);
    }

    public static PortalNotifyPreferencesResponse ParsePrefs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PortalNotifyPreferencesResponse(true, true);
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new();
            return new PortalNotifyPreferencesResponse(
                dict.TryGetValue("push", out var p) ? p : true,
                dict.TryGetValue("email", out var e) ? e : true);
        }
        catch
        {
            return new PortalNotifyPreferencesResponse(true, true);
        }
    }
}

// ============================================================
// Portal: Cap nhat tuy chon kenh thong bao
// ============================================================
public record UpdatePortalNotifyPreferencesCommand(Guid PatientId, int TenantId, UpdatePortalNotifyPreferencesRequest Request)
    : IRequest<PortalNotifyPreferencesResponse>;

public class UpdatePortalNotifyPreferencesHandler
    : IRequestHandler<UpdatePortalNotifyPreferencesCommand, PortalNotifyPreferencesResponse>
{
    private readonly IDapperConnectionFactory _db;
    public UpdatePortalNotifyPreferencesHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PortalNotifyPreferencesResponse> Handle(UpdatePortalNotifyPreferencesCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var json = JsonSerializer.Serialize(new { push = cmd.Request.Push, email = cmd.Request.Email });

        await conn.ExecuteAsync(
            @"UPDATE diab_his_pat_portal_accounts SET notify_prefs_json = @Json, updated_at = UTC_TIMESTAMP()
              WHERE patient_id = @PatientId AND tenant_id = @TenantId",
            new { Json = json, PatientId = cmd.PatientId.ToString(), cmd.TenantId });

        return new PortalNotifyPreferencesResponse(cmd.Request.Push, cmd.Request.Email);
    }
}

// ============================================================
// Portal: Dang ky web push subscription (theo patient)
// ============================================================
public record PortalPushSubscribeCommand(Guid PatientId, int TenantId, PortalPushSubscribeRequest Request) : IRequest;

public class PortalPushSubscribeHandler : IRequestHandler<PortalPushSubscribeCommand>
{
    private readonly IDapperConnectionFactory _db;
    public PortalPushSubscribeHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(PortalPushSubscribeCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            // user_id/id la BINARY(16) NOT NULL -> dung UUID_TO_BIN. Benh nhan khong phai user noi bo
            // nhung tai dung cot user_id = UUID cua patient (patient_id CHAR36 rieng de query portal).
            @"INSERT INTO diab_his_nti_web_push_subs
                (id, tenant_id, user_id, patient_id, endpoint, p256dh_key, auth_key, created_at)
              VALUES (UUID_TO_BIN(@Id), @TenantId, UUID_TO_BIN(@PatientId), @PatientId, @Endpoint, @P256dh, @Auth, UTC_TIMESTAMP())
              ON DUPLICATE KEY UPDATE p256dh_key = @P256dh, auth_key = @Auth, patient_id = @PatientId",
            new
            {
                Id = Guid.NewGuid().ToString(), cmd.TenantId, PatientId = cmd.PatientId.ToString(),
                cmd.Request.Endpoint, P256dh = cmd.Request.P256dh, Auth = cmd.Request.Auth
            });
    }
}

// ============================================================
// Portal: Huy web push subscription
// ============================================================
public record PortalPushUnsubscribeCommand(Guid PatientId, int TenantId, string Endpoint) : IRequest;

public class PortalPushUnsubscribeHandler : IRequestHandler<PortalPushUnsubscribeCommand>
{
    private readonly IDapperConnectionFactory _db;
    public PortalPushUnsubscribeHandler(IDapperConnectionFactory db) => _db = db;

    public async Task Handle(PortalPushUnsubscribeCommand cmd, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM diab_his_nti_web_push_subs WHERE endpoint = @Endpoint AND patient_id = @PatientId AND tenant_id = @TenantId",
            new { cmd.Endpoint, PatientId = cmd.PatientId.ToString(), cmd.TenantId });
    }
}
