using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Roles;

public record CreateRoleCommand(
    string Code,
    string Name,
    string? Description,
    IEnumerable<string> PermissionCodes
) : IRequest<Result<RoleResponse>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches(@"^[A-Z][A-Z0-9_]{2,30}$")
            .WithMessage("Mã vai trò phải từ 3-31 ký tự, bắt đầu bằng chữ hoa, chỉ dùng chữ hoa số và dấu _");
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.PermissionCodes).NotEmpty()
            .WithMessage("Phải chọn ít nhất một quyền");
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateRoleCommandHandler(IApplicationDbContext db, ITenantProvider tenantProvider,
        ICurrentUser user, IAuditService audit)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _user = user;
        _audit = audit;
    }

    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand req, CancellationToken ct)
    {
        // Chan tu goc: khong cho tenant tu tao role CUSTOM voi ma trung role he thong (ADMIN,
        // SUPER_ADMIN...) — neu khong se bi loi dung de gia mao super admin (xem JwtService).
        if (ReservedRoleCodes.IsReserved(req.Code))
            return Result<RoleResponse>.Failure(
                "ROLE_CODE_RESERVED",
                "Mã vai trò này được dành riêng cho vai trò hệ thống, không thể sử dụng để tạo vai trò tùy chỉnh");

        var codeExists = await _db.Roles.IgnoreQueryFilters()
            .AnyAsync(r => r.Code == req.Code && r.DeletedAt == null, ct);
        if (codeExists)
            return Result<RoleResponse>.Failure("ROLE_CODE_TAKEN", "Mã vai trò đã tồn tại");

        var permCodes = req.PermissionCodes.ToList();
        var permissions = await _db.Permissions
            .Where(p => permCodes.Contains(p.Code))
            .ToListAsync(ct);

        if (permissions.Count != permCodes.Count)
            return Result<RoleResponse>.Failure("PERMISSION_NOT_FOUND", "Một hoặc nhiều quyền không tồn tại");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = req.Code,
            Name = req.Name,
            Description = req.Description,
            RoleType = RoleType.Custom,
            TenantId = _tenantProvider.TenantId,
            IsActive = true
        };

        _db.Roles.Add(role);

        foreach (var perm in permissions)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
        }

        await _db.SaveChangesAsync(ct);

        // Reload voi permissions
        role.RolePermissions = permissions.Select(p => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = p.Id,
            Permission = p
        }).ToList();

        // Audit: tao vai tro moi la thao tac nhay cam (role la vector leo thang quyen)
        await _audit.LogAsync("CREATE", "ROLE", role.Id.ToString(),
            new { code = role.Code, name = role.Name, permissionCodes = permCodes, tenantId = role.TenantId, userId = _user.UserId },
            ct);

        return Result<RoleResponse>.Success(role.ToResponse());
    }
}
