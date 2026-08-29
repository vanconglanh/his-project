using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Packages;

namespace ProDiabHis.Api.Controllers;

/// <summary>FR-1202/1203/1204/1205/1206 - ban/thu tien/theo doi dinh muc goi cua benh nhan.</summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class PackageSubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PackageSubscriptionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("package-subscriptions")]
    [RequirePermission("package_subscription.read")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? patient_id, [FromQuery] string? status, [FromQuery] string? payment_status,
        [FromQuery] bool? has_debt, [FromQuery] int? expiring_within_days, [FromQuery] int? branch_id,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListSubscriptionsQuery(
            patient_id, status, payment_status, has_debt, expiring_within_days, branch_id, page, Math.Min(page_size, 100)), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpGet("package-subscriptions/{id:guid}")]
    [RequirePermission("package_subscription.read")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubscriptionQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpPost("package-subscriptions")]
    [RequirePermission("package_subscription.sell")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSubscriptionCommand(request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPost("package-subscriptions/{id:guid}/payments")]
    [RequirePermission("package_subscription.collect")]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddPaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddSubscriptionPaymentCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);
        return Ok(new { data = result.Value });
    }

    [HttpPost("package-subscriptions/{id:guid}/cancel")]
    [RequirePermission("package_subscription.cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelSubscriptionCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);
        return Ok(new { data = result.Value });
    }

    /// <summary>H-14 (FR-1211): Gia han goi da het han nhung con dinh muc (them X ngay cau hinh).</summary>
    [HttpPost("package-subscriptions/{id:guid}/extend")]
    [RequirePermission("package_subscription.extend")]
    public async Task<IActionResult> Extend(Guid id, [FromBody] ExtendSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExtendSubscriptionCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/patients/{patientId}/package-summary - FR-1205
    [HttpGet("patients/{patientId:guid}/package-summary")]
    [RequirePermission("package_subscription.read")]
    public async Task<IActionResult> GetPatientSummary(Guid patientId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPatientPackageSummaryQuery(patientId), ct);
        return Ok(new { data = result.Value });
    }

    private IActionResult MapError(string? code, string? message, object? details) => code switch
    {
        "PACKAGE_NOT_FOUND" or "PATIENT_NOT_FOUND" or "PACKAGE_SUBSCRIPTION_NOT_FOUND"
            => NotFound(new { error = new { code, message, details } }),
        "PACKAGE_SUBSCRIPTION_ALREADY_CLOSED" => Conflict(new { error = new { code, message, details } }),
        "PACKAGE_DEPOSIT_BELOW_MINIMUM" or "PACKAGE_PAYMENT_EXCEEDS_TOTAL" or "PACKAGE_NOT_SELLABLE"
            or "PACKAGE_PAYMENT_INVALID_AMOUNT" or "PACKAGE_PAYMENT_EXCEEDS_DUE"
            => UnprocessableEntity(new { error = new { code, message, details } }),
        _ => Problem(message, statusCode: 400)
    };
}
