using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Application.Diabetes.Cgm;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// FR-711 [P2]: API Portal cho bệnh nhân tự liên kết tài khoản thiết bị đo đường huyết/CGM
/// (Dexcom/LibreView/...). Xác thực: JWT Portal với claim "patient_id" (giống TelehealthPortalController).
/// </summary>
[ApiController]
[Route("api/v1/portal/cgm")]
[Authorize]
public class CgmPortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public CgmPortalController(IMediator mediator) => _mediator = mediator;

    private Guid PatientId => Guid.Parse(User.FindFirst("patient_id")!.Value);

    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] CgmLinkRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LinkCgmAccountCommand(PatientId, request), ct);
        if (!result.IsSuccess)
            return StatusCode(MapErrorStatus(result.ErrorCode!), Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// FR-711: Đồng bộ (push) batch dữ liệu đo đường huyết liên tục (CGM) từ thiết bị/app của bệnh nhân.
    /// Bổ sung cho CgmReadingsSyncJob (pull định kỳ) — dùng khi thiết bị/app chủ động đẩy dữ liệu về
    /// (thay vì chờ HIS poll theo lịch). Idempotent theo (tenant_id, patient_id, provider, device_id, reading_at).
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] CgmSyncRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SyncCgmReadingsCommand(PatientId, request), ct);
        if (!result.IsSuccess)
            return StatusCode(MapErrorStatus(result.ErrorCode!), Error(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { data = result.Value });
    }

    private static int MapErrorStatus(string code) => code switch
    {
        "PATIENT_NOT_FOUND" or "CGM_ACCOUNT_NOT_LINKED" => 404,
        "CGM_PROVIDER_NOT_SUPPORTED" or "CGM_AUTH_CODE_REQUIRED" or "CGM_LINK_FAILED" or "CGM_SYNC_EMPTY_BATCH" => 400,
        "CGM_PROVIDER_NOT_CONFIGURED" or "CGM_PROVIDER_UNAVAILABLE" => 502,
        _ => 400
    };

    private static object Error(string code, string message, object? details = null) =>
        new { error = new { code, message, details } };
}
