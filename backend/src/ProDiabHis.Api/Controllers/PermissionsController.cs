using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Roles;

namespace ProDiabHis.Api.Controllers;

/// <summary>Danh muc quyen he thong</summary>
[ApiController]
[Route("api/v1/permissions")]
[Authorize]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Liet ke toan bo permission co san</summary>
    [HttpGet]
    [RequirePermission("role.read")]
    public async Task<IActionResult> ListPermissions(
        [FromQuery] string? resource = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListPermissionsQuery(resource), ct);
        return Ok(new { data = result.Value });
    }
}
