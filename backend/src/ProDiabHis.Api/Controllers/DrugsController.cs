using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Pharmacy.Drugs;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/drugs")]
[Authorize]
public class DrugsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DrugsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/drugs  — hỗ trợ cả ?q= và ?search= để tương thích E2E test
    [HttpGet]
    [RequirePermission("drug.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] bool? requires_prescription,
        [FromQuery] string? atc_code,
        [FromQuery] int? category_id,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var searchTerm = q ?? search;
        var result = await _mediator.Send(new ListDrugsQuery(searchTerm, status, requires_prescription, atc_code, category_id, page, Math.Min(page_size, 100)), ct);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    // POST /api/v1/drugs/import  (must be before /{id} to avoid route conflict)
    [HttpPost("import")]
    [RequirePermission("drug.import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file, [FromForm] string mode = "UPSERT", CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "DRUG_IMPORT_INVALID_FORMAT", message = "Vui long upload file Excel." } });

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportDrugsCommand(stream, mode.ToUpper()), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new { data = result.Value });
    }

    // GET /api/v1/drugs/search
    [HttpGet("search")]
    [RequirePermission("drug.read")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { code = "VALIDATION", message = "q la bat buoc." } });

        var result = await _mediator.Send(new SearchDrugsQuery(q, limit), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/drugs/categories
    [HttpGet("categories")]
    [RequirePermission("drug.read")]
    public async Task<IActionResult> ListCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDrugCategoriesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/drugs/categories
    [HttpPost("categories")]
    [RequirePermission("drug.write")]
    public async Task<IActionResult> CreateCategory([FromBody] DrugCategoryCreateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDrugCategoryCommand(request), ct);
        return Created("", new { data = result.Value });
    }

    // POST /api/v1/drugs/sync-cuc-qld
    [HttpPost("sync-cuc-qld")]
    [RequirePermission("drug.sync")]
    public async Task<IActionResult> SyncCucQld([FromBody] SyncCucQldRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SyncCucQldCommand(request?.Mode ?? "INCREMENTAL", request?.Since), ct);
        return Accepted(new { data = result.Value });
    }

    // GET /api/v1/drugs/{id}
    [HttpGet("{id}")]
    [RequirePermission("drug.read")]
    public async Task<IActionResult> GetDetail(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDrugQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/drugs/{id}
    [HttpPut("{id}")]
    [RequirePermission("drug.write")]
    public async Task<IActionResult> Update(string id, [FromBody] DrugMasterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDrugCommand(id, request), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // DELETE /api/v1/drugs/{id}
    [HttpDelete("{id}")]
    [RequirePermission("drug.write")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteDrugCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    // POST /api/v1/drugs (Create)
    [HttpPost]
    [RequirePermission("drug.write")]
    public async Task<IActionResult> Create([FromBody] DrugMasterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDrugCommand(request), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Created("", new { data = result.Value });
    }

    // GET /api/v1/drugs/{id}/equivalents
    [HttpGet("{id}/equivalents")]
    [RequirePermission("drug.read")]
    public async Task<IActionResult> GetEquivalents(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEquivalentDrugsQuery(id), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/drugs/{id}/interactions
    [HttpGet("{id}/interactions")]
    [RequirePermission("ddi.check")]
    public async Task<IActionResult> GetInteractions(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDrugInteractionsQuery(id), ct);
        return Ok(new { data = result.Value });
    }
}
