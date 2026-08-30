using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Contracts.Auth;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginCommandHandler> logger,
        IConfiguration configuration)
    {
        _db = db;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>Danh sach role_code bat buoc 2FA (FR-1011), cau hinh qua Security:MandatoryMfaRoles
    /// (CSV hoac mang trong appsettings). Mac dinh chi "admin" (Quan tri vien) vi he thong hien khong co
    /// role rieng "quan_ly_chi_nhanh" — Quan ly chi nhanh trong SRS duoc anh xa toi role "admin" (role duy
    /// nhat co quyen branch.create/update/delete/assign_user, xem db/migrations/9086_seed_branch_permissions.sql).</summary>
    private IReadOnlyList<string> GetMandatoryMfaRoleCodes()
    {
        var section = _configuration.GetSection("Security:MandatoryMfaRoles");
        var fromArray = section.GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        if (fromArray.Length > 0)
            return fromArray!;

        var raw = _configuration["Security:MandatoryMfaRoles"];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return new[] { "admin" };
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Tim user theo email (cross-tenant cho login)
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .IgnoreQueryFilters()
            .Where(u => u.Email == request.Email && u.DeletedAt == null && u.IsActive
                && u.Status == Domain.Entities.UserStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed: user not found for email {Email}", request.Email);
            return Result<LoginResponse>.Failure("AUTH_INVALID_CREDENTIALS", "Email hoac mat khau khong dung");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            return Result<LoginResponse>.Failure("AUTH_INVALID_CREDENTIALS", "Email hoac mat khau khong dung");
        }

        var roleCodes = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .ToList();

        // P0 (bao mat): user DA bat 2FA -> KHONG cap token day du. Tra ve buoc 1 (requires2fa) kem
        // mfa-pending token. Client phai goi POST /api/v1/auth/2fa/verify voi ma TOTP de lay token.
        // KHONG cap nhat LastLoginAt vi dang nhap chua hoan tat.
        if (user.TwoFaEnabled)
        {
            _logger.LogInformation("User {UserId} da bat 2FA - yeu cau nhap ma TOTP truoc khi cap token", user.Id);
            return Result<LoginResponse>.Success(new LoginResponse(
                AccessToken: "",
                RefreshToken: "",
                ExpiresIn: 0,
                User: new UserInfo(user.Id, user.Email, user.FullName, user.TenantId,
                    Roles: Array.Empty<string>(), RoleCodes: Array.Empty<string>()),
                Permissions: Array.Empty<string>(),
                MfaSetupRequired: false,
                MfaSetupMessage: null,
                Requires2fa: true,
                MfaPendingToken: _jwtService.GenerateMfaPendingToken(user)));
        }

        // FR-1011: 2FA bat buoc cho role trong danh sach cau hinh (vd admin/quan ly chi nhanh) khi
        // user chua bat 2FA. P0: KHONG cap token day du nua (truoc day chi canh bao, bypass duoc).
        // Tra ve mfa-setup token de client bat 2FA lan dau qua me/2fa/setup + me/2fa/enable.
        var mandatoryRoles = GetMandatoryMfaRoleCodes();
        var isMandatoryMfaRole = roleCodes.Any(rc => mandatoryRoles.Contains(rc, StringComparer.OrdinalIgnoreCase));
        if (isMandatoryMfaRole && !user.TwoFaEnabled)
        {
            _logger.LogWarning(
                "User {UserId} thuoc role bat buoc 2FA nhung chua bat 2FA - yeu cau thiet lap truoc khi cap token",
                user.Id);
            return Result<LoginResponse>.Success(new LoginResponse(
                AccessToken: "",
                RefreshToken: "",
                ExpiresIn: 0,
                User: new UserInfo(user.Id, user.Email, user.FullName, user.TenantId,
                    Roles: Array.Empty<string>(), RoleCodes: Array.Empty<string>()),
                Permissions: Array.Empty<string>(),
                MfaSetupRequired: true,
                MfaSetupMessage: "Tài khoản của bạn thuộc nhóm vai trò bắt buộc bật xác thực hai lớp (2FA). Vui lòng thiết lập 2FA ngay để tiếp tục sử dụng hệ thống.",
                MfaSetupToken: _jwtService.GenerateMfaSetupToken(user)));
        }

        // Luong binh thuong: cap token day du.
        return Result<LoginResponse>.Success(await BuildSuccessResponseAsync(user, cancellationToken));
    }

    /// <summary>Tao AccessToken + RefreshToken + cap nhat LastLoginAt + build LoginResponse day du.
    /// Dung chung cho login binh thuong va buoc verify TOTP (Verify2faLoginCommand).</summary>
    public async Task<LoginResponse> BuildSuccessResponseAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var roleCodes = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .ToList();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user, roles, roleCodes);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresIn: 900,
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email,
                FullName: user.FullName,
                TenantId: user.TenantId,
                Roles: roles,
                RoleCodes: roleCodes),
            Permissions: permissions);
    }
}
