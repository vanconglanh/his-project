using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ProDiabHis.Api.Filters;
using Xunit;

namespace ProDiabHis.UnitTests.Permissions;

/// <summary>
/// BM-01 (Lark Bug-Feedback) — le_tan chi duoc xem bao cao trong 1 ngay, khong duoc xem
/// theo khoang ngay/thang/quy. Xem migration 9204_seed_report_date_range_permission.sql.
/// </summary>
public class ReportDateRangeGuardAttributeTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void KhongCoQuyenMoRong_TuNgayKhacDenNgay_Bi403()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: Array.Empty<string>(),
            isSuperAdmin: false,
            arguments: new Dictionary<string, object?> { ["from"] = Today.AddDays(-30), ["to"] = Today });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)ctx.Result!).StatusCode.Should().Be(403);
    }

    [Fact]
    public void KhongCoQuyenMoRong_TuNgayBangDenNgay_ChoPhep()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: Array.Empty<string>(),
            isSuperAdmin: false,
            arguments: new Dictionary<string, object?> { ["from"] = Today, ["to"] = Today });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void KhongCoQuyenMoRong_FromToNull_MacDinhNhieuNgay_Bi403()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: Array.Empty<string>(),
            isSuperAdmin: false,
            arguments: new Dictionary<string, object?> { ["from"] = null, ["to"] = null });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)ctx.Result!).StatusCode.Should().Be(403);
    }

    [Fact]
    public void CoQuyenMoRong_KhoangNgayBatKy_ChoPhep()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: new[] { "report.date_range.extended" },
            isSuperAdmin: false,
            arguments: new Dictionary<string, object?> { ["from"] = Today.AddMonths(-1), ["to"] = Today });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void SuperAdmin_KhoangNgayBatKy_ChoPhep()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: Array.Empty<string>(),
            isSuperAdmin: true,
            arguments: new Dictionary<string, object?> { ["from"] = Today.AddYears(-1), ["to"] = Today });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void ActionKhongCoFromTo_BoQuaGuard()
    {
        var filter = new ReportDateRangeGuardAttribute();
        var ctx = CreateContext(
            permissions: Array.Empty<string>(),
            isSuperAdmin: false,
            arguments: new Dictionary<string, object?> { ["date"] = Today.AddDays(-10) });

        filter.OnActionExecuting(ctx);

        ctx.Result.Should().BeNull();
    }

    private static ActionExecutingContext CreateContext(
        string[] permissions, bool isSuperAdmin, Dictionary<string, object?> arguments)
    {
        var claims = permissions.Select(p => new Claim("permissions", p)).ToList();
        if (isSuperAdmin) claims.Add(new Claim("is_super_admin", "true"));

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var httpCtx = new DefaultHttpContext { User = principal };
        var actionCtx = new ActionContext(httpCtx, new RouteData(), new ActionDescriptor());

        return new ActionExecutingContext(
            actionCtx,
            new List<IFilterMetadata>(),
            arguments!,
            controller: new object());
    }
}
