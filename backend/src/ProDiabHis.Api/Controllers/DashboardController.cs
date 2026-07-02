using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Dashboard tong quan — Sprint 11 EPIC 9 US-RP08.
/// Route: /api/v1/dashboard/*
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>KPI tong quan hom nay</summary>
    [HttpGet("overview")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardOverviewQuery(), ct);
        return Ok(new { data = result });
    }

    /// <summary>Bieu do xu huong doanh thu</summary>
    [HttpGet("charts/revenue-trend")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetRevenueTrend(
        [FromQuery] string range = "30d",
        CancellationToken ct = default)
    {
        var series = await _mediator.Send(new GetRevenueTrendChartQuery(range), ct);
        return Ok(new { data = new { series } });
    }

    /// <summary>Bieu do xu huong luot kham</summary>
    [HttpGet("charts/encounters-trend")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetEncountersTrend(
        [FromQuery] string range = "30d",
        CancellationToken ct = default)
    {
        var series = await _mediator.Send(new GetEncountersTrendChartQuery(range), ct);
        return Ok(new { data = new { series } });
    }

    /// <summary>Top bac si (stub)</summary>
    [HttpGet("charts/top-doctors")]
    [RequirePermission("dashboard.read")]
    public IActionResult GetTopDoctors(
        [FromQuery] string range = "30d",
        [FromQuery] int top = 5)
    {
        return Ok(new { data = new { series = Array.Empty<object>() } });
    }

    /// <summary>Top thuoc (stub)</summary>
    [HttpGet("charts/top-drugs")]
    [RequirePermission("dashboard.read")]
    public IActionResult GetTopDrugs(
        [FromQuery] string range = "30d",
        [FromQuery] int top = 5)
    {
        return Ok(new { data = new { series = Array.Empty<object>() } });
    }

    /// <summary>Phan phoi HbA1c dai thao duong</summary>
    [HttpGet("charts/diabetes-hba1c")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetDiabetesHba1c(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from = today.AddDays(-90);
        var result = await _mediator.Send(new GetDiabetesCohortQuery(from, today), ct);
        var series = result.Buckets.Select(b => new ChartDataPoint(b.Label, b.PatientCount)).ToList();
        return Ok(new { data = new { series } });
    }

    /// <summary>Danh sach canh bao hoat dong</summary>
    [HttpGet("alerts")]
    [RequirePermission("dashboard.read")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? severity,
        [FromQuery] string? type,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAlertsQuery(severity, type), ct);
        return Ok(new { data = result });
    }
}
