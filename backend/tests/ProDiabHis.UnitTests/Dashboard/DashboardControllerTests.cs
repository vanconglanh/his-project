using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using ProDiabHis.Api.Controllers;
using ProDiabHis.Application.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Dashboard;

/// <summary>
/// BM-56: endpoint /dashboard/overview dung chung quyen "dashboard.read" (cap cho moi role)
/// nhung tra ca "today_revenue". Chi role co "report.read" moi duoc xem doanh thu.
/// Controller phai null-hoa TodayRevenue voi role thieu report.read.
/// </summary>
public class DashboardControllerTests
{
    private static DashboardOverviewResponse SampleOverview() => new(
        TodayEncounters: 10,
        WaitingPatients: 3,
        TodayRevenue: 1_250_000m,
        LowStockAlerts: 1,
        NearExpiryAlerts: 2,
        BhytPendingCount: 0,
        DtqgFailedCount: 0);

    private static DashboardController BuildController(IMediator mediator, params string[] permissions)
    {
        var controller = new DashboardController(mediator);
        var claims = permissions.Select(p => new Claim("permissions", p)).ToList();
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private static DashboardOverviewResponse ExtractOverview(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value!);
        return data.Should().BeOfType<DashboardOverviewResponse>().Subject;
    }

    [Fact]
    public async Task GetOverview_WithReportRead_KeepsTodayRevenue()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetDashboardOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleOverview());
        var controller = BuildController(mediator, "dashboard.read", "report.read");

        var result = await controller.GetOverview(CancellationToken.None);

        ExtractOverview(result).TodayRevenue.Should().Be(1_250_000m);
    }

    [Fact]
    public async Task GetOverview_WithoutReportRead_NullsTodayRevenue()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetDashboardOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleOverview());
        // Vai tro ky_thuat_vien: co dashboard.read nhung KHONG co report.read.
        var controller = BuildController(mediator, "dashboard.read");

        var result = await controller.GetOverview(CancellationToken.None);

        var overview = ExtractOverview(result);
        overview.TodayRevenue.Should().BeNull();
        // Cac KPI khac van giu nguyen cho moi role.
        overview.TodayEncounters.Should().Be(10);
        overview.WaitingPatients.Should().Be(3);
    }

    [Fact]
    public async Task GetOverview_SuperAdmin_KeepsTodayRevenue()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetDashboardOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleOverview());
        var controller = new DashboardController(mediator);
        var identity = new ClaimsIdentity(new[] { new Claim("is_super_admin", "true") }, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var result = await controller.GetOverview(CancellationToken.None);

        ExtractOverview(result).TodayRevenue.Should().Be(1_250_000m);
    }
}
