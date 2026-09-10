using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.CLS;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Api.Controllers;

/// <summary>BM-12: Bo chi dinh mau CLS (lab/rad), luu RIENG tung bac si.</summary>
[ApiController]
[Authorize]
[Route("api/v1/cls-order-templates")]
public class ClsOrderTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClsOrderTemplatesController(IMediator mediator) => _mediator = mediator;

    private IActionResult Fail<T>(Result<T> result) => Fail(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);
    private IActionResult Fail(Result result) => Fail(result.ErrorCode, result.ErrorMessage, result.ErrorDetails);

    private IActionResult Fail(string? errorCode, string? errorMessage, object? details)
    {
        var status = errorCode switch
        {
            "CLS_TEMPLATE_NOT_FOUND" => 404,
            "UNAUTHORIZED" => 401,
            "VALIDATION_ERROR" or "CLS_TEMPLATE_EMPTY" => 400,
            _ => 422
        };
        return StatusCode(status, new { error = new { code = errorCode, message = errorMessage, details } });
    }

    /// <summary>Lưu bộ chỉ định mẫu mới (của bác sĩ đang đăng nhập)</summary>
    [HttpPost]
    [RequirePermission("cls_order_template.create")]
    public async Task<IActionResult> Create([FromBody] CreateClsOrderTemplateRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CreateClsOrderTemplateCommand(request), ct);
        if (!result.IsSuccess) return Fail(result);
        return StatusCode(201, new { data = result.Value });
    }

    /// <summary>Danh sách bộ chỉ định mẫu CỦA bác sĩ đang đăng nhập (không thấy của bác sĩ khác)</summary>
    [HttpGet]
    [RequirePermission("cls_order_template.read")]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListClsOrderTemplatesQuery(), ct);
        if (!result.IsSuccess) return Fail(result);
        return Ok(new { data = result.Value!.Templates });
    }

    /// <summary>Xoá bộ chỉ định mẫu (chỉ xoá được bộ mẫu của chính mình)</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("cls_order_template.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteClsOrderTemplateCommand(id), ct);
        if (!result.IsSuccess) return Fail(result);
        return NoContent();
    }
}
