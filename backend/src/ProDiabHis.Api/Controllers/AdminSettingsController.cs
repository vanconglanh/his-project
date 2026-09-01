using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Settings;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan tri cau hinh he thong (diab_his_sys_settings) — Viec 4, ke thua-override theo tenant.</summary>
[ApiController]
[Route("api/v1/admin/settings")]
[Authorize]
[Produces("application/json")]
public class AdminSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSettingsController(IMediator mediator) => _mediator = mediator;

    public record UpdateSettingRequest(string Value);

    /// <summary>Danh sach cau hinh kem metadata + gia tri da resolve tenant &gt; global &gt; default</summary>
    [HttpGet]
    [RequirePermission("setting.manage")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminSettingsQuery(), ct);
        return Ok(new { data = result.Value });
    }

    /// <summary>Ghi override gia tri cho tenant hien tai</summary>
    [HttpPut("{key}")]
    [RequirePermission("setting.manage")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSettingCommand(key, request.Value), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "SETTING_KEY_NOT_FOUND")
                return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return NoContent();
    }

    /// <summary>Xoa override rieng cua tenant, revert ve gia tri global</summary>
    [HttpDelete("{key}")]
    [RequirePermission("setting.manage")]
    public async Task<IActionResult> Delete(string key, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSettingOverrideCommand(key), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }
}
