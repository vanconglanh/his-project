using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Packages;

namespace ProDiabHis.Api.Controllers;

/// <summary>FR-1201 - quan tri template "Goi dinh muc tra truoc" (khac voi Gói giá dịch vụ hiện có).</summary>
[ApiController]
[Route("api/v1/packages")]
[Authorize]
public class PackagesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PackagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission("package.read")]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] bool? is_active,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPackagesQuery(q, is_active, page, Math.Min(page_size, 100)), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("package.read")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPackageQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpPost]
    [RequirePermission("package.create")]
    public async Task<IActionResult> Create([FromBody] PackageUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePackageCommand(request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage);
        return StatusCode(201, new { data = result.Value });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("package.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PackageUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePackageCommand(id, request), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage);
        return Ok(new { data = result.Value });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("package.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePackageCommand(id), ct);
        if (!result.IsSuccess) return MapError(result.ErrorCode, result.ErrorMessage);
        return NoContent();
    }

    private IActionResult MapError(string? code, string? message) => code switch
    {
        "PACKAGE_NOT_FOUND" => NotFound(new { error = new { code, message } }),
        "PACKAGE_CODE_DUPLICATE" or "PACKAGE_IN_USE" => Conflict(new { error = new { code, message } }),
        "PACKAGE_ENTITLEMENT_REQUIRED" or "PACKAGE_ENTITLEMENT_TYPE_INVALID" or
        "PACKAGE_ENTITLEMENT_DUPLICATE_ITEM" or "PACKAGE_ITEM_NOT_FOUND" or "PACKAGE_DURATION_INVALID"
            => UnprocessableEntity(new { error = new { code, message } }),
        _ => Problem(message, statusCode: 400)
    };
}
