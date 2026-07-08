using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// GHI CHU: mọi query trong file này BẮT BUỘC lọc tenant_id + patient_id
// (co lap benh nhan trong cung tenant). Xem PublicApiDtos.cs cho DTO.
// ============================================================

// ============================================================
// Portal: Danh sach don thuoc cua toi
// ============================================================
public record GetPortalPrescriptionsQuery(Guid PatientId, int TenantId) : IRequest<List<PortalPrescriptionResponse>>;

public class GetPortalPrescriptionsHandler : IRequestHandler<GetPortalPrescriptionsQuery, List<PortalPrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalPrescriptionsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PortalPrescriptionResponse>> Handle(GetPortalPrescriptionsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT p.id AS id, p.created_at AS issued_at, p.note AS note, p.dtqg_code,
                     u.full_name AS doctor_name
              FROM diab_his_pha_prescriptions p
              LEFT JOIN diab_his_sec_users u ON u.id = p.doctor_id
              WHERE p.patient_id = @PatientId AND p.tenant_id = @TenantId AND p.deleted_at IS NULL
              ORDER BY p.created_at DESC",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        var items = new List<PortalPrescriptionResponse>();
        foreach (var r in rows)
        {
            string id = (string)r.id;
            var itemRows = await conn.QueryAsync<dynamic>(
                @"SELECT d.name AS drug_name, d.strength AS drug_strength, d.unit,
                         i.dosage, i.frequency, i.duration_days, i.quantity, i.instructions AS note
                  FROM diab_his_pha_prescription_items i
                  JOIN diab_his_pha_drugs d ON d.id = i.drug_id
                  WHERE i.prescription_id = @Id AND i.tenant_id = @TenantId",
                new { Id = id, q.TenantId });

            var items2 = itemRows.Select(i => new PrescriptionItemDto(
                (string)i.drug_name,
                (string)i.dosage,
                (decimal)i.quantity,
                BuildUsageInstruction((string?)i.frequency, (int?)i.duration_days, (string?)i.note))).ToList();

            var code = !string.IsNullOrWhiteSpace((string?)r.dtqg_code)
                ? (string)r.dtqg_code
                : id[..Math.Min(8, id.Length)].ToUpperInvariant();

            items.Add(new PortalPrescriptionResponse(
                Guid.TryParse(id, out var pid) ? pid : Guid.Empty,
                code,
                (DateTime)r.issued_at,
                (string?)r.doctor_name ?? "",
                (string?)r.note,
                (string?)r.dtqg_code,
                items2));
        }

        return items;
    }

    internal static string BuildUsageInstruction(string? frequency, int? durationDays, string? note)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(frequency)) parts.Add(frequency);
        if (durationDays.HasValue) parts.Add($"{durationDays.Value} ngày");
        if (!string.IsNullOrWhiteSpace(note)) parts.Add(note);
        return parts.Count > 0 ? string.Join(" - ", parts) : "";
    }
}

// ============================================================
// Portal: Ket qua xet nghiem (chi VERIFIED)
// ============================================================
public record GetPortalLabResultsQuery(Guid PatientId, int TenantId) : IRequest<List<PortalLabResultResponse>>;

public class GetPortalLabResultsHandler : IRequestHandler<GetPortalLabResultsQuery, List<PortalLabResultResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalLabResultsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<PortalLabResultResponse>> Handle(GetPortalLabResultsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT id, test_name, performed_at, value_numeric, unit, flag, status
              FROM diab_his_lab_results
              WHERE patient_id = @PatientId AND tenant_id = @TenantId
                AND status = 'VERIFIED' AND deleted_at IS NULL
              ORDER BY performed_at DESC",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        return rows.Select(r =>
        {
            string? conclusion = r.value_numeric != null
                ? $"{(decimal)r.value_numeric} {(string?)r.unit} ({(string)r.flag})"
                : null;

            return new PortalLabResultResponse(
                Guid.Parse((string)r.id),
                (string)r.test_name,
                (DateTime)r.performed_at,
                conclusion,
                (string)r.status);
        }).ToList();
    }
}

// ============================================================
// Portal: Chi tiet 1 luot kham
// ============================================================
public record GetPortalEncounterDetailQuery(Guid EncounterId, Guid PatientId, int TenantId)
    : IRequest<Result<PortalEncounterDetailResponse>>;

public class GetPortalEncounterDetailHandler
    : IRequestHandler<GetPortalEncounterDetailQuery, Result<PortalEncounterDetailResponse>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalEncounterDetailHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<Result<PortalEncounterDetailResponse>> Handle(GetPortalEncounterDetailQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var encId = q.EncounterId.ToString();

        var e = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT e.id, e.encounter_no, e.started_at, e.chief_complaint, u.full_name AS doctor_name
              FROM diab_his_enc_encounters e
              LEFT JOIN diab_his_sec_users u ON u.id = e.doctor_id
              WHERE e.id = @Id AND e.patient_id = @PatientId AND e.tenant_id = @TenantId AND e.deleted_at IS NULL",
            new { Id = encId, PatientId = q.PatientId.ToString(), q.TenantId });

        if (e == null)
            return Result<PortalEncounterDetailResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var diagRows = await conn.QueryAsync<(string icd10, string name)>(
            @"SELECT icd10_code AS icd10, name FROM diab_his_enc_diagnoses
              WHERE encounter_id = @Id AND deleted_at IS NULL
              ORDER BY (type = 'PRIMARY') DESC",
            new { Id = encId });

        // Ket luan CDHA: diab_his_rad_results lien ket qua order_id (khong co encounter_id/patient_id
        // truc tiep). Join qua rad order de lay dung ket luan cua luot kham nay.
        var conclusion = await conn.ExecuteScalarAsync<string?>(
            @"SELECT rr.conclusion FROM diab_his_rad_results rr
              JOIN diab_his_rad_orders ro ON ro.id = rr.order_id
              WHERE ro.encounter_id = @Id AND ro.tenant_id = @TenantId AND rr.deleted_at IS NULL
              ORDER BY rr.performed_at DESC LIMIT 1",
            new { Id = encId, q.TenantId });

        var presc = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, note FROM diab_his_pha_prescriptions
              WHERE encounter_id = @Id AND patient_id = @PatientId AND tenant_id = @TenantId AND deleted_at IS NULL
              ORDER BY created_at DESC LIMIT 1",
            new { Id = encId, PatientId = q.PatientId.ToString(), q.TenantId });

        string? doctorAdvice = presc?.note;
        var prescItems = new List<PortalEncounterPrescriptionItem>();
        if (presc != null)
        {
            var itemRows = await conn.QueryAsync<dynamic>(
                @"SELECT d.name AS drug_name, i.dosage, i.frequency, i.duration_days, i.instructions AS note
                  FROM diab_his_pha_prescription_items i
                  JOIN diab_his_pha_drugs d ON d.id = i.drug_id
                  WHERE i.prescription_id = @Id AND i.tenant_id = @TenantId",
                new { Id = (string)presc.id, q.TenantId });

            prescItems = itemRows.Select(i => new PortalEncounterPrescriptionItem(
                (string)i.drug_name, (string)i.dosage, (string?)i.frequency,
                (int?)i.duration_days, (string?)i.note)).ToList();
        }

        return Result<PortalEncounterDetailResponse>.Success(new PortalEncounterDetailResponse(
            Guid.Parse((string)e.id),
            (string?)e.encounter_no ?? "",
            e.started_at != null ? (DateTime)e.started_at : default,
            (string?)e.doctor_name ?? "",
            (string?)e.chief_complaint ?? "",
            diagRows.Select(d => new DiagnosisItem(d.icd10, d.name)).ToList(),
            conclusion,
            doctorAdvice,
            prescItems));
    }
}

// ============================================================
// Portal: Trang thai hang doi hom nay
// ============================================================
public record GetPortalQueueStatusQuery(Guid PatientId, int TenantId) : IRequest<PortalQueueStatusResponse?>;

public class GetPortalQueueStatusHandler : IRequestHandler<GetPortalQueueStatusQuery, PortalQueueStatusResponse?>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalQueueStatusHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<PortalQueueStatusResponse?> Handle(GetPortalQueueStatusQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        var ticket = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id, ticket_no, room_id, status
              FROM diab_his_rcp_queue_tickets
              WHERE tenant_id = @TenantId AND patient_id = @PatientId AND ticket_date = @Today
                AND status IN ('WAITING','CALLED','IN_PROGRESS') AND deleted_at IS NULL
              ORDER BY created_at DESC LIMIT 1",
            new { q.TenantId, PatientId = q.PatientId.ToString(), Today = today });

        if (ticket == null) return null;

        string roomId = (string)ticket.room_id;
        string ticketNo = (string)ticket.ticket_no;

        var roomName = await conn.ExecuteScalarAsync<string?>(
            "SELECT name FROM diab_his_sys_rooms WHERE id = @Id", new { Id = roomId });

        var currentCalledNo = await conn.ExecuteScalarAsync<string?>(
            @"SELECT ticket_no FROM diab_his_rcp_queue_tickets
              WHERE tenant_id = @TenantId AND room_id = @RoomId AND ticket_date = @Today
                AND status IN ('CALLED','IN_PROGRESS') AND deleted_at IS NULL
              ORDER BY CAST(ticket_no AS UNSIGNED) DESC LIMIT 1",
            new { q.TenantId, RoomId = roomId, Today = today });

        var waitingAhead = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_rcp_queue_tickets
              WHERE tenant_id = @TenantId AND room_id = @RoomId AND ticket_date = @Today
                AND status = 'WAITING' AND CAST(ticket_no AS UNSIGNED) < CAST(@TicketNo AS UNSIGNED)
                AND deleted_at IS NULL",
            new { q.TenantId, RoomId = roomId, Today = today, TicketNo = ticketNo });

        var avgWaitMinutes = await conn.ExecuteScalarAsync<double?>(
            @"SELECT AVG(TIMESTAMPDIFF(MINUTE, started_at, finished_at))
              FROM diab_his_rcp_queue_tickets
              WHERE tenant_id = @TenantId AND room_id = @RoomId
                AND started_at IS NOT NULL AND finished_at IS NOT NULL
                AND created_at >= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY)
                AND deleted_at IS NULL",
            new { q.TenantId, RoomId = roomId });

        var perTicketMinutes = avgWaitMinutes.HasValue && avgWaitMinutes.Value > 0 ? avgWaitMinutes.Value : 15;
        var estWaitMinutes = (int)Math.Round(waitingAhead * perTicketMinutes);

        return new PortalQueueStatusResponse(
            ticketNo, roomName, (string)ticket.status, currentCalledNo, waitingAhead, estWaitMinutes);
    }
}
