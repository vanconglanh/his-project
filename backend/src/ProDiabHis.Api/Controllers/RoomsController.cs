using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Rooms;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan ly phong kham CRUD (Admin)</summary>
[ApiController]
[Route("api/v1/rooms")]
[Authorize]
[Produces("application/json")]
public class RoomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach phong kham cua tenant</summary>
    [HttpGet]
    [RequirePermission("room.read")]
    public async Task<IActionResult> ListRooms(
        [FromQuery] bool? is_active,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListRoomsAdminQuery(is_active, page, page_size), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    /// <summary>Chi tiet phong kham</summary>
    [HttpGet("{id}")]
    [RequirePermission("room.read")]
    public async Task<IActionResult> GetRoom(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRoomQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Tao phong kham moi (Admin)</summary>
    [HttpPost]
    [RequirePermission("room.write")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateRoomCommand(request), ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Cap nhat phong kham (Admin)</summary>
    [HttpPut("{id}")]
    [RequirePermission("room.write")]
    public async Task<IActionResult> UpdateRoom(string id, [FromBody] UpdateRoomRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateRoomCommand(id, request), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "ROOM_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Xoa mem phong kham (Admin)</summary>
    [HttpDelete("{id}")]
    [RequirePermission("room.delete")]
    public async Task<IActionResult> DeleteRoom(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteRoomCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}
