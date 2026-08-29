using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Doctors;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// P2-07: danh ba bac si rut gon phuc vu dat lich / tao luot kham.
/// Bao ve bang quyen HEP (appointment.read) - le_tan va bac_si deu da co san,
/// khong can them mo rong quyen. Muc tieu dai han: thay the viec dung
/// GET /api/v1/users?role=bac_si (yeu cau user.read - qua rong, tra ca email/
/// phone/trang thai tai khoan) bang endpoint nay.
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DoctorsController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/doctors/lookup
    [HttpGet("lookup")]
    [RequirePermission("appointment.read")]
    public async Task<IActionResult> Lookup([FromQuery] string? q, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DoctorLookupQuery(q), ct);
        return Ok(new { data = result });
    }
}
