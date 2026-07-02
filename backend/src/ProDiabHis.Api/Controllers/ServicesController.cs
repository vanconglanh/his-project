using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/services
    [HttpGet]
    [RequirePermission("service.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] bool? is_active,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListServicesQuery(q, category, is_active, page, Math.Min(page_size, 200)), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    // POST /api/v1/services
    [HttpPost]
    [RequirePermission("service.write")]
    public async Task<IActionResult> Create([FromBody] ServiceUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateServiceCommand(request), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "SERVICE_CODE_EXISTS")
                return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return Problem(result.ErrorMessage, statusCode: 400);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, new { data = result.Value });
    }

    // GET /api/v1/services/search
    [HttpGet("search")]
    [RequirePermission("service.read")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { error = new { code = "Q_REQUIRED", message = "q is required" } });
        var result = await _mediator.Send(new SearchServicesQuery(q), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/services/categories
    [HttpGet("categories")]
    public IActionResult Categories()
    {
        var cats = new[] { "CONSULTATION", "PROCEDURE", "LAB", "RAD", "PHARMACY", "OTHER" };
        return Ok(new { data = cats });
    }

    // POST /api/v1/services/import
    [HttpPost("import")]
    [RequirePermission("service.write")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "FILE_REQUIRED", message = "File Excel la bat buoc" } });
        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportServicesFromExcelCommand(stream), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/services/{id}
    [HttpGet("{id:guid}")]
    [RequirePermission("service.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetServiceQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/services/{id}
    [HttpPut("{id:guid}")]
    [RequirePermission("service.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateServiceCommand(id, request), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/services/{id}
    [HttpDelete("{id:guid}")]
    [RequirePermission("service.write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteServiceCommand(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/service-packages")]
[Authorize]
public class ServicePackagesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicePackagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [RequirePermission("service_package.read")]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] bool? is_active, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListServicePackagesQuery(q, is_active), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total, total_pages = paged.TotalPages } });
    }

    [HttpPost]
    [RequirePermission("service_package.write")]
    public async Task<IActionResult> Create([FromBody] ServicePackageUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateServicePackageCommand(request), ct);
        if (!result.IsSuccess) return Problem(result.ErrorMessage, statusCode: 400);
        return StatusCode(201, new { data = result.Value });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("service_package.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetServicePackageQuery(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("service_package.write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServicePackageUpsertRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateServicePackageCommand(id, request), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("service_package.write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteServicePackageCommand(id), ct);
        if (!result.IsSuccess) return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}
