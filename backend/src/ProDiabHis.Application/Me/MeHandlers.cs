using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Branches;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Me;

public record GetBranchContextQuery : IRequest<Result<BranchContextResponse>>;

public record SwitchBranchRequest(int BranchId);

public record SwitchBranchResponse(string AccessToken, int ExpiresIn, int BranchId);

public record SwitchBranchCommand(int BranchId) : IRequest<Result<SwitchBranchResponse>>;

public class GetBranchContextHandler : IRequestHandler<GetBranchContextQuery, Result<BranchContextResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;

    public GetBranchContextHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IBranchProvider branchProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _branchProvider = branchProvider;
    }

    public async Task<Result<BranchContextResponse>> Handle(GetBranchContextQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var canCrossView = _branchProvider.IgnoreBranchFilter;

        IEnumerable<dynamic> rows;
        if (canCrossView)
        {
            rows = await conn.QueryAsync<dynamic>(
                @"SELECT id, code, name, is_default FROM diab_his_sys_branches
                   WHERE tenant_id = @tenantId AND deleted_at IS NULL AND is_active = 1
                   ORDER BY sort_order, code",
                new { tenantId });
        }
        else
        {
            var allowedIds = _branchProvider.AllowedBranchIds;
            rows = await conn.QueryAsync<dynamic>(
                @"SELECT id, code, name, is_default FROM diab_his_sys_branches
                   WHERE tenant_id = @tenantId AND deleted_at IS NULL AND is_active = 1 AND id IN @allowedIds
                   ORDER BY sort_order, code",
                new { tenantId, allowedIds });
        }

        var branches = rows.Select(r => new BranchOptionDto(
            (int)r.id, (string)r.code, (string)r.name, BranchMapper.ToBool(r.is_default))).ToList();

        return Result<BranchContextResponse>.Success(new BranchContextResponse(
            _branchProvider.BranchId, branches, canCrossView));
    }
}

public class SwitchBranchHandler : IRequestHandler<SwitchBranchCommand, Result<SwitchBranchResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IJwtService _jwtService;
    private readonly IAuditService _auditService;

    public SwitchBranchHandler(IApplicationDbContext db, ICurrentUser currentUser, IBranchProvider branchProvider,
        IJwtService jwtService, IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _branchProvider = branchProvider;
        _jwtService = jwtService;
        _auditService = auditService;
    }

    public async Task<Result<SwitchBranchResponse>> Handle(SwitchBranchCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;
        var tenantId = _currentUser.TenantId!.Value;

        // Super admin / branch.cross_view duoc doi sang bat ky branch nao thuoc tenant.
        // User thuong phai nam trong branch_ids da gan (D7).
        var isAllowed = _branchProvider.IgnoreBranchFilter || _branchProvider.AllowedBranchIds.Contains(request.BranchId);
        if (!isAllowed)
        {
            await _auditService.LogAsync("BRANCH_ACCESS_DENIED", "branch", request.BranchId.ToString(),
                AuditSeverity.WARN, details: new { attempted_branch_id = request.BranchId }, cancellationToken: ct);
            return Result<SwitchBranchResponse>.Failure("BRANCH_ACCESS_DENIED", "Bạn không có quyền truy cập chi nhánh này");
        }

        var branch = await _db.Branches
            .Where(b => b.Id == request.BranchId && b.TenantId == tenantId && b.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        if (branch == null)
            return Result<SwitchBranchResponse>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy chi nhánh");
        if (!branch.IsActive)
            return Result<SwitchBranchResponse>.Failure("BRANCH_INACTIVE", "Chi nhánh đã ngừng hoạt động");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Result<SwitchBranchResponse>.Failure("BRANCH_NOT_FOUND", "Không tìm thấy người dùng");

        var roles = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name).ToList();
        var roleCodes = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Code).ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roles, roleCodes, overrideBranchId: request.BranchId);

        await _auditService.LogAsync("BRANCH_SWITCH", "branch", request.BranchId.ToString(),
            details: new { from_branch_id = _branchProvider.BranchId, to_branch_id = request.BranchId }, cancellationToken: ct);

        return Result<SwitchBranchResponse>.Success(new SwitchBranchResponse(accessToken, 900, request.BranchId));
    }
}
