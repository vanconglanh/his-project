using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record AcceptInviteCommand(
    string Token,
    string Password,
    string? FullName
) : IRequest<Result<AcceptInviteResponse>>;

public record AcceptInviteResponse(UserResponse User, string AccessToken, string RefreshToken);

public class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().Must(BeStrongPassword)
            .WithMessage("Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt");
    }

    private static bool BeStrongPassword(string pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 12) return false;
        return pwd.Any(char.IsUpper)
            && pwd.Any(char.IsLower)
            && pwd.Any(char.IsDigit)
            && pwd.Any(c => !char.IsLetterOrDigit(c));
    }
}

public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Result<AcceptInviteResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AcceptInviteCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AcceptInviteResponse>> Handle(AcceptInviteCommand req, CancellationToken ct)
    {
        // IgnoreQueryFilters() o day CHI de bo qua filter TenantId cua User/Role (accept-invite duoc
        // goi truoc khi user dang nhap, chua co tenant context). KHONG duoc de no vo tinh bo qua luon
        // filter Role.DeletedAt == null — loc tuong minh lai ben duoi truoc khi dua vao JWT.
        var user = await _db.Users.IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.InviteToken == req.Token && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<AcceptInviteResponse>.Failure("USER_INVITE_EXPIRED", "Liên kết mời đã hết hạn");

        if (user.InviteTokenExpiresAt < DateTime.UtcNow)
            return Result<AcceptInviteResponse>.Failure("USER_INVITE_EXPIRED", "Liên kết mời đã hết hạn");

        // Validate password strength
        if (!IsStrongPassword(req.Password))
            return Result<AcceptInviteResponse>.Failure("PASSWORD_TOO_WEAK",
                "Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt");

        user.PasswordHash = _passwordHasher.Hash(req.Password);
        user.Status = UserStatus.Active;
        user.IsActive = true;
        if (!string.IsNullOrEmpty(req.FullName)) user.FullName = req.FullName;
        user.InviteToken = null;
        user.InviteTokenExpiresAt = null;

        // Chi lay UserRole tro toi Role con hieu luc (chua bi xoa mem, dang active). Loc tuong minh
        // o day thay vi dua vao EF global query filter, vi query o tren da IgnoreQueryFilters().
        var activeUserRoles = user.UserRoles
            .Where(ur => ur.Role != null && ur.Role!.DeletedAt == null && ur.Role!.IsActive)
            .ToList();

        var roles = activeUserRoles
            .Select(ur => ur.Role!.Name)
            .ToList();
        var roleCodes = activeUserRoles
            .Select(ur => ur.Role!.Code)
            .ToList();

        // Chi lay ma cua nhung role RoleType == System (role seed that) de tinh is_super_admin —
        // role CUSTOM du trung ma ADMIN/SUPER_ADMIN cung KHONG duoc tin tuong (xem JwtService).
        var systemRoleCodes = activeUserRoles
            .Where(ur => ur.Role!.RoleType == RoleType.System)
            .Select(ur => ur.Role!.Code)
            .ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roles, roleCodes, systemRoleCodes);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<AcceptInviteResponse>.Success(
            new AcceptInviteResponse(user.ToResponse(), accessToken, refreshTokenValue));
    }

    private static bool IsStrongPassword(string pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 12) return false;
        return pwd.Any(char.IsUpper)
            && pwd.Any(char.IsLower)
            && pwd.Any(char.IsDigit)
            && pwd.Any(c => !char.IsLetterOrDigit(c));
    }
}
