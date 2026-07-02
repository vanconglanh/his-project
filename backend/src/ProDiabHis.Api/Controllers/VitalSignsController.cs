using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.VitalSigns;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Authorize]
public class VitalSignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VitalSignsController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/encounters/{encounterId}/vital-signs
    [HttpPost("api/v1/encounters/{encounterId:guid}/vital-signs")]
    [RequirePermission("vital_sign.write")]
    public async Task<IActionResult> Create(Guid encounterId, [FromBody] VitalSignsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateVitalSignsCommand(encounterId, request), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "VITAL_INVALID_RANGE" ? 422 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/vital-signs
    [HttpGet("api/v1/encounters/{encounterId:guid}/vital-signs")]
    [RequirePermission("vital_sign.read")]
    public async Task<IActionResult> List(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListVitalSignsByEncounterQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/encounters/{encounterId}/vital-signs/latest
    [HttpGet("api/v1/encounters/{encounterId:guid}/vital-signs/latest")]
    [RequirePermission("vital_sign.read")]
    public async Task<IActionResult> Latest(Guid encounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLatestVitalSignsQuery(encounterId), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/encounters/{encounterId}/vital-signs/batch
    [HttpPost("api/v1/encounters/{encounterId:guid}/vital-signs/batch")]
    [RequirePermission("vital_sign.write")]
    public async Task<IActionResult> Batch(Guid encounterId, [FromBody] BatchVitalBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new BatchCreateVitalSignsCommand(encounterId, body.Records), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    // PUT /api/v1/vital-signs/{id}
    [HttpPut("api/v1/vital-signs/{id:guid}")]
    [RequirePermission("vital_sign.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] VitalSignsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateVitalSignsCommand(id, request), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "VITAL_EDIT_TIMEOUT" ? 403 : 422;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/vital-signs/{id}
    [HttpDelete("api/v1/vital-signs/{id:guid}")]
    [RequirePermission("vital_sign.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteVitalSignsCommand(id), ct);
        if (!result.IsSuccess)
        {
            var code = result.ErrorCode == "VITAL_EDIT_TIMEOUT" ? 403 : 404;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }

    // GET /api/v1/patients/{patientId}/vital-signs/history
    [HttpGet("api/v1/patients/{patientId:guid}/vital-signs/history")]
    [RequirePermission("vital_sign.read")]
    public async Task<IActionResult> History(
        Guid patientId,
        [FromQuery] DateOnly? date_from,
        [FromQuery] DateOnly? date_to,
        [FromQuery] string? metric,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVitalSignsHistoryQuery(patientId, date_from, date_to, metric), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/patients/{patientId}/vital-signs/trend
    // Thống kê xu hướng (min/max/avg/latest/first + chuỗi thời gian) cho MỘT chỉ số trong khoảng ngày.
    // Khác /history (trả danh sách bản ghi): endpoint này trả số liệu tổng hợp đã tính sẵn.
    [HttpGet("api/v1/patients/{patientId:guid}/vital-signs/trend")]
    [RequirePermission("vital_sign.read")]
    public async Task<IActionResult> Trend(
        Guid patientId,
        [FromQuery] string metric,
        [FromQuery] DateOnly? date_from,
        [FromQuery] DateOnly? date_to,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVitalSignsTrendQuery(patientId, metric, date_from, date_to), ct);
        if (!result.IsSuccess)
        {
            // metric ngoài whitelist = lỗi nghiệp vụ -> 422 (giống VITAL_INVALID_RANGE ở Create); còn lại -> 400.
            var code = result.ErrorCode == "VITAL_TREND_INVALID_METRIC" ? 422 : 400;
            return StatusCode(code, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }
}

public record BatchVitalBody(IReadOnlyList<VitalSignsRequest> Records);
