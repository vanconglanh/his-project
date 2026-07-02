using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.ICD10;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/icd10")]
[Authorize]
public class Icd10Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public Icd10Controller(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/icd10  — endpoint gốc hỗ trợ ?search= hoặc ?q= (tương thích E2E)
    [HttpGet]
    [RequirePermission("icd10.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] bool billable_only = false,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var term = search ?? q ?? "";
        var result = await _mediator.Send(new SearchIcd10Query(term, type, category, billable_only, Math.Min(limit, 100)), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/icd10/search
    [HttpGet("search")]
    [RequirePermission("icd10.read")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] bool billable_only = false,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Tham số q là bắt buộc" } });

        var result = await _mediator.Send(new SearchIcd10Query(q, type, category, billable_only, Math.Min(limit, 100)), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/icd10/categories  (must come before /{code})
    [HttpGet("categories")]
    [RequirePermission("icd10.read")]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIcd10CategoriesQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/icd10/{code}
    [HttpGet("{code}")]
    [RequirePermission("icd10.read")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIcd10ByCodeQuery(code), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = "ICD10_NOT_FOUND", message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }
}
