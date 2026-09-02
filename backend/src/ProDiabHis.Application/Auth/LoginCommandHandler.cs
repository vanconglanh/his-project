using Dapper;
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
    private readonly IDapperConnectionFactory _dapper;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginCommandHandler> logger,
        IConfiguration configuration,
        IDapperConnectionFactory dapper)
    {
        _db = db;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
        _dapper = dapper;
    }

    /// <summary>Setting key (diab_his_sys_setting_meta / diab_his_sys_settings) — danh sach role_code
    /// bat buoc 2FA, dang CSV. Admin bat/tat qua UI /admin/settings, ap dung ngay lan login sau.</summary>
    private const string MandatoryMfaRolesSettingKey = "security.mandatory_mfa_roles";

    /// <summary>Danh sach role_code bat buoc 2FA (FR-1011). Thu tu uu tien:
    /// 1) Setting UI (key security.mandatory_mfa_roles, CSV) — doc theo tenant cua user (tenant-specific
    ///    row uu tien, fallback row global tenant_id IS NULL). Admin bat/tat qua /admin/settings, ap dung
    ///    ngay lan login sau, KHONG can deploy lai.
    /// 2) appsettings/bien moi truong Security:MandatoryMfaRoles (tuong thich nguoc).
    /// 3) Mac dinh ["admin"] (Quan tri vien) neu chua cau hinh gi — giu hanh vi hien tai.
    /// Luu y: login la anonymous nen ITenantProvider chua co tenant; ta doc scoped theo user.TenantId
    /// (da biet sau khi tim thay user) thay vi qua ISettingsProvider (bind theo request tenant = rong).</summary>
    private async Task<IReadOnlyList<string>> GetMandatoryMfaRoleCodesAsync(int tenantId, CancellationToken ct)
    {
        try
        {
            using var conn = _dapper.CreateConnection();
            // Tenant-specific row uu tien, fallback global (tenant_id IS NULL)
            var raw = await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
                @"SELECT setting_value FROM diab_his_sys_settings
                  WHERE setting_key=@key AND (tenant_id=@tenantId OR tenant_id IS NULL)
                  ORDER BY (tenant_id IS NULL) ASC LIMIT 1",
                new { key = MandatoryMfaRolesSettingKey, tenantId }, cancellationToken: ct));

            // raw != null => co row cau hinh (du la chuoi rong): honor tuyet doi.
            // Chuoi rong = admin da TAT bat buoc 2FA cho moi role (danh sach rong), KHONG fallback default.
            if (raw != null)
            {
                return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }
        catch (Exception ex)
        {
            // Neu bang setting chua migrate hoac loi DB -> fallback config, khong chan login
            _logger.LogWarning(ex, "Khong doc duoc setting {Key}, fallback config", MandatoryMfaRolesSettingKey);
        }

        var section = _configuration.GetSection("Security:MandatoryMfaRoles");
        var fromArray = section.GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        if (fromArray.Length > 0)
            return fromArray!;

        var rawCfg = _configuration["Security:MandatoryMfaRoles"];
        if (!string.IsNullOrWhiteSpace(rawCfg))
            return rawCfg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
        var mandatoryRoles = await GetMandatoryMfaRoleCodesAsync(user.TenantId, cancellationToken);
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
