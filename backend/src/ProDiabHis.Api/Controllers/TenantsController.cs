using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Tenants;

namespace ProDiabHis.Api.Controllers;

/// <summary>Quan ly phong kham (tenant) trong he thong</summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Danh sach tenant (SUPER_ADMIN)</summary>
    [HttpGet]
    [RequireSuperAdmin]
    public async Task<IActionResult> ListTenants(
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListTenantsQuery(page, page_size, status, q), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { page = result.Page, page_size = result.PageSize, total = result.Total, total_pages = result.TotalPages }
        });
    }

    /// <summary>Tao tenant moi (SUPER_ADMIN)</summary>
    [HttpPost]
    [RequireSuperAdmin]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateTenantCommand(
            request.Code, request.Name, request.CskcbCode, request.TaxCode,
            request.Address, request.Phone, request.Email, request.Subdomain,
            request.StorageQuotaGb, request.AdminEmail, request.AdminFullName, request.ExpiresAt), ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Chi tiet tenant (SUPER_ADMIN)</summary>
    [HttpGet("{id:int}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> GetTenant(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat tenant (SUPER_ADMIN)</summary>
    [HttpPut("{id:int}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> UpdateTenant(int id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTenantCommand(
            id, request.Name, request.CskcbCode, request.TaxCode,
            request.Address, request.Phone, request.Email, request.StorageQuotaGb, request.ExpiresAt), ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "TENANT_NOT_FOUND"
                ? NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } })
                : UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new { data = result.Value });
    }

    /// <summary>Cham dut tenant - soft delete (SUPER_ADMIN)</summary>
    [HttpDelete("{id:int}")]
    [RequireSuperAdmin]
    public async Task<IActionResult> DeleteTenant(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTenantCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return NoContent();
    }

    /// <summary>Tam ngung tenant (SUPER_ADMIN)</summary>
    [HttpPost("{id:int}/suspend")]
    [RequireSuperAdmin]
    public async Task<IActionResult> SuspendTenant(int id, [FromBody] SuspendRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SuspendTenantCommand(id, request?.Reason), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Kich hoat lai tenant (SUPER_ADMIN)</summary>
    [HttpPost("{id:int}/activate")]
    [RequireSuperAdmin]
    public async Task<IActionResult> ActivateTenant(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ActivateTenantCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Thong tin phong kham hien tai cua user dang dang nhap (alias /me)</summary>
    [HttpGet("current")]
    [RequirePermission("tenant.read")]
    public async Task<IActionResult> GetCurrentTenant(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyTenantQuery(), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Thong tin phong kham cua user dang dang nhap</summary>
    [HttpGet("me")]
    [RequirePermission("tenant.read")]
    public async Task<IActionResult> GetMyTenant(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyTenantQuery(), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Thong tin letterhead phong kham hien tai (dung in bao cao PDF)</summary>
    /// <remarks>
    /// Tra ve ClinicName, CompanyName, Address, Phone, Email, LogoUrl cua tenant hien tai.
    /// Bat ky user nao trong tenant deu xem duoc.
    /// </remarks>
    [HttpGet("me/letterhead")]
    [RequirePermission("tenant.read")]
    public async Task<IActionResult> GetMyLetterhead(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLetterheadQuery(), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    /// <summary>Cap nhat thong tin phong kham cua minh (ADMIN tenant)</summary>
    [HttpPut("me")]
    [RequirePermission("tenant.write")]
    public async Task<IActionResult> UpdateMyTenant([FromBody] UpdateTenantProfileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateMyTenantCommand(
            request.Name, request.Address, request.Phone,
            request.Email, request.CskcbCode, request.BhytToken,
            request.CompanyName, request.EmailSupport, request.LogoUrl,
            request.Slogan, request.Website), ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        return Ok(new { data = result.Value });
    }
}

// Request DTOs (Contracts)
public record CreateTenantRequest(
    string Code, string Name, string? CskcbCode, string? TaxCode,
    string? Address, string? Phone, string Email, string Subdomain,
    int StorageQuotaGb = 20, string AdminEmail = "", string AdminFullName = "",
    DateTime? ExpiresAt = null
);

public record UpdateTenantRequest(
    string? Name, string? CskcbCode, string? TaxCode,
    string? Address, string? Phone, string? Email,
    int? StorageQuotaGb, DateTime? ExpiresAt
);

public record UpdateTenantProfileRequest(
    string? Name, string? Address, string? Phone,
    string? Email, string? CskcbCode, string? BhytToken,
    string? CompanyName = null, string? EmailSupport = null, string? LogoUrl = null,
    string? Slogan = null, string? Website = null
);

public record SuspendRequest(string? Reason);
