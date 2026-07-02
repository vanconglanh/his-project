using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Users;

public record InviteUserCommand(
    string Email,
    string FullName,
    string? Phone,
    IEnumerable<string> RoleCodes
) : IRequest<Result<InviteUserResponse>>;

public record InviteUserResponse(Guid UserId, string Email, DateTime InviteExpiresAt);

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .WithMessage("Email không hợp lệ");
        RuleFor(x => x.FullName).NotEmpty()
            .WithMessage("Họ tên không được để trống");
        RuleFor(x => x.RoleCodes).NotEmpty()
            .WithMessage("Phải chọn ít nhất một vai trò");
    }
}

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result<InviteUserResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ITenantProvider _tenantProvider;
    private readonly IAuditService _audit;
    private readonly ILogger<InviteUserCommandHandler> _logger;

    public InviteUserCommandHandler(
        IApplicationDbContext db,
        IEmailSender emailSender,
        ITenantProvider tenantProvider,
        IAuditService audit,
        ILogger<InviteUserCommandHandler> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _tenantProvider = tenantProvider;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<InviteUserResponse>> Handle(InviteUserCommand req, CancellationToken ct)
    {
        // Kiem tra email trung trong tenant
        var tenantId = _tenantProvider.TenantId;
        var emailExists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == req.Email && u.TenantId == tenantId && u.DeletedAt == null, ct);
        if (emailExists)
            return Result<InviteUserResponse>.Failure("USER_EMAIL_EXISTS", "Email đã được đăng ký");

        // Kiem tra role codes hop le
        var roleCodes = req.RoleCodes.ToList();
        var roles = await _db.Roles.IgnoreQueryFilters()
            .Where(r => roleCodes.Contains(r.Code) && r.DeletedAt == null)
            .ToListAsync(ct);

        if (roles.Count != roleCodes.Count)
            return Result<InviteUserResponse>.Failure("ROLE_NOT_FOUND", "Một hoặc nhiều vai trò không tồn tại");

        var inviteToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Email = req.Email,
            FullName = req.FullName,
            Phone = req.Phone,
            PasswordHash = string.Empty,
            Status = UserStatus.Pending,
            InviteToken = inviteToken,
            InviteTokenExpiresAt = expiresAt
        };

        _db.Users.Add(user);

        foreach (var role in roles)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = _tenantProvider.TenantId
            });
        }

        await _db.SaveChangesAsync(ct);

        var inviteUrl = $"https://app.prodiab.vn/accept-invite?token={inviteToken}";
        var emailBody = BuildInviteEmail(req.FullName, inviteUrl);
        await _emailSender.SendAsync(req.Email, "Bạn được mời tham gia Pro-Diab HIS", emailBody, ct);

        await _audit.LogAsync(AuditAction.Create, "user", user.Id.ToString(),
            new { email = req.Email, roles = roleCodes }, ct);

        _logger.LogInformation("Moi user {Email} thanh cong, token het han {ExpiresAt}", req.Email, expiresAt);

        return Result<InviteUserResponse>.Success(new InviteUserResponse(user.Id, user.Email, expiresAt));
    }

    private static string BuildInviteEmail(string fullName, string inviteUrl) => $"""
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
          <h2>Xin chào {fullName},</h2>
          <p>Bạn được mời tham gia hệ thống <strong>Pro-Diab HIS</strong>.</p>
          <p>Vui lòng nhấn nút bên dưới để kích hoạt tài khoản và đặt mật khẩu:</p>
          <a href="{inviteUrl}" style="
              display: inline-block;
              padding: 12px 24px;
              background-color: #2563eb;
              color: white;
              text-decoration: none;
              border-radius: 6px;
              margin: 16px 0;">
            Kích hoạt tài khoản
          </a>
          <p style="color: #6b7280; font-size: 14px;">Liên kết này có hiệu lực trong 7 ngày.</p>
          <hr/>
          <p style="color: #9ca3af; font-size: 12px;">Pro-Diab HIS — Hệ thống quản lý phòng khám</p>
        </body>
        </html>
        """;
}
