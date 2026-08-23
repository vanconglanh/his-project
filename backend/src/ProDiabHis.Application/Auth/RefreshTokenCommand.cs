using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Contracts.Auth;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext db,
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters() o day CHI de bo qua filter TenantId cua User/Role (refresh token co the
        // duoc goi truoc khi co tenant context ro rang trong scope hien tai). KHONG duoc de no vo tinh
        // bo qua luon filter Role.DeletedAt == null — loc tuong minh lai ben duoi truoc khi dua vao JWT.
        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existing is null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token invalid or expired");
            return Result<LoginResponse>.Failure("AUTH_INVALID_REFRESH_TOKEN", "Refresh token khong hop le hoac da het han");
        }

        var user = existing.User!;

        // Chi lay UserRole tro toi Role con hieu luc (chua bi xoa mem, dang active). Loc tuong minh
        // o day thay vi dua vao EF global query filter, vi query o tren da IgnoreQueryFilters().
        var activeUserRoles = user.UserRoles
            .Where(ur => ur.Role != null && ur.Role!.DeletedAt == null && ur.Role!.IsActive)
            .ToList();

        var roles = activeUserRoles
            .Select(ur => ur.Role!.Name)
            .ToList();

        // Token rotation
        existing.IsRevoked = true;
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();
        existing.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(newRefreshToken);

        var roleCodes = activeUserRoles.Select(ur => ur.Role!.Code).ToList();
        // Chi lay ma cua nhung role RoleType == System (role seed that) de tinh is_super_admin —
        // role CUSTOM du trung ma ADMIN/SUPER_ADMIN cung KHONG duoc tin tuong (xem JwtService).
        var systemRoleCodes = activeUserRoles
            .Where(ur => ur.Role!.RoleType == RoleType.System)
            .Select(ur => ur.Role!.Code)
            .ToList();
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes);
        await _db.SaveChangesAsync(cancellationToken);

        var roleIds = activeUserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenValue,
            ExpiresIn: 900,
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email,
                FullName: user.FullName,
                TenantId: user.TenantId,
                Roles: roles,
                RoleCodes: roleCodes),
            Permissions: permissions));
    }
}
