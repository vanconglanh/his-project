using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Admin endpoint quan ly feature flags.
/// Chi SUPER_ADMIN moi co quyen write.
/// </summary>
[ApiController]
[Route("api/v1/admin/feature-flags")]
[Authorize]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;

    public FeatureFlagsController(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    /// <summary>GET /api/v1/admin/feature-flags — Lay danh sach tat ca flags</summary>
    [HttpGet]
    [RequireSuperAdmin]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var flags = await _featureFlagService.GetAllAsync(ct);
        return Ok(new { data = flags });
    }

    /// <summary>GET /api/v1/admin/feature-flags/{key} — Kiem tra trang thai mot flag</summary>
    [HttpGet("{key}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var enabled = await _featureFlagService.IsEnabledAsync(key, ct);
        return Ok(new { data = new { key, enabled } });
    }

    /// <summary>PUT /api/v1/admin/feature-flags/{key} — Cap nhat trang thai flag</summary>
    [HttpPut("{key}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> Set(string key, [FromBody] SetFeatureFlagRequest request, CancellationToken ct)
    {
        await _featureFlagService.SetEnabledAsync(key, request.Enabled, ct);
        return Ok(new { data = new { key, enabled = request.Enabled } });
    }
}

public record SetFeatureFlagRequest(bool Enabled);
