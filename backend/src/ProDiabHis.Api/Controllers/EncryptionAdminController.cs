using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Controllers;

public record RotateKeyRequest(string Purpose, int? TenantId = null);

/// <summary>Admin endpoints quan ly encryption key rotation</summary>
[ApiController]
[Route("api/v1/admin/encryption")]
[Authorize]
[Produces("application/json")]
public class EncryptionAdminController : ControllerBase
{
    private readonly IKeyRotationService _keyRotationService;
    private readonly IEncryptionKeyStore _keyStore;

    public EncryptionAdminController(IKeyRotationService keyRotationService, IEncryptionKeyStore keyStore)
    {
        _keyRotationService = keyRotationService;
        _keyStore = keyStore;
    }

    /// <summary>Rotate encryption key cho purpose cu the</summary>
    [HttpPost("rotate-key")]
    [RequirePermission("encryption.rotate")]
    public async Task<IActionResult> RotateKey([FromBody] RotateKeyRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<KeyPurpose>(request.Purpose, ignoreCase: true, out var purpose))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_PURPOSE",
                    message = $"Purpose '{request.Purpose}' khong hop le. Cac gia tri cho phep: PII, BHYT, OAUTH_TOKEN, VAPID, OTHER"
                }
            });
        }

        var result = await _keyRotationService.RotateKeyAsync(request.TenantId, purpose, ct);

        return Ok(new
        {
            data = new
            {
                new_key_id = result.NewKeyId,
                new_version = result.NewVersion,
                old_keys_deactivated = result.OldKeysDeactivated,
                purpose = request.Purpose,
                tenant_id = request.TenantId
            },
            meta = new { }
        });
    }

    /// <summary>Danh sach cac key versions (an material)</summary>
    [HttpGet("keys")]
    [RequirePermission("encryption.rotate")]
    public async Task<IActionResult> ListKeys(
        [FromQuery] int? tenant_id = null,
        [FromQuery] string? purpose = null,
        CancellationToken ct = default)
    {
        KeyPurpose? parsedPurpose = null;
        if (!string.IsNullOrEmpty(purpose))
        {
            if (!Enum.TryParse<KeyPurpose>(purpose, ignoreCase: true, out var p))
            {
                return BadRequest(new
                {
                    error = new { code = "INVALID_PURPOSE", message = $"Purpose '{purpose}' khong hop le" }
                });
            }
            parsedPurpose = p;
        }

        var keys = await _keyStore.ListKeysAsync(tenant_id, parsedPurpose, ct);

        return Ok(new
        {
            data = keys.Select(k => new
            {
                id = k.Id,
                tenant_id = k.TenantId,
                key_version = k.KeyVersion,
                purpose = k.Purpose.ToString(),
                algorithm = k.Algorithm,
                is_active = k.IsActive,
                rotated_at = k.RotatedAt,
                created_at = k.CreatedAt
            }),
            meta = new { total = keys.Count }
        });
    }
}
