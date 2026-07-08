using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Tenants;

public record UpdateMyTenantCommand(
    string? Name,
    string? Address,
    string? Phone,
    string? Email,
    string? CskcbCode,
    string? BhytToken,
    string? CompanyName,
    string? EmailSupport,
    string? LogoUrl,
    string? Slogan,
    string? Website
) : IRequest<Result<TenantResponse>>;

public class UpdateMyTenantCommandHandler : IRequestHandler<UpdateMyTenantCommand, Result<TenantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEncryptionService _encryption;

    public UpdateMyTenantCommandHandler(
        IApplicationDbContext db,
        ITenantProvider tenantProvider,
        IEncryptionService encryption)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _encryption = encryption;
    }

    public async Task<Result<TenantResponse>> Handle(UpdateMyTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.DeletedAt == null, ct);

        if (tenant is null)
            return Result<TenantResponse>.Failure("TENANT_NOT_FOUND", "Không tìm thấy phòng khám");

        if (req.Name != null) tenant.Name = req.Name;
        if (req.Address != null) tenant.Address = req.Address;
        if (req.Phone != null) tenant.Phone = req.Phone;
        if (req.Email != null) tenant.Email = req.Email;
        if (req.CskcbCode != null) tenant.CskcbCode = req.CskcbCode;
        if (req.BhytToken != null) tenant.BhytTokenEncrypted = _encryption.Encrypt(req.BhytToken);
        if (req.CompanyName != null) tenant.CompanyName = req.CompanyName;
        if (req.EmailSupport != null) tenant.EmailSupport = req.EmailSupport;
        if (req.LogoUrl != null) tenant.LogoUrl = req.LogoUrl;
        if (req.Slogan != null) tenant.Slogan = req.Slogan;
        if (req.Website != null) tenant.Website = req.Website;

        await _db.SaveChangesAsync(ct);
        return Result<TenantResponse>.Success(tenant.ToResponse());
    }
}
