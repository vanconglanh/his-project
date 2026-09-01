using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Settings;

namespace ProDiabHis.Api.Controllers;

/// <summary>Cau hinh he thong duoc phep doc boi FE (whitelist qua meta.is_public) — moi user dang nhap deu doc duoc.
/// Muc dich: tranh FE hardcode nguong/tham so nhu stock_transfer_approval_threshold (Viec 3.1).</summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach cau hinh public { key: value } — chi cac key duoc admin danh dau is_public=1</summary>
    [HttpGet("public")]
    public async Task<IActionResult> Public(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicSettingsQuery(), ct);
        return Ok(new { data = result.Value });
    }
}
