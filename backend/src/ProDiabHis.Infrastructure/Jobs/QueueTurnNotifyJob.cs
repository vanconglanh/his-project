using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.PublicApi;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Enqueue tu handler khi 1 phieu tiep don chuyen sang CALLED (xem ReceptionHandlers.
/// TicketTransitionHelper). Thong bao cho cac benh nhan WAITING con lai (<=3 nguoi truoc)
/// cung phong rang sap den luot.
/// </summary>
public class QueueTurnNotifyJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPatientNotifyService _notify;
    private readonly ILogger<QueueTurnNotifyJob> _logger;

    public QueueTurnNotifyJob(IDapperConnectionFactory db, IPatientNotifyService notify, ILogger<QueueTurnNotifyJob> logger)
    {
        _db = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task ExecuteAsync(string roomId, int tenantId, string calledTicketId, CancellationToken ct = default)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var calledTicket = await conn.QueryFirstOrDefaultAsync<(string ticket_no, DateTime ticket_date)>(
            "SELECT ticket_no, ticket_date FROM diab_his_rcp_queue_tickets WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = calledTicketId, TenantId = tenantId });

        if (calledTicket.ticket_no == null) return;

        var upcoming = (await conn.QueryAsync<(string patient_id, string ticket_no)>(
            @"SELECT patient_id, ticket_no
              FROM diab_his_rcp_queue_tickets
              WHERE tenant_id = @TenantId AND room_id = @RoomId AND ticket_date = @Date
                AND status = 'WAITING' AND deleted_at IS NULL
                AND CAST(ticket_no AS UNSIGNED) > CAST(@CalledNo AS UNSIGNED)
                AND CAST(ticket_no AS UNSIGNED) <= CAST(@CalledNo AS UNSIGNED) + 3
              ORDER BY CAST(ticket_no AS UNSIGNED)",
            new { TenantId = tenantId, RoomId = roomId, Date = calledTicket.ticket_date.ToString("yyyy-MM-dd"), CalledNo = calledTicket.ticket_no }))
            .ToList();

        foreach (var u in upcoming)
        {
            try
            {
                await _notify.NotifyAsync(Guid.Parse(u.patient_id), tenantId, "QUEUE_TURN_SOON",
                    "Sắp đến lượt khám",
                    $"Số hiện tại đang gọi là {calledTicket.ticket_no}. Số của bạn là {u.ticket_no}, vui lòng có mặt tại phòng khám.",
                    null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gui thong bao sap den luot cho benh nhan {PatientId} that bai", u.patient_id);
            }
        }
    }
}
