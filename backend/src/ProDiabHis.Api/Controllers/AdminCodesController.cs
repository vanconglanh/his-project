using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Codes;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan tri danh muc ma (code master/detail) — tao/sua/an/xoa ma rieng tenant,
/// ke thua-override tren nen ma chuan he thong (N1, audit-hardcode-vs-master-data).</summary>
[ApiController]
[Route("api/v1/admin/codes")]
[Authorize]
[Produces("application/json")]
public class AdminCodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCodesController(IMediator mediator) => _mediator = mediator;

    public record CreateCodeDetailRequest(string Code, string Name, string? NameEn, int? SortOrder, string? Extra);
    public record UpdateCodeDetailRequest(string Name, string? NameEn, int? SortOrder, bool? IsActive, string? Extra);
    public record SetVisibilityRequest(bool IsHidden);

    /// <summary>Danh sach nhom ma</summary>
    [HttpGet]
    [RequirePermission("code.read")]
    public async Task<IActionResult> Groups(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminCodeGroupsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    /// <summary>Danh sach chi tiet ma trong 1 nhom, da resolve theo tenant (bao gom override)</summary>
    [HttpGet("{groupId}/details")]
    [RequirePermission("code.read")]
    public async Task<IActionResult> Details(string groupId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminCodeDetailsQuery(groupId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Tao ma rieng cua tenant trong nhom</summary>
    [HttpPost("{groupId}/details")]
    [RequirePermission("code.manage")]
    public async Task<IActionResult> Create(string groupId, [FromBody] CreateCodeDetailRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCodeDetailCommand(
            groupId, request.Code, request.Name, request.NameEn, request.SortOrder, request.Extra), ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CODE_GROUP_NOT_FOUND")
                return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }

        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Sua ma (neu la ma he thong -> tu dong tao/ghi de ban override rieng cua tenant)</summary>
    [HttpPut("{groupId}/details/{id}")]
    [RequirePermission("code.manage")]
    public async Task<IActionResult> Update(string groupId, string id, [FromBody] UpdateCodeDetailRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCodeDetailCommand(
            groupId, id, request.Name, request.NameEn, request.SortOrder, request.IsActive, request.Extra), ct);

        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new { data = result.Value });
    }

    /// <summary>An/hien 1 ma chuan he thong rieng cho tenant hien tai (khong doi du lieu global)</summary>
    [HttpPatch("{groupId}/details/{code}/visibility")]
    [RequirePermission("code.manage")]
    public async Task<IActionResult> SetVisibility(string groupId, string code, [FromBody] SetVisibilityRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetCodeVisibilityCommand(groupId, code, request.IsHidden), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Xoa ma rieng cua tenant (khong xoa duoc ma he thong)</summary>
    [HttpDelete("{groupId}/details/{id}")]
    [RequirePermission("code.manage")]
    public async Task<IActionResult> Delete(string groupId, string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCodeDetailCommand(groupId, id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CODE_IS_SYSTEM_READONLY")
                return StatusCode(403, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }
}
