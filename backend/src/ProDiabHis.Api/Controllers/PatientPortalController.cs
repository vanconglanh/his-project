using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.PublicApi;
using System.IdentityModel.Tokens.Jwt;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/portal/v1")]
public class PatientPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortalAuthService _portalAuth;

    public PatientPortalController(IMediator mediator, IPortalAuthService portalAuth)
    {
        _mediator = mediator;
        _portalAuth = portalAuth;
    }

    private Guid PatientId => Guid.Parse(User.FindFirst("patient_id")!.Value);
    private int TenantId => int.Parse(User.FindFirst("tenant_id")!.Value);

    // -------- Auth --------
    [HttpPost("auth/request-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestOtp(
        [FromBody] PortalAuthOtpRequest request,
        CancellationToken cancellationToken)
    {
        // Determine tenant from TenantCode header or query
        int tenantId = 0;
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tidHeader)
            && int.TryParse(tidHeader, out var tid))
            tenantId = tid;

        try
        {
            await _mediator.Send(new PortalRequestOtpCommand(request.Phone, tenantId), cancellationToken);
            return Accepted();
        }
        catch (PortalPhoneNotRegisteredException)
        {
            return NotFound(new { error = new { code = "PORTAL_PHONE_NOT_REGISTERED", message = "So dien thoai chua dang ky" } });
        }
        catch (OtpTooManyAttemptsException)
        {
            return StatusCode(429, new { error = new { code = "PORTAL_OTP_TOO_MANY_ATTEMPTS", message = "Qua nhieu yeu cau OTP, thu lai sau 1 gio" } });
        }
    }

    [HttpPost("auth/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] PortalVerifyRequest request,
        CancellationToken cancellationToken)
    {
        int tenantId = 0;
        if (Request.Headers.TryGetValue("X-Tenant-Id", out var tidHeader)
            && int.TryParse(tidHeader, out var tid))
            tenantId = tid;

        try
        {
            var result = await _mediator.Send(
                new PortalVerifyOtpCommand(request.Phone, request.Otp, tenantId), cancellationToken);
            return Ok(new PortalAuthResponse(
                result.AccessToken, "Bearer", result.ExpiresIn, result.PatientCode, result.FullName));
        }
        catch (OtpInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_OTP_INVALID", message = "OTP khong dung" } });
        }
        catch (OtpExpiredException)
        {
            return StatusCode(410, new { error = new { code = "PORTAL_OTP_EXPIRED", message = "OTP da het han" } });
        }
        catch (OtpTooManyAttemptsException)
        {
            return StatusCode(429, new { error = new { code = "PORTAL_OTP_TOO_MANY_ATTEMPTS", message = "Qua nhieu lan thu sai" } });
        }
        catch (PortalPhoneNotRegisteredException)
        {
            return NotFound(new { error = new { code = "PORTAL_PHONE_NOT_REGISTERED", message = "So dien thoai chua dang ky" } });
        }
    }

    [HttpPost("auth/logout")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "";
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var expiresAt = expClaim != null
            ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
            : DateTime.UtcNow.AddHours(24);

        await _mediator.Send(new PortalLogoutCommand(jti, expiresAt), cancellationToken);
        return NoContent();
    }

    // -------- Profile --------
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPortalMeQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    // -------- Encounters --------
    [HttpGet("me/encounters")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetEncounters(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _mediator.Send(
            new GetPortalEncountersQuery(PatientId, TenantId, page, page_size), cancellationToken);
        return Ok(new { data = items, meta = new { page, total } });
    }

    [HttpGet("me/encounters/{id:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetEncounterDetail(Guid id, CancellationToken cancellationToken)
    {
        // Delegate to GetPortalEncountersQuery filtered — stub for now
        return Ok(new { data = new { id } });
    }

    // -------- Prescriptions --------
    [HttpGet("me/prescriptions")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public IActionResult GetPrescriptions()
        => Ok(new { data = Array.Empty<object>() });

    [HttpGet("me/prescriptions/{id:guid}/pdf")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public IActionResult GetPrescriptionPdf(Guid id)
        => File(Array.Empty<byte>(), "application/pdf", $"prescription_{id}.pdf");

    // -------- Lab Results --------
    [HttpGet("me/lab-results")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public IActionResult GetLabResults()
        => Ok(new { data = Array.Empty<object>() });

    [HttpGet("me/lab-results/{id:guid}/pdf")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public IActionResult GetLabResultPdf(Guid id)
        => File(Array.Empty<byte>(), "application/pdf", $"lab_{id}.pdf");

    // -------- Appointments --------
    [HttpGet("me/appointments")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> GetAppointments(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPortalAppointmentsQuery(PatientId, TenantId), cancellationToken);
        return Ok(new { data = result });
    }

    [HttpPost("me/appointments")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] PortalAppointmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new CreatePortalAppointmentCommand(PatientId, TenantId, request), cancellationToken);
            return StatusCode(201, new { data = result });
        }
        catch (SlotTakenException)
        {
            return Conflict(new { error = new { code = "APPOINTMENT_SLOT_TAKEN", message = "Khung gio da duoc dat" } });
        }
    }

    [HttpDelete("me/appointments/{id:guid}")]
    [Authorize(AuthenticationSchemes = "PortalBearer")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new CancelPortalAppointmentCommand(id, PatientId, TenantId), cancellationToken);
            return NoContent();
        }
        catch (AppointmentCancelTooLateException)
        {
            return BadRequest(new { error = new { code = "APPOINTMENT_CANCEL_TOO_LATE", message = "Khong the huy trong vong 2 gio truoc hen" } });
        }
        catch (AppointmentNotFoundException)
        {
            return NotFound(new { error = new { code = "APPOINTMENT_NOT_FOUND", message = "Khong tim thay lich hen" } });
        }
    }
}
