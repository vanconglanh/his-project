using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Appointments;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/appointments/{id}/slip-pdf
    [HttpGet("api/v1/appointments/{id:int}/slip-pdf")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> SlipPdf(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAppointmentSlipPdfQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return File(result.Value!, "application/pdf", $"giay-hen-tai-kham-{id}.pdf");
    }

    // GET /api/v1/appointments
    [HttpGet("api/v1/appointments")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? doctor_ref,
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListAppointmentsQuery(from, to, doctor_ref, status, q, page, page_size), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total }
        });
    }

    // GET /api/v1/appointments/{id}
    [HttpGet("api/v1/appointments/{id:int}")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAppointmentQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/appointments
    [HttpPost("api/v1/appointments")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode is "PATIENT_NOT_FOUND" or "DOCTOR_NOT_FOUND" ? 404 : 422;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/appointments/{id}
    [HttpPut("api/v1/appointments/{id:int}")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAppointmentCommand(id, body), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode switch
            {
                "APPOINTMENT_NOT_FOUND" => 404,
                "PATIENT_NOT_FOUND" or "DOCTOR_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // PATCH /api/v1/appointments/{id}/status
    [HttpPatch("api/v1/appointments/{id:int}/status")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAppointmentStatusCommand(id, body.Status), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "APPOINTMENT_NOT_FOUND" ? 404 : 422;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/appointments/options/doctors
    [HttpGet("api/v1/appointments/options/doctors")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> OptionsDoctors(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDoctorOptionsQuery(), ct);
        return Ok(new { data = result });
    }

    // GET /api/v1/appointments/options/patients
    [HttpGet("api/v1/appointments/options/patients")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> OptionsPatients([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListPatientOptionsQuery(q), ct);
        return Ok(new { data = result });
    }
}
