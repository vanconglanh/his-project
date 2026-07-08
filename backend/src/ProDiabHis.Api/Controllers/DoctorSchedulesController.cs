using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Application.Scheduling;

namespace ProDiabHis.Api.Controllers;

// Quan ly lich lam viec bac si (dung cho Patient Portal sinh slot dat lich).
[ApiController]
[Authorize]
[Route("api/v1/doctor-schedules")]
public class DoctorSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenant;

    public DoctorSchedulesController(IMediator mediator, ITenantProvider tenant)
    {
        _mediator = mediator;
        _tenant = tenant;
    }

    [HttpGet]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> List([FromQuery] Guid? doctor_ref, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDoctorSchedulesQuery(_tenant.TenantId, doctor_ref), ct);
        return Ok(new { data = result });
    }

    [HttpPost]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> Create([FromBody] DoctorScheduleUpsertRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDoctorScheduleCommand(_tenant.TenantId, body), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:int}")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> Update(int id, [FromBody] DoctorScheduleUpsertRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDoctorScheduleCommand(id, _tenant.TenantId, body), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteDoctorScheduleCommand(id, _tenant.TenantId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // -------- Block nghi/khoa gio --------
    [HttpGet("blocks")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> ListBlocks([FromQuery] Guid? doctor_ref, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListScheduleBlocksQuery(_tenant.TenantId, doctor_ref), ct);
        return Ok(new { data = result });
    }

    [HttpPost("blocks")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> CreateBlock([FromBody] ScheduleBlockCreateRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateScheduleBlockCommand(_tenant.TenantId, body), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpDelete("blocks/{id:int}")]
    [RequirePermission("appointment.write")]
    public async Task<IActionResult> DeleteBlock(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteScheduleBlockCommand(id, _tenant.TenantId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}
