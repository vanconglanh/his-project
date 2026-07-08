using System.Data;
using Dapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.ChronicCare;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Api.Controllers;

public record UpdateRecallStatusRequest(string Status, string? Note, string? Channel);
public record NotifyRecallRequest(string? Channel);

[ApiController]
[Authorize]
[Route("api/v1")]
public class RecallController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISmsGateway _smsGateway;

    public RecallController(IMediator mediator, IDapperConnectionFactory db, ICurrentUser currentUser, ISmsGateway smsGateway)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
        _smsGateway = smsGateway;
    }

    // GET /api/v1/recall
    [HttpGet("recall")]
    [RequirePermission("recall.read")]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] DateOnly? dueBefore,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListRecallQuery(status, dueBefore, page, pageSize), ct);
        return Ok(new { data = result.Value!.Items, meta = new { page = result.Value!.Page, pageSize = result.Value!.PageSize, total = result.Value!.Total } });
    }

    // PATCH /api/v1/recall/{id}
    [HttpPatch("recall/{id:guid}")]
    [RequirePermission("recall.manage")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRecallStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRecallStatusCommand(id, request.Status, request.Note, request.Channel), ct);
        if (!result.IsSuccess)
            return StatusCode(422, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        return Ok();
    }

    // POST /api/v1/recall/{id}/notify
    [HttpPost("recall/{id:guid}/notify")]
    [RequirePermission("recall.manage")]
    public async Task<IActionResult> Notify(Guid id, [FromBody] NotifyRecallRequest? request, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var recall = await conn.QueryFirstOrDefaultAsync<(string PatientId, string? Phone)?>(
            @"SELECT r.patient_id AS PatientId, pat.phone AS Phone
              FROM diab_his_cli_followup_recall r
              JOIN diab_his_pat_patients pat ON pat.id = r.patient_id AND pat.tenant_id = r.tenant_id
              WHERE r.id = @id AND r.tenant_id = @tenantId AND r.deleted_at IS NULL",
            new { id = id.ToString(), tenantId });

        if (recall is null)
            return StatusCode(422, new { error = new { code = "RECALL_NOT_FOUND", message = "Không tìm thấy recall" } });

        var channel = request?.Channel ?? "SMS";

        if (string.Equals(channel, "SMS", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(recall.Value.Phone))
        {
            try
            {
                await _smsGateway.SendAsync(recall.Value.Phone!,
                    "Pro-Diab HIS: Da den han tai kham/xet nghiem HbA1c dinh ky. Vui long lien he phong kham de dat lich.", ct);
            }
            catch
            {
                // Khong chan luong neu gui SMS that bai - van cap nhat channel + log
            }
        }

        await conn.ExecuteAsync(
            "UPDATE diab_his_cli_followup_recall SET channel = @channel, updated_at = NOW(3) WHERE id = @id AND tenant_id = @tenantId",
            new { channel, id = id.ToString(), tenantId });

        return Ok(new { data = new { notified = true, channel } });
    }

    // GET /api/v1/care-pathway/targets
    [HttpGet("care-pathway/targets")]
    [RequirePermission("diabetes.assess")]
    public async Task<IActionResult> CarePathwayTargets([FromQuery] string code = "DM_T2_5481", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCarePathwayTargetsQuery(code), ct);
        return Ok(new { data = result.Value });
    }
}
