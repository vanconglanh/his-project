using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Api.Controllers;

/// <summary>Public API cho doi tac B2B — auth bang X-Api-Key</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/v1")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class PublicApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicApiController(IMediator mediator) => _mediator = mediator;

    private ApiPartnerContext GetPartner() =>
        (ApiPartnerContext)HttpContext.Items["ApiPartner"]!;

    private int GetTenantId() => (int)HttpContext.Items["TenantId"]!;

    // -------- Patients --------
    [HttpPost("patients/register")]
    [ApiKeyAuth(Scope = "public.patient.write")]
    [ProducesResponseType(typeof(PublicPatientResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterPatient(
        [FromBody] PublicRegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var partner = GetPartner();
        var result = await _mediator.Send(
            new RegisterPatientCommand(request, partner.PartnerId, GetTenantId()),
            cancellationToken);
        return StatusCode(201, new { data = result });
    }

    // -------- Appointments --------
    [HttpPost("appointments/book")]
    [ApiKeyAuth(Scope = "public.appointment.write")]
    [ProducesResponseType(typeof(PublicAppointmentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookAppointment(
        [FromBody] PublicAppointmentBookRequest request,
        CancellationToken cancellationToken)
    {
        var partner = GetPartner();
        try
        {
            var result = await _mediator.Send(
                new BookAppointmentCommand(request, partner.PartnerId, GetTenantId()),
                cancellationToken);
            return StatusCode(201, new { data = result });
        }
        catch (SlotTakenException)
        {
            return Conflict(new { error = new { code = "APPOINTMENT_SLOT_TAKEN", message = "Khung gio da duoc dat" } });
        }
    }

    [HttpGet("appointments/{id:guid}")]
    [ApiKeyAuth(Scope = "public.appointment.read")]
    public async Task<IActionResult> GetAppointment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicAppointmentQuery(id, GetTenantId()), cancellationToken);
        if (result == null)
            return NotFound(new { error = new { code = "APPOINTMENT_NOT_FOUND", message = "Khong tim thay lich hen" } });
        return Ok(new { data = result });
    }

    // -------- Catalog --------
    [HttpGet("catalog/service-packages")]
    [ApiKeyAuth(Scope = "public.catalog.read")]
    public IActionResult GetServicePackages()
        => Ok(new { data = Array.Empty<object>(), meta = new { message = "Coming soon" } });

    [HttpGet("catalog/services")]
    [ApiKeyAuth(Scope = "public.catalog.read")]
    public IActionResult GetServices([FromQuery] string? q, [FromQuery] Guid? department_id)
        => Ok(new { data = Array.Empty<object>(), meta = new { message = "Coming soon" } });

    [HttpGet("catalog/doctors")]
    [ApiKeyAuth(Scope = "public.catalog.read")]
    public IActionResult GetDoctors()
        => Ok(new { data = Array.Empty<object>(), meta = new { message = "Coming soon" } });

    // -------- Visit Lookup --------
    [HttpPost("visits/{patientCode}/request-otp")]
    [ApiKeyAuth(Scope = "public.visit.lookup")]
    public async Task<IActionResult> RequestVisitOtp(string patientCode, CancellationToken cancellationToken)
    {
        var partner = GetPartner();
        try
        {
            await _mediator.Send(new RequestVisitOtpCommand(patientCode, GetTenantId(), partner.PartnerId), cancellationToken);
            return Accepted();
        }
        catch (PatientNotFoundException)
        {
            return NotFound(new { error = new { code = "PORTAL_PHONE_NOT_REGISTERED", message = "Ma benh nhan khong ton tai" } });
        }
    }

    [HttpPost("visits/{patientCode}/verify-otp")]
    [ApiKeyAuth(Scope = "public.visit.lookup")]
    public async Task<IActionResult> VerifyVisitOtp(
        string patientCode,
        [FromBody] VerifyOtpBody body,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new VerifyVisitOtpCommand(patientCode, body.Otp, GetTenantId()), cancellationToken);
            return Ok(new { lookup_token = result.LookupToken, expires_in = result.ExpiresIn });
        }
        catch (OtpInvalidException)
        {
            return BadRequest(new { error = new { code = "PORTAL_OTP_INVALID", message = "Ma OTP khong dung" } });
        }
        catch (OtpExpiredException)
        {
            return StatusCode(410, new { error = new { code = "PORTAL_OTP_EXPIRED", message = "OTP da het han" } });
        }
        catch (OtpTooManyAttemptsException)
        {
            return StatusCode(429, new { error = new { code = "PORTAL_OTP_TOO_MANY_ATTEMPTS", message = "Qua nhieu lan thu" } });
        }
    }

    [HttpGet("visits/{patientCode}/lookup")]
    [ApiKeyAuth(Scope = "public.visit.lookup")]
    public IActionResult GetVisits(string patientCode)
        => Ok(new { data = Array.Empty<object>(), message = "Use lookup_token as Bearer" });
}

public record VerifyOtpBody(string Otp);
