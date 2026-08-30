using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Billing.InterBranchDebts;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// Dot 4 da chi nhanh: Cong no noi bo giua cac chi nhanh (BR-84, BR-85, BR-87, US-5.2).
/// Route: /api/v1/inter-branch-debts.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/inter-branch-debts")]
public class InterBranchDebtsController : ControllerBase
{
    private readonly IMediator _mediator;
    public InterBranchDebtsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult FromResult<T>(ProDiabHis.Application.Common.Result<T> result)
    {
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode switch
            {
                InterBranchDebtErrors.NotFound => StatusCodes.Status404NotFound,
                InterBranchDebtErrors.BranchAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return new ObjectResult(new { error = new { code = result.ErrorCode, message = result.ErrorMessage, details = result.ErrorDetails } })
            { StatusCode = status };
        }
        return new OkObjectResult(new { data = result.Value });
    }

    [HttpGet]
    [RequirePermission("inter_branch_debt.read")]
    public async Task<IActionResult> List(
        [FromQuery] int? debtor_branch_id, [FromQuery] int? creditor_branch_id, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListInterBranchDebtsQuery(debtor_branch_id, creditor_branch_id, status, page, page_size), ct);
        if (!result.IsSuccess) return FromResult(result);
        var paged = result.Value!;
        return Ok(new { data = paged.Items, meta = new { page = paged.Page, page_size = paged.PageSize, total = paged.Total } });
    }

    [HttpPost("{id}/settle")]
    [RequirePermission("inter_branch_debt.settle")]
    public async Task<IActionResult> Settle(Guid id, [FromBody] SettleInterBranchDebtRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SettleInterBranchDebtCommand(id, request ?? new SettleInterBranchDebtRequest(null)), ct);
        return FromResult(result);
    }
}
