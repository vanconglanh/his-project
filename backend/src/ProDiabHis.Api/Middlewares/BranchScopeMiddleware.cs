using Dapper;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Middlewares;

/// <summary>
/// Middleware doc claim branch_id/branch_ids/branch_cross_view (+ is_super_admin) tu JWT,
/// ket hop header X-Branch-Id / query ?branchId= de xac dinh branch context cho request.
/// Dang ky NGAY SAU TenantScopeMiddleware (can ITenantProvider.TenantId da duoc set).
/// </summary>
public class BranchScopeMiddleware
{
    private readonly RequestDelegate _next;

    public BranchScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx, IBranchProvider branchProvider, ITenantProvider tenantProvider,
        IDapperConnectionFactory dapper)
    {
        var user = ctx.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await _next(ctx);
            return;
        }

        var isSuperAdmin = user.FindFirst("is_super_admin")?.Value == "true";
        var hasCrossView = isSuperAdmin || user.FindAll("permissions").Any(c => c.Value == "branch.cross_view");

        var branchIdClaim = user.FindFirst("branch_id")?.Value;
        int.TryParse(branchIdClaim, out var defaultBranchId);

        var branchIdsClaim = user.FindFirst("branch_ids")?.Value ?? string.Empty;
        var allowedBranchIds = branchIdsClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : (int?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        // R5: JWT cu khong co claim branch_id -> tra ve DB branch mac dinh cua tenant
        if (defaultBranchId == 0 && tenantProvider.TenantId > 0)
        {
            var conn = dapper.CreateConnection();
            defaultBranchId = await conn.ExecuteScalarAsync<int?>(
                "SELECT id FROM diab_his_sys_branches WHERE tenant_id = @tid AND is_default = 1 AND deleted_at IS NULL LIMIT 1",
                new { tid = tenantProvider.TenantId }) ?? 0;
        }

        // Doc branch dich tu header hoac query string
        var requestedRaw = ctx.Request.Headers["X-Branch-Id"].FirstOrDefault()
            ?? ctx.Request.Query["branchId"].FirstOrDefault();

        int? requestedBranchId = null;
        var requestedAll = false;
        if (!string.IsNullOrWhiteSpace(requestedRaw))
        {
            if (requestedRaw.Equals("all", StringComparison.OrdinalIgnoreCase))
                requestedAll = true;
            else if (int.TryParse(requestedRaw, out var rb))
                requestedBranchId = rb;
        }

        if (requestedAll)
        {
            if (!hasCrossView)
            {
                await WriteForbidden(ctx, defaultBranchId);
                return;
            }
            branchProvider.SetContext(0, true, allowedBranchIds);
            await _next(ctx);
            return;
        }

        if (requestedBranchId.HasValue)
        {
            if (!hasCrossView && !allowedBranchIds.Contains(requestedBranchId.Value))
            {
                await WriteForbidden(ctx, requestedBranchId.Value);
                return;
            }

            if (hasCrossView)
            {
                // Van phai verify branch thuoc dung tenant hien tai (chan cross-tenant qua X-Branch-Id)
                var conn = dapper.CreateConnection();
                var belongsToTenant = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM diab_his_sys_branches WHERE id = @id AND tenant_id = @tid AND deleted_at IS NULL",
                    new { id = requestedBranchId.Value, tid = tenantProvider.TenantId });
                if (belongsToTenant == 0)
                {
                    await WriteForbidden(ctx, requestedBranchId.Value);
                    return;
                }
            }

            branchProvider.SetContext(requestedBranchId.Value, false, allowedBranchIds);
            await _next(ctx);
            return;
        }

        // Khong truyen branch nao
        if (hasCrossView)
        {
            branchProvider.SetContext(defaultBranchId, true, allowedBranchIds);
        }
        else
        {
            branchProvider.SetContext(defaultBranchId, false, allowedBranchIds);
        }

        await _next(ctx);
    }

    private static async Task WriteForbidden(HttpContext ctx, int targetBranchId)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "BRANCH_ACCESS_DENIED",
                message = "Bạn không có quyền truy cập chi nhánh này",
                details = new { branch_id = targetBranchId }
            }
        });
    }
}
