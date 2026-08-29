using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Telehealth;

namespace ProDiabHis.Api.Controllers;

/// <summary>Admin CRUD mapping dich vu telehealth HIS &lt;-&gt; Docosan (khong co UI, chi API).</summary>
[ApiController]
[Authorize]
public class TelehealthAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public TelehealthAdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/v1/telehealth/service-mappings")]
    [RequirePermission("telehealth.admin_mapping")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListServiceMappingsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost("api/v1/telehealth/service-mappings")]
    [RequirePermission("telehealth.admin_mapping")]
    public async Task<IActionResult> Create([FromBody] ServiceMappingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateServiceMappingCommand(request), ct);
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode == "TELEHEALTH_SERVICE_MAPPING_DUPLICATE" ? 409 : 400,
                new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPut("api/v1/telehealth/service-mappings/{id:guid}")]
    [RequirePermission("telehealth.admin_mapping")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceMappingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateServiceMappingCommand(id, request), ct);
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode == "TELEHEALTH_SERVICE_MAPPING_NOT_FOUND" ? 404 : 400,
                new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // ── FR-804: Admin CRUD danh muc ICD-10 duoc phep tu van tu xa ──

    [HttpGet("api/v1/telehealth/allowed-icd10")]
    [RequirePermission("telehealth.icd10_read")]
    public async Task<IActionResult> ListAllowedIcd10(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListAllowedIcd10Query(), ct);
        return Ok(new { data = result.Value });
    }

    [HttpPost("api/v1/telehealth/allowed-icd10")]
    [RequirePermission("telehealth.icd10_manage")]
    public async Task<IActionResult> CreateAllowedIcd10([FromBody] AllowedIcd10Request request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAllowedIcd10Command(request), ct);
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode == "TELEHEALTH_ICD10_DUPLICATE" ? 409 : 400,
                new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPut("api/v1/telehealth/allowed-icd10/{id:guid}")]
    [RequirePermission("telehealth.icd10_manage")]
    public async Task<IActionResult> UpdateAllowedIcd10(Guid id, [FromBody] AllowedIcd10Request request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAllowedIcd10Command(id, request), ct);
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode == "TELEHEALTH_ICD10_NOT_FOUND" ? 404 : 400,
                new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpDelete("api/v1/telehealth/allowed-icd10/{id:guid}")]
    [RequirePermission("telehealth.icd10_manage")]
    public async Task<IActionResult> DeleteAllowedIcd10(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAllowedIcd10Command(id), ct);
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode == "TELEHEALTH_ICD10_NOT_FOUND" ? 404 : 400,
                new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}
