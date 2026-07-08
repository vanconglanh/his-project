using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Codes;

namespace ProDiabHis.Api.Controllers;

// Danh muc ma dung chung (GLOBAL, khong loc tenant). Moi user dang nhap deu doc duoc.
[ApiController]
[Route("api/v1/codes")]
[Authorize]
public class CodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CodesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/codes  — danh sach nhom ma [{ id, name }]
    [HttpGet]
    public async Task<IActionResult> Groups(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCodeGroupsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/codes/batch?ids=GENDER,BLOOD_TYPE  — map { groupId: [{ code, name }] }
    // (phai dat truoc /{groupId} de khong bi nuot route)
    [HttpGet("batch")]
    public async Task<IActionResult> Batch([FromQuery] string? ids, CancellationToken ct)
    {
        var groupIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var result = await _mediator.Send(new GetCodeBatchQuery(groupIds), ct);
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/codes/{groupId}  — danh sach ma trong nhom [{ code, name }]
    [HttpGet("{groupId}")]
    public async Task<IActionResult> Items(string groupId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCodeItemsQuery(groupId), ct);
        return Ok(new { data = result.Value });
    }
}
