using System.Text.Json;
using ProDiabHis.Application.Auth.Sso;
using StackExchange.Redis;

namespace ProDiabHis.Infrastructure.Sso;

/// <summary>State SSO luu Redis (khong dung ASP.NET Data Protection -> khong phu thuoc key persist).</summary>
public class RedisSsoStateStore : ISsoStateStore
{
    private readonly IConnectionMultiplexer _redis;
    public RedisSsoStateStore(IConnectionMultiplexer redis) => _redis = redis;
    public bool IsAvailable => true;
    private IDatabase Db => _redis.GetDatabase();

    public Task SaveStateAsync(string state, SsoStateData data, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:state:" + state, JsonSerializer.Serialize(data), ttl);

    public async Task<SsoStateData?> ConsumeStateAsync(string state, CancellationToken ct)
    {
        var v = await Db.StringGetDeleteAsync("sso:state:" + state); // GETDEL
        return v.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SsoStateData>((string)v!);
    }

    public Task<bool> TryMarkLogoutJtiAsync(string jti, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:logout-jti:" + jti, "1", ttl, When.NotExists);

    public Task RevokeSidAsync(string sid, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:revoked-sid:" + sid, "1", ttl);

    public Task<bool> IsSidRevokedAsync(string sid, CancellationToken ct)
        => Db.KeyExistsAsync("sso:revoked-sid:" + sid);

    public Task SaveIdTokenHintAsync(string sid, string idToken, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:idt:" + sid, idToken, ttl);

    public async Task<string?> GetIdTokenHintAsync(string sid, CancellationToken ct)
    {
        var v = await Db.StringGetAsync("sso:idt:" + sid);
        return v.IsNullOrEmpty ? null : (string)v!;
    }

    public Task SaveMfaLinkAsync(Guid userId, SsoSessionInfo info, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:mfa:" + userId, JsonSerializer.Serialize(info), ttl);

    public async Task<SsoSessionInfo?> TakeMfaLinkAsync(Guid userId, CancellationToken ct)
    {
        var v = await Db.StringGetDeleteAsync("sso:mfa:" + userId);
        return v.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SsoSessionInfo>((string)v!);
    }

    public Task SaveTicketAsync(string ticket, ProDiabHis.Contracts.Auth.LoginResponse login, TimeSpan ttl, CancellationToken ct)
        => Db.StringSetAsync("sso:ticket:" + ticket, JsonSerializer.Serialize(login), ttl);

    public async Task<ProDiabHis.Contracts.Auth.LoginResponse?> TakeTicketAsync(string ticket, CancellationToken ct)
    {
        var v = await Db.StringGetDeleteAsync("sso:ticket:" + ticket);
        return v.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ProDiabHis.Contracts.Auth.LoginResponse>((string)v!);
    }
}
