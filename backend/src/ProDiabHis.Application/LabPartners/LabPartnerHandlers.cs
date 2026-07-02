using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.LabPartners;

// ═══════════════════════════════════════════════
// COMMANDS / QUERIES
// ═══════════════════════════════════════════════
public record ListLabPartnersQuery(string? Status, string? Q)
    : IRequest<Result<IReadOnlyList<LabPartnerResponse>>>;

public record GetLabPartnerQuery(Guid Id)
    : IRequest<Result<LabPartnerResponse>>;

public record CreateLabPartnerCommand(LabPartnerCreateRequest Req)
    : IRequest<Result<LabPartnerResponse>>;

public record UpdateLabPartnerCommand(Guid Id, LabPartnerUpdateRequest Req)
    : IRequest<Result<bool>>;

public record DeleteLabPartnerCommand(Guid Id)
    : IRequest<Result<bool>>;

public record TestLabPartnerConnectionCommand(Guid Id)
    : IRequest<Result<TestConnectionResponse>>;

public record UpdateLabPartnerCredentialsCommand(Guid Id, LabPartnerCredentialsRequest Req)
    : IRequest<Result<bool>>;

public record RotateLabPartnerApiKeyCommand(Guid Id)
    : IRequest<Result<RotateApiKeyResponse>>;

// ═══════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════
file static class Mapper
{
    public static LabPartnerResponse Map(LabPartner e) => new(
        e.Id,
        e.TenantId,
        e.Code,
        e.Name,
        e.EndpointUrl,
        e.AuthType,
        e.ApiKeyMasked,
        e.Transport,
        e.SupportedTests is not null
            ? JsonSerializer.Deserialize<List<string>>(e.SupportedTests) ?? new()
            : new(),
        e.Status,
        e.ContactEmail,
        e.ContactPhone,
        e.CreatedAt,
        e.UpdatedAt);
}

file static class MaskKey
{
    public static string Mask(string key) =>
        key.Length <= 6 ? "***" : key[..3] + "***" + key[^3..];
}

// ═══════════════════════════════════════════════
// LIST
// ═══════════════════════════════════════════════
public class ListLabPartnersQueryHandler
    : IRequestHandler<ListLabPartnersQuery, Result<IReadOnlyList<LabPartnerResponse>>>
{
    private readonly IApplicationDbContext _db;

    public ListLabPartnersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<LabPartnerResponse>>> Handle(
        ListLabPartnersQuery q, CancellationToken ct)
    {
        var query = _db.LabPartners.AsQueryable();

        if (!string.IsNullOrEmpty(q.Status))
            query = query.Where(e => e.Status == q.Status);

        if (!string.IsNullOrEmpty(q.Q))
            query = query.Where(e => e.Name.Contains(q.Q) || e.Code.Contains(q.Q));

        var items = await query
            .OrderBy(e => e.Name)
            .Select(e => Mapper.Map(e))
            .ToListAsync(ct);

        return Result<IReadOnlyList<LabPartnerResponse>>.Success(items);
    }
}

// ═══════════════════════════════════════════════
// GET
// ═══════════════════════════════════════════════
public class GetLabPartnerQueryHandler
    : IRequestHandler<GetLabPartnerQuery, Result<LabPartnerResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetLabPartnerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<LabPartnerResponse>> Handle(GetLabPartnerQuery q, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == q.Id, ct);
        if (entity is null)
            return Result<LabPartnerResponse>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        return Result<LabPartnerResponse>.Success(Mapper.Map(entity));
    }
}

// ═══════════════════════════════════════════════
// CREATE
// ═══════════════════════════════════════════════
public class CreateLabPartnerCommandHandler
    : IRequestHandler<CreateLabPartnerCommand, Result<LabPartnerResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IEncryptionService _enc;

    public CreateLabPartnerCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IEncryptionService enc)
    { _db = db; _tenant = tenant; _user = user; _enc = enc; }

    public async Task<Result<LabPartnerResponse>> Handle(CreateLabPartnerCommand cmd, CancellationToken ct)
    {
        var req = cmd.Req;

        byte[]? encApiKey    = req.ApiKey is not null ? Encoding.UTF8.GetBytes(_enc.Encrypt(req.ApiKey)) : null;
        byte[]? encBearer    = req.BearerToken is not null ? Encoding.UTF8.GetBytes(_enc.Encrypt(req.BearerToken)) : null;
        string? apiKeyMasked = req.ApiKey is not null ? MaskKey.Mask(req.ApiKey) : null;
        string? testsJson    = req.SupportedTests is not null ? JsonSerializer.Serialize(req.SupportedTests) : null;

        var entity = new LabPartner
        {
            TenantId              = _tenant.TenantId,
            Code                  = req.Code,
            Name                  = req.Name,
            EndpointUrl           = req.EndpointUrl,
            AuthType              = req.AuthType,
            ApiKeyEncrypted       = encApiKey,
            BearerTokenEncrypted  = encBearer,
            ApiKeyMasked          = apiKeyMasked,
            Transport             = req.Transport,
            SupportedTests        = testsJson,
            Status                = "INACTIVE",
            ContactEmail          = req.ContactEmail,
            ContactPhone          = req.ContactPhone,
            CreatedBy             = _user.UserId,
        };

        _db.LabPartners.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<LabPartnerResponse>.Success(Mapper.Map(entity));
    }
}

// ═══════════════════════════════════════════════
// UPDATE
// ═══════════════════════════════════════════════
public class UpdateLabPartnerCommandHandler
    : IRequestHandler<UpdateLabPartnerCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateLabPartnerCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<bool>> Handle(UpdateLabPartnerCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<bool>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        var req = cmd.Req;
        if (req.Name is not null)          entity.Name          = req.Name;
        if (req.EndpointUrl is not null)   entity.EndpointUrl   = req.EndpointUrl;
        if (req.Transport is not null)     entity.Transport     = req.Transport;
        if (req.SupportedTests is not null) entity.SupportedTests = JsonSerializer.Serialize(req.SupportedTests);
        if (req.Status is not null)        entity.Status        = req.Status;
        if (req.ContactEmail is not null)  entity.ContactEmail  = req.ContactEmail;
        if (req.ContactPhone is not null)  entity.ContactPhone  = req.ContactPhone;
        entity.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// DELETE (soft)
// ═══════════════════════════════════════════════
public class DeleteLabPartnerCommandHandler
    : IRequestHandler<DeleteLabPartnerCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public DeleteLabPartnerCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<bool>> Handle(DeleteLabPartnerCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<bool>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// TEST CONNECTION
// ═══════════════════════════════════════════════
public class TestLabPartnerConnectionCommandHandler
    : IRequestHandler<TestLabPartnerConnectionCommand, Result<TestConnectionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _enc;
    private readonly ILabPartnerClient _client;

    public TestLabPartnerConnectionCommandHandler(IApplicationDbContext db,
        IEncryptionService enc, ILabPartnerClient client)
    { _db = db; _enc = enc; _client = client; }

    public async Task<Result<TestConnectionResponse>> Handle(
        TestLabPartnerConnectionCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<TestConnectionResponse>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        string? apiKey = null;
        string? bearer = null;

        try
        {
            if (entity.ApiKeyEncrypted is not null)
                apiKey = _enc.Decrypt(Encoding.UTF8.GetString(entity.ApiKeyEncrypted));
            if (entity.BearerTokenEncrypted is not null)
                bearer = _enc.Decrypt(Encoding.UTF8.GetString(entity.BearerTokenEncrypted));
        }
        catch
        {
            return Result<TestConnectionResponse>.Failure("LAB_PARTNER_AUTH_INVALID",
                "Không thể giải mã credentials");
        }

        var testResult = await _client.TestConnectionAsync(
            entity.EndpointUrl, entity.AuthType, apiKey, bearer, ct);

        if (!testResult.Ok)
            return Result<TestConnectionResponse>.Failure("LAB_PARTNER_CONNECTION_FAILED", testResult.Message);

        return Result<TestConnectionResponse>.Success(testResult);
    }
}

// ═══════════════════════════════════════════════
// UPDATE CREDENTIALS
// ═══════════════════════════════════════════════
public class UpdateLabPartnerCredentialsCommandHandler
    : IRequestHandler<UpdateLabPartnerCredentialsCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _enc;
    private readonly ICurrentUser _user;

    public UpdateLabPartnerCredentialsCommandHandler(IApplicationDbContext db,
        IEncryptionService enc, ICurrentUser user)
    { _db = db; _enc = enc; _user = user; }

    public async Task<Result<bool>> Handle(UpdateLabPartnerCredentialsCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<bool>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        var req = cmd.Req;
        entity.AuthType = req.AuthType;

        if (req.ApiKey is not null)
        {
            entity.ApiKeyEncrypted = Encoding.UTF8.GetBytes(_enc.Encrypt(req.ApiKey));
            entity.ApiKeyMasked    = MaskKey.Mask(req.ApiKey);
        }
        if (req.BearerToken is not null)
            entity.BearerTokenEncrypted = Encoding.UTF8.GetBytes(_enc.Encrypt(req.BearerToken));

        entity.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════
// ROTATE API KEY
// ═══════════════════════════════════════════════
public class RotateLabPartnerApiKeyCommandHandler
    : IRequestHandler<RotateLabPartnerApiKeyCommand, Result<RotateApiKeyResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _enc;
    private readonly ICurrentUser _user;

    public RotateLabPartnerApiKeyCommandHandler(IApplicationDbContext db,
        IEncryptionService enc, ICurrentUser user)
    { _db = db; _enc = enc; _user = user; }

    public async Task<Result<RotateApiKeyResponse>> Handle(RotateLabPartnerApiKeyCommand cmd, CancellationToken ct)
    {
        var entity = await _db.LabPartners.FirstOrDefaultAsync(e => e.Id == cmd.Id, ct);
        if (entity is null)
            return Result<RotateApiKeyResponse>.Failure("LAB_PARTNER_NOT_FOUND", "Không tìm thấy đối tác lab");

        var newKey  = "sk_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var masked  = MaskKey.Mask(newKey);
        var rotated = DateTime.UtcNow;

        entity.ApiKeyEncrypted = Encoding.UTF8.GetBytes(_enc.Encrypt(newKey));
        entity.ApiKeyMasked    = masked;
        entity.UpdatedBy       = _user.UserId;

        await _db.SaveChangesAsync(ct);
        return Result<RotateApiKeyResponse>.Success(new(masked, rotated));
    }
}
