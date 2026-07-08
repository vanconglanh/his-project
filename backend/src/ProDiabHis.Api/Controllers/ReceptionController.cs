using Dapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Api.Services;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Encounters;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Application.Reception;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Api.Controllers;

[ApiController]
[Route("api/v1/reception")]
[Authorize]
public class ReceptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITicketPdfService _pdfService;
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ReceptionController(IMediator mediator, ITicketPdfService pdfService, IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _mediator = mediator;
        _pdfService = pdfService;
        _db = db;
        _tenant = tenant;
    }

    // POST /api/v1/reception/check-in
    [HttpPost("check-in")]
    [RequirePermission("reception.checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CheckInCommand(request), ct);
        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                "RECEPTION_DUPLICATE_CHECKIN" or "RECEPTION_ROOM_FULL" => 409,
                "PATIENT_NOT_FOUND" or "ROOM_NOT_FOUND" => 404,
                _ => 422
            };
            return StatusCode(statusCode, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return StatusCode(201, new { data = result.Value });
    }

    // POST /api/v1/reception/patients/{id}/portal-activation
    // Le tan cap ma kich hoat portal cho benh nhan (in kem phieu kham). Tra ma plaintext 1 lan.
    [HttpPost("patients/{id:guid}/portal-activation")]
    [RequirePermission("reception.checkin")]
    public async Task<IActionResult> IssuePortalActivation(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new IssuePortalActivationCommand(id, _tenant.TenantId), ct);
            return Ok(new { data = result });
        }
        catch (PatientNotFoundException)
        {
            return NotFound(new { error = new { code = "PATIENT_NOT_FOUND", message = "Không tìm thấy bệnh nhân" } });
        }
        catch (PortalPhoneNotRegisteredException)
        {
            return BadRequest(new { error = new { code = "PATIENT_PHONE_MISSING", message = "Bệnh nhân chưa có số điện thoại trong hồ sơ" } });
        }
    }

    // GET /api/v1/reception/queue
    [HttpGet("queue")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] Guid? room_id,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListQueueQuery(room_id, status, date), ct);
        return Ok(new { data = result });
    }

    // PUT /api/v1/reception/queue/{ticketId}/call
    [HttpPut("queue/{ticketId:guid}/call")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> CallTicket(Guid ticketId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CallTicketCommand(ticketId), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "TICKET_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // POST /api/v1/reception/queue/{ticketId}/admit
    // Dua benh nhan vao kham: tao (hoac lay lai) luot kham tu ve hang doi,
    // tra ve encounter_id de FE dieu huong sang man kham /encounters/{id}.
    [HttpPost("queue/{ticketId:guid}/admit")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> AdmitTicket(Guid ticketId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AdmitTicketToEncounterCommand(ticketId), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "TICKET_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/reception/queue/{ticketId}/skip
    [HttpPut("queue/{ticketId:guid}/skip")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> SkipTicket(Guid ticketId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SkipTicketCommand(ticketId), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "TICKET_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // PUT /api/v1/reception/queue/{ticketId}/cancel
    [HttpPut("queue/{ticketId:guid}/cancel")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> CancelTicket(Guid ticketId, [FromBody] CancelTicketBody? body, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CancelTicketCommand(ticketId, body?.Reason), ct);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "TICKET_NOT_FOUND" ? 404 : 422;
            return StatusCode(status, new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });
        }
        return Ok(new { data = result.Value });
    }

    // GET /api/v1/reception/queue/{ticketId}/ticket-pdf
    [HttpGet("queue/{ticketId:guid}/ticket-pdf")]
    [RequirePermission("reception.queue.manage")]
    public async Task<IActionResult> GetTicketPdf(Guid ticketId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTicketQuery(ticketId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = new { code = result.ErrorCode, message = result.ErrorMessage } });

        LetterheadDto? letterhead;
        using (var conn = (System.Data.IDbConnection)_db.CreateConnection())
        {
            letterhead = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
                @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                         phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                         slogan AS Slogan, website AS Website
                  FROM diab_his_sys_tenants WHERE id = @tenantId", new { tenantId = _tenant.TenantId });
        }

        var pdfBytes = await _pdfService.GenerateTicketPdfAsync(result.Value!, ct, letterhead);
        return File(pdfBytes, "application/pdf", $"ticket-{result.Value!.TicketNo}.pdf");
    }

    // GET /api/v1/reception/rooms
    [HttpGet("rooms")]
    [RequirePermission("reception.rooms.read")]
    public async Task<IActionResult> GetRooms(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListRoomsQuery(), ct);
        return Ok(new { data = result });
    }

    // GET /api/v1/reception/stats
    [HttpGet("stats")]
    [RequirePermission("reception.stats.read")]
    public async Task<IActionResult> GetStats([FromQuery] DateOnly? date, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetReceptionStatsQuery(date), ct);
        return Ok(new { data = result });
    }
}

public record CancelTicketBody(string? Reason);
