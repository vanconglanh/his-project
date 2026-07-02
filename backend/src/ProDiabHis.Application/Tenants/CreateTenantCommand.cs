using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Tenants;

public record CreateTenantCommand(
    string Code,
    string Name,
    string? CskcbCode,
    string? TaxCode,
    string? Address,
    string? Phone,
    string Email,
    string Subdomain,
    int StorageQuotaGb,
    string AdminEmail,
    string AdminFullName,
    DateTime? ExpiresAt
) : IRequest<Result<TenantResponse>>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().Matches(@"^[A-Z0-9]{3,20}$")
            .WithMessage("Mã phòng khám phải từ 3-20 ký tự, chỉ gồm chữ hoa và số");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .WithMessage("Tên phòng khám không được để trống và tối đa 200 ký tự");
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .WithMessage("Email phòng khám không hợp lệ");
        RuleFor(x => x.Subdomain).NotEmpty().Matches(@"^[a-z0-9-]{3,63}$")
            .WithMessage("Subdomain phải từ 3-63 ký tự, chỉ gồm chữ thường, số và dấu gạch ngang");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress()
            .WithMessage("Email admin không hợp lệ");
        RuleFor(x => x.AdminFullName).NotEmpty()
            .WithMessage("Họ tên admin không được để trống");
        RuleFor(x => x.StorageQuotaGb).InclusiveBetween(1, 1000);
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        IApplicationDbContext db,
        IEmailSender emailSender,
        IPasswordHasher passwordHasher,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<TenantResponse>> Handle(CreateTenantCommand req, CancellationToken ct)
    {
        // Kiem tra subdomain trung
        var subdomainExists = await _db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Subdomain == req.Subdomain && t.DeletedAt == null, ct);
        if (subdomainExists)
            return Result<TenantResponse>.Failure("TENANT_SUBDOMAIN_TAKEN", "Subdomain đã được sử dụng");

        // Kiem tra code trung
        var codeExists = await _db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Code == req.Code && t.DeletedAt == null, ct);
        if (codeExists)
            return Result<TenantResponse>.Failure("TENANT_CODE_TAKEN", "Mã phòng khám đã tồn tại");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Code = req.Code,
            Name = req.Name,
            CskcbCode = req.CskcbCode,
            TaxCode = req.TaxCode,
            Address = req.Address,
            Phone = req.Phone,
            Email = req.Email,
            Subdomain = req.Subdomain,
            StorageQuotaGb = req.StorageQuotaGb,
            Status = Domain.Entities.TenantStatus.Active,
            ExpiresAt = req.ExpiresAt
        };

        _db.Tenants.Add(tenant);

        // Tao user ADMIN dau tien cho tenant
        var inviteToken = GenerateInviteToken();
        var adminRole = await _db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Code == "ADMIN" && r.DeletedAt == null, ct);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = req.AdminEmail,
            FullName = req.AdminFullName,
            PasswordHash = string.Empty, // Se set khi accept invite
            Status = UserStatus.Pending,
            InviteToken = inviteToken,
            InviteTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Users.Add(adminUser);

        if (adminRole != null)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                TenantId = 0 // se update sau
            });
        }

        await _db.SaveChangesAsync(ct);

        // Gui email moi
        var inviteUrl = $"https://{req.Subdomain}.prodiab.vn/accept-invite?token={inviteToken}";
        var emailBody = BuildInviteEmail(req.AdminFullName, req.Name, inviteUrl);
        await _emailSender.SendAsync(req.AdminEmail, $"Mời bạn kích hoạt tài khoản {req.Name}", emailBody, ct);

        _logger.LogInformation("Tao tenant {Code} thanh cong, da gui email moi den {AdminEmail}", req.Code, req.AdminEmail);
        return Result<TenantResponse>.Success(tenant.ToResponse());
    }

    private static string GenerateInviteToken()
        => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();

    private static string BuildInviteEmail(string fullName, string tenantName, string inviteUrl) => $"""
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
          <h2>Xin chào {fullName},</h2>
          <p>Bạn được mời làm Quản trị viên của <strong>{tenantName}</strong> trên hệ thống Pro-Diab HIS.</p>
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
