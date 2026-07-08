using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Ai;

namespace ProDiabHis.Api.Controllers;

public record TreatmentSuggestionRequest(Guid? EncounterId);
public record UpdateAiSuggestionStatusRequest(string Status);

[ApiController]
[Authorize]
public class AiSuggestionController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiSuggestionController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/patients/{id}/ai/treatment-suggestion
    [HttpPost("api/v1/patients/{id:guid}/ai/treatment-suggestion")]
    [RequirePermission("ai.suggest")]
    public async Task<IActionResult> GenerateSuggestion(Guid id, [FromBody] TreatmentSuggestionRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateTreatmentSuggestionCommand(id, request?.EncounterId), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok(new { data = result.Value });
    }

    // PATCH /api/v1/ai/suggestions/{logId}
    [HttpPatch("api/v1/ai/suggestions/{logId:guid}")]
    [RequirePermission("ai.suggest")]
    public async Task<IActionResult> UpdateStatus(Guid logId, [FromBody] UpdateAiSuggestionStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAiSuggestionStatusCommand(logId, request.Status), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }
}
