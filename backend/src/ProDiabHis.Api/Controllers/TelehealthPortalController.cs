using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Telehealth;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// API Portal cho benh nhan tu dat lich tu van tu xa qua Docosan (FR-801/802).
/// Xac thuc: JWT Portal voi claim "patient_id"/"tenant_id" (giong PatientPortalController).
/// </summary>
[ApiController]
[Route("api/v1/portal/telehealth")]
[Authorize]
public class TelehealthPortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelehealthPortalController(IMediator mediator) => _mediator = mediator;

    private Guid PatientId => Guid.Parse(User.FindFirst("patient_id")!.Value);

    [HttpGet("eligibility")]
    public async Task<IActionResult> CheckEligibility(CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckTelehealthEligibilityQuery(PatientId), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost("link-docosan-account")]
    public async Task<IActionResult> LinkAccount([FromBody] LinkDocosanAccountRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LinkDocosanAccountCommand(PatientId, request), ct);
        if (!result.IsSuccess)
            return StatusCode(MapErrorStatus(result.ErrorCode!), Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    [HttpPost("appointments")]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateTelehealthAppointmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateTelehealthAppointmentCommand(PatientId, request), ct);
        if (!result.IsSuccess)
            return StatusCode(MapErrorStatus(result.ErrorCode!), Error(result.ErrorCode!, result.ErrorMessage!, result.ErrorDetails));
        return StatusCode(201, new { data = result.Value });
    }

    [HttpGet("appointments/{id:guid}")]
    public async Task<IActionResult> GetAppointment(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTelehealthSessionQuery(PatientId, id), ct);
        if (!result.IsSuccess)
            return NotFound(Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    [HttpGet("appointments/{id:guid}/join-link")]
    public async Task<IActionResult> GetJoinLink(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTelehealthJoinLinkQuery(PatientId, id), ct);
        if (!result.IsSuccess)
            return StatusCode(MapErrorStatus(result.ErrorCode!), Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    private static int MapErrorStatus(string code) => code switch
    {
        "PATIENT_NOT_FOUND" or "TELEHEALTH_SESSION_NOT_FOUND" => 404,
        "TELEHEALTH_NOT_ELIGIBLE" or "TELEHEALTH_DOCTOR_NOT_MAPPED" or "TELEHEALTH_CLINIC_NOT_MAPPED"
            or "TELEHEALTH_SERVICE_NOT_CONFIGURED" or "TELEHEALTH_ACCOUNT_NOT_LINKED"
            or "TELEHEALTH_JOIN_LINK_EXPIRED" or "TELEHEALTH_PAYMENT_PENDING" => 409,
        "TELEHEALTH_PROVIDER_UNAVAILABLE" => 502,
        _ => 400
    };

    private static object Error(string code, string message, object? details = null) =>
        new { error = new { code, message, details } };
}
