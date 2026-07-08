using System.Data;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Encounters;

// ────────────────────────────────────────────────
// Create
// ────────────────────────────────────────────────
public class CreateEncounterCommandHandler : IRequestHandler<CreateEncounterCommand, Result<EncounterResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateEncounterCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<EncounterResponse>> Handle(CreateEncounterCommand command, CancellationToken ct)
    {
        var req = command.Request;

        // Validate patient thuoc tenant (HasQueryFilter tu dong loc)
        var patientExists = await _db.Patients.AnyAsync(p => p.Id.ToString() == req.PatientId, ct);
        if (!patientExists)
            return Result<EncounterResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var now = DateTime.UtcNow;
        var encounter = new Encounter
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            RoomId = req.RoomId,
            EncounterType = req.EncounterType,
            Status = EncounterStatus.Waiting,
            ReasonForVisit = req.ReasonForVisit,
            ChiefComplaint = req.ChiefComplaint,
            CreatedAt = now,
            CreatedBy = _user.UserId,
            UpdatedAt = now
        };

        _db.Encounters.Add(encounter);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE", "Encounter", encounter.Id.ToString(), new { type = req.EncounterType }, ct);

        var response = await BuildEncounterResponse(encounter, ct);
        return Result<EncounterResponse>.Success(response);
    }

    internal async Task<EncounterResponse> BuildEncounterResponse(Encounter enc, CancellationToken ct)
    {
        PatientSummaryDto? patSummary = null;
        if (Guid.TryParse(enc.PatientId, out var patGuid))
        {
            var patient = await _db.Patients.AsNoTracking()
                .Where(p => p.Id == patGuid)
                .Select(p => new { p.FullName, p.DateOfBirth, p.Gender, p.Phone })
                .FirstOrDefaultAsync(ct);

            if (patient != null)
                patSummary = new PatientSummaryDto(patient.FullName, patient.DateOfBirth?.Year, patient.Gender, patient.Phone);
        }

        string? doctorName = null;
        if (Guid.TryParse(enc.DoctorId, out var docGuid))
        {
            doctorName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == docGuid)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        var emrSignedCnt = await _db.EmrContents.AsNoTracking()
            .CountAsync(e => e.EncounterId == enc.Id.ToString() && e.SignedAt != null, ct);

        var started = enc.StartedAt;
        var alertOver12h = enc.Status == EncounterStatus.InProgress
            && started.HasValue
            && (DateTime.UtcNow - started.Value).TotalHours > 12;

        return new EncounterResponse(
            enc.Id, enc.TenantId, enc.PatientId, patSummary,
            enc.RoomId, enc.DoctorId, doctorName,
            enc.EncounterType, enc.ReasonForVisit, enc.ChiefComplaint,
            enc.Status, enc.StartedAt, enc.FinishedAt, alertOver12h,
            Array.Empty<DiagnosisResponse>(), null,
            emrSignedCnt > 0, false, enc.CreatedAt);
    }
}

// ────────────────────────────────────────────────
// Update
// ────────────────────────────────────────────────
public class UpdateEncounterCommandHandler : IRequestHandler<UpdateEncounterCommand, Result<EncounterResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public UpdateEncounterCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<EncounterResponse>> Handle(UpdateEncounterCommand cmd, CancellationToken ct)
    {
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == cmd.EncounterId, ct);
        if (enc is null)
            return Result<EncounterResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var req = cmd.Request;
        if (!string.IsNullOrEmpty(req.RoomId)) enc.RoomId = req.RoomId;
        if (!string.IsNullOrEmpty(req.DoctorId)) enc.DoctorId = req.DoctorId;
        if (!string.IsNullOrEmpty(req.EncounterType)) enc.EncounterType = req.EncounterType;
        if (!string.IsNullOrEmpty(req.ReasonForVisit)) enc.ReasonForVisit = req.ReasonForVisit;
        if (!string.IsNullOrEmpty(req.ChiefComplaint)) enc.ChiefComplaint = req.ChiefComplaint;
        enc.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UPDATE", "Encounter", enc.Id.ToString(), null, ct);

        var helper = new CreateEncounterCommandHandler(_db, _tenant, _user, _audit);
        return Result<EncounterResponse>.Success(await helper.BuildEncounterResponse(enc, ct));
    }
}

// ────────────────────────────────────────────────
// Admit (Tiep don -> Kham): tao/lay luot kham tu ve hang doi
// ────────────────────────────────────────────────
public class AdmitTicketToEncounterCommandHandler
    : IRequestHandler<AdmitTicketToEncounterCommand, Result<AdmitTicketResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public AdmitTicketToEncounterCommandHandler(IApplicationDbContext db, IDapperConnectionFactory dapper,
        ITenantProvider tenant, ICurrentUser user, IAuditService audit)
    {
        _db = db; _dapper = dapper; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<AdmitTicketResponse>> Handle(AdmitTicketToEncounterCommand cmd, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        using var conn = (IDbConnection)_dapper.CreateConnection();

        // 1) Doc ve hang doi
        var ticket = await conn.QueryFirstOrDefaultAsync(
            @"SELECT patient_id, room_id, doctor_id, reason_for_visit, status
              FROM diab_his_rcp_queue_tickets
              WHERE id = @Id AND tenant_id = @Tid AND deleted_at IS NULL",
            new { Id = cmd.TicketId.ToString(), Tid = tenantId });
        if (ticket is null)
            return Result<AdmitTicketResponse>.Failure("TICKET_NOT_FOUND", "Không tìm thấy phiếu tiếp đón");

        string patientId = (string)ticket.patient_id;
        string? roomId = ticket.room_id is null ? null : (string)ticket.room_id;
        string? doctorId = ticket.doctor_id is null ? null : (string)ticket.doctor_id;
        string? reason = ticket.reason_for_visit is null ? null : (string)ticket.reason_for_visit;
        string ticketStatus = (string)ticket.status;

        // 2) Idempotent: neu benh nhan da co luot kham dang hoat dong (Cho/Dang kham) thi dung lai
        var existing = await _db.Encounters
            .Where(e => e.PatientId == patientId
                     && (e.Status == EncounterStatus.Waiting || e.Status == EncounterStatus.InProgress))
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return Result<AdmitTicketResponse>.Success(new AdmitTicketResponse(existing.Id, false));

        // 3) Tao luot kham moi tu thong tin ve
        var now = DateTime.UtcNow;
        var enc = new Encounter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = patientId,
            RoomId = roomId,
            DoctorId = doctorId ?? _user.UserId?.ToString(),
            EncounterType = EncounterTypes.FirstVisit,
            Status = EncounterStatus.Waiting,
            ReasonForVisit = reason,
            CreatedAt = now,
            CreatedBy = _user.UserId,
            UpdatedAt = now
        };
        _db.Encounters.Add(enc);

        // Ve dang Cho -> chuyen Da goi (dua vao kham ngu y da goi benh nhan)
        if (ticketStatus == TicketStatus.Waiting)
        {
            await conn.ExecuteAsync(
                @"UPDATE diab_his_rcp_queue_tickets
                  SET status = 'CALLED', called_at = @now, updated_at = @now
                  WHERE id = @Id AND tenant_id = @Tid",
                new { now, Id = cmd.TicketId.ToString(), Tid = tenantId });
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ADMIT", "Encounter", enc.Id.ToString(),
            new { ticketId = cmd.TicketId }, ct);

        return Result<AdmitTicketResponse>.Success(new AdmitTicketResponse(enc.Id, true));
    }
}

// ────────────────────────────────────────────────
// Start
// ────────────────────────────────────────────────
public class StartEncounterCommandHandler : IRequestHandler<StartEncounterCommand, Result<EncounterResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IDapperConnectionFactory _dapper;

    public StartEncounterCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, IDapperConnectionFactory dapper)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit; _dapper = dapper;
    }

    public async Task<Result<EncounterResponse>> Handle(StartEncounterCommand cmd, CancellationToken ct)
    {
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == cmd.EncounterId, ct);
        if (enc is null)
            return Result<EncounterResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        if (!EncounterStatus.CanTransition(enc.Status, EncounterStatus.InProgress))
            return Result<EncounterResponse>.Failure("ENCOUNTER_INVALID_TRANSITION",
                $"Không thể chuyển trạng thái từ {enc.Status} sang IN_PROGRESS");

        enc.Status = EncounterStatus.InProgress;
        enc.StartedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(enc.DoctorId)) enc.DoctorId = _user.UserId?.ToString();
        enc.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("START", "Encounter", enc.Id.ToString(), null, ct);

        // Dong bo VE hang doi: bac si "Bat dau kham" -> ve chuyen sang "Dang kham"
        // (de man Tiep don + o thong ke "Dang kham" phan anh dung).
        await QueueTicketSync.SyncStatusAsync(
            _dapper, enc.TenantId, enc.PatientId, enc.RoomId, TicketStatus.InProgress, ct);

        var helper = new CreateEncounterCommandHandler(_db, _tenant, _user, _audit);
        return Result<EncounterResponse>.Success(await helper.BuildEncounterResponse(enc, ct));
    }
}

// ────────────────────────────────────────────────
// Close
// ────────────────────────────────────────────────
public class CloseEncounterCommandHandler : IRequestHandler<CloseEncounterCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IDapperConnectionFactory _dapper;

    public CloseEncounterCommandHandler(IApplicationDbContext db, ICurrentUser user, IAuditService audit,
        IDapperConnectionFactory dapper)
    {
        _db = db; _user = user; _audit = audit; _dapper = dapper;
    }

    public async Task<Result<bool>> Handle(CloseEncounterCommand cmd, CancellationToken ct)
    {
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == cmd.EncounterId, ct);
        if (enc is null)
            return Result<bool>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        if (!EncounterStatus.CanTransition(enc.Status, EncounterStatus.Done))
            return Result<bool>.Failure("ENCOUNTER_INVALID_TRANSITION",
                $"Không thể đóng lượt khám từ trạng thái {enc.Status}");

        if (string.IsNullOrWhiteSpace(enc.ChiefComplaint))
            return Result<bool>.Failure("ENCOUNTER_MISSING_CHIEF_COMPLAINT", "Cần nhập lý do khám chính trước khi đóng");

        var encIdStr = enc.Id.ToString();

        var diagCount = await _db.EncounterDiagnoses
            .CountAsync(d => d.EncounterId == encIdStr && d.Type == DiagnosisType.Primary, ct);
        if (diagCount == 0)
            return Result<bool>.Failure("ENCOUNTER_MISSING_DIAGNOSIS", "Cần ít nhất 1 chẩn đoán ICD-10 CHÍNH trước khi đóng");

        var emrSigned = await _db.EmrContents
            .CountAsync(e => e.EncounterId == encIdStr && e.SignedAt != null, ct);
        if (emrSigned == 0)
            return Result<bool>.Failure("EMR_NOT_SIGNED", "Bệnh án cần được ký số trước khi đóng lượt khám");

        enc.Status = EncounterStatus.Done;
        enc.FinishedAt = DateTime.UtcNow;
        enc.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CLOSE", "Encounter", enc.Id.ToString(), null, ct);

        // Dong luot kham -> ve hang doi chuyen sang "Xong".
        await QueueTicketSync.SyncStatusAsync(
            _dapper, enc.TenantId, enc.PatientId, enc.RoomId, TicketStatus.Done, ct);

        return Result<bool>.Success(true);
    }
}

/// <summary>
/// Dong bo trang thai VE hang doi (diab_his_rcp_queue_tickets) theo LUOT KHAM.
/// ReceptionTicket khong nam trong EF context nen cap nhat truc tiep bang Dapper.
/// Match theo benh nhan + phong (neu biet) + cac trang thai con hoat dong; khong lam
/// fail luong kham neu khong tim thay ve tuong ung (0 dong bi anh huong).
/// </summary>
internal static class QueueTicketSync
{
    public static async Task SyncStatusAsync(
        IDapperConnectionFactory dapper, int tenantId, string patientId, string? roomId,
        string newStatus, CancellationToken ct)
    {
        // Dong bo trang thai ve hang doi tiep don chi la SIDE-EFFECT hien thi (man Tiep don + thong ke).
        // Khong duoc lam hong nghiep vu bat dau/dong luot kham neu buoc dong bo nay loi -> non-fatal.
        try
        {
            using var conn = (IDbConnection)dapper.CreateConnection();
            var now = DateTime.UtcNow;
            // Trang thai nguon hop le: Bat dau kham chi tu Cho/Da goi; Xong thi tu ca Dang kham.
            var fromStatuses = newStatus == TicketStatus.InProgress
                ? new[] { TicketStatus.Waiting, TicketStatus.Called }
                : new[] { TicketStatus.Waiting, TicketStatus.Called, TicketStatus.InProgress };
            var startedAt  = newStatus == TicketStatus.InProgress ? (DateTime?)now : null;
            var finishedAt = newStatus == TicketStatus.Done       ? (DateTime?)now : null;

            await conn.ExecuteAsync(
                @"UPDATE diab_his_rcp_queue_tickets
                  SET status      = @newStatus,
                      started_at  = COALESCE(@startedAt, started_at),
                      finished_at = COALESCE(@finishedAt, finished_at),
                      updated_at  = @now
                  WHERE tenant_id = @tenantId
                    AND patient_id = @patientId
                    AND status IN @fromStatuses
                    AND deleted_at IS NULL
                    AND (@roomId IS NULL OR room_id = @roomId)",
                new { newStatus, startedAt, finishedAt, now, tenantId, patientId, fromStatuses, roomId });
        }
        catch
        {
            // Nuot loi dong bo hang doi (khong chan luong kham). Loi thuc su van duoc log boi middleware neu bubble len.
        }
    }
}

// ────────────────────────────────────────────────
// Chief complaint
// ────────────────────────────────────────────────
public class UpdateChiefComplaintCommandHandler : IRequestHandler<UpdateChiefComplaintCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateChiefComplaintCommandHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task<Result<bool>> Handle(UpdateChiefComplaintCommand cmd, CancellationToken ct)
    {
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == cmd.EncounterId, ct);
        if (enc is null)
            return Result<bool>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        enc.ChiefComplaint = cmd.ChiefComplaint;
        enc.UpdatedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Add Diagnosis
// ────────────────────────────────────────────────
public class AddDiagnosisCommandHandler : IRequestHandler<AddDiagnosisCommand, Result<DiagnosisResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public AddDiagnosisCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<DiagnosisResponse>> Handle(AddDiagnosisCommand cmd, CancellationToken ct)
    {
        var encExists = await _db.Encounters.AnyAsync(e => e.Id == cmd.EncounterId, ct);
        if (!encExists)
            return Result<DiagnosisResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var icdName = cmd.Request.Icd10Code; // fallback; ICD-10 lookup có thể bổ sung sau
        var now = DateTime.UtcNow;
        var diagnosis = new EncounterDiagnosis
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            EncounterId = cmd.EncounterId.ToString(),
            Icd10Code = cmd.Request.Icd10Code,
            Name = icdName,
            Type = cmd.Request.Type,
            Note = cmd.Request.Note,
            CreatedAt = now,
            CreatedBy = _user.UserId,
            UpdatedAt = now
        };

        _db.EncounterDiagnoses.Add(diagnosis);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ADD_DIAGNOSIS", "Encounter", cmd.EncounterId.ToString(),
            new { icd10Code = cmd.Request.Icd10Code }, ct);

        return Result<DiagnosisResponse>.Success(new DiagnosisResponse(
            diagnosis.Id, diagnosis.Icd10Code, icdName, diagnosis.Type, diagnosis.Note, now));
    }
}

// ────────────────────────────────────────────────
// Remove Diagnosis
// ────────────────────────────────────────────────
public class RemoveDiagnosisCommandHandler : IRequestHandler<RemoveDiagnosisCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;

    public RemoveDiagnosisCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(RemoveDiagnosisCommand cmd, CancellationToken ct)
    {
        var diagnosis = await _db.EncounterDiagnoses
            .FirstOrDefaultAsync(d => d.Id == cmd.DiagnosisId && d.EncounterId == cmd.EncounterId.ToString(), ct);

        if (diagnosis is null)
            return Result<bool>.Failure("DIAGNOSIS_NOT_FOUND", "Không tìm thấy chẩn đoán");

        diagnosis.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Get Detail
// ────────────────────────────────────────────────
public class GetEncounterDetailQueryHandler : IRequestHandler<GetEncounterDetailQuery, Result<EncounterDetailResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public GetEncounterDetailQueryHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<EncounterDetailResponse>> Handle(GetEncounterDetailQuery query, CancellationToken ct)
    {
        var enc = await _db.Encounters.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EncounterId, ct);

        if (enc is null)
            return Result<EncounterDetailResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var helper = new CreateEncounterCommandHandler(_db, _tenant, _user, _audit);
        var base_ = await helper.BuildEncounterResponse(enc, ct);

        var encIdStr = enc.Id.ToString();

        var diags = await _db.EncounterDiagnoses.AsNoTracking()
            .Where(d => d.EncounterId == encIdStr)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new DiagnosisResponse(d.Id, d.Icd10Code, d.Name, d.Type, d.Note, d.CreatedAt))
            .ToListAsync(ct);

        var latestVital = await _db.VitalSigns.AsNoTracking()
            .Where(v => v.EncounterId == encIdStr)
            .OrderByDescending(v => v.RecordedAt)
            .FirstOrDefaultAsync(ct);

        var emrRow = await _db.EmrContents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);

        EmrSummaryDto? emrSummary = emrRow is null ? null
            : new EmrSummaryDto(emrRow.Id, emrRow.SignedAt, emrRow.Version);

        return Result<EncounterDetailResponse>.Success(new EncounterDetailResponse(
            base_.Id, base_.TenantId, base_.PatientId, base_.PatientSummary,
            base_.RoomId, base_.DoctorId, base_.DoctorName,
            base_.EncounterType, base_.ReasonForVisit, base_.ChiefComplaint,
            base_.Status, base_.StartedAt, base_.FinishedAt, base_.AlertOver12h,
            diags.AsReadOnly(), latestVital, base_.HasEmrSigned, base_.HasPrescription, base_.CreatedAt,
            Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>(),
            emrSummary));
    }
}

// ────────────────────────────────────────────────
// List Encounters
// ────────────────────────────────────────────────
public class ListEncountersQueryHandler : IRequestHandler<ListEncountersQuery, Result<PagedResult<EncounterResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public ListEncountersQueryHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit;
    }

    public async Task<Result<PagedResult<EncounterResponse>>> Handle(ListEncountersQuery q, CancellationToken ct)
    {
        var query = _db.Encounters.AsNoTracking();

        if (!string.IsNullOrEmpty(q.PatientId)) query = query.Where(e => e.PatientId == q.PatientId);
        if (!string.IsNullOrEmpty(q.DoctorId))  query = query.Where(e => e.DoctorId == q.DoctorId);
        if (!string.IsNullOrEmpty(q.RoomId))    query = query.Where(e => e.RoomId == q.RoomId);
        if (!string.IsNullOrEmpty(q.Status))    query = query.Where(e => e.Status == q.Status);
        if (!string.IsNullOrEmpty(q.EncounterType)) query = query.Where(e => e.EncounterType == q.EncounterType);
        if (q.DateFrom.HasValue)
        {
            var from = q.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.CreatedAt >= from);
        }
        if (q.DateTo.HasValue)
        {
            var to = q.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(e => e.CreatedAt <= to);
        }

        var total = await query.CountAsync(ct);
        var offset = (q.Page - 1) * q.PageSize;
        var encounters = await query.OrderByDescending(e => e.CreatedAt)
            .Skip(offset).Take(q.PageSize).ToListAsync(ct);

        var helper = new CreateEncounterCommandHandler(_db, _tenant, _user, _audit);
        var items = new List<EncounterResponse>();
        foreach (var enc in encounters)
            items.Add(await helper.BuildEncounterResponse(enc, ct));

        return Result<PagedResult<EncounterResponse>>.Success(
            new PagedResult<EncounterResponse>(items, q.Page, q.PageSize, total));
    }
}

// ────────────────────────────────────────────────
// Timeline
// ────────────────────────────────────────────────
public class GetEncounterTimelineQueryHandler : IRequestHandler<GetEncounterTimelineQuery, Result<IReadOnlyList<TimelineEventDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetEncounterTimelineQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TimelineEventDto>>> Handle(GetEncounterTimelineQuery q, CancellationToken ct)
    {
        var eidStr = q.EncounterId.ToString();
        var events = new List<TimelineEventDto>();

        var vitals = await _db.VitalSigns.AsNoTracking()
            .Where(v => v.EncounterId == eidStr)
            .OrderBy(v => v.RecordedAt)
            .ToListAsync(ct);

        foreach (var v in vitals)
            events.Add(new TimelineEventDto(v.RecordedAt, "VITAL", null, null,
                v.Id, $"Sinh hiệu: HR={v.HeartRateBpm} BP={v.BpSystolic}/{v.BpDiastolic}", null));

        var emr = await _db.EmrContents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EncounterId == eidStr, ct);
        if (emr != null)
        {
            var evType = emr.SignedAt.HasValue ? "EMR_SIGNED" : "EMR_SAVED";
            events.Add(new TimelineEventDto(emr.UpdatedAt, evType, null, null,
                emr.Id, $"Bệnh án v{emr.Version}", null));
        }

        events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return Result<IReadOnlyList<TimelineEventDto>>.Success(events.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Over 12h Alerts
// ────────────────────────────────────────────────
public class GetOver12hAlertsQueryHandler : IRequestHandler<GetOver12hAlertsQuery, Result<IReadOnlyList<Over12hAlertDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetOver12hAlertsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<Over12hAlertDto>>> Handle(GetOver12hAlertsQuery q, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-12);

        var encounters = await _db.Encounters.AsNoTracking()
            .Where(e => e.Status == EncounterStatus.InProgress
                && e.StartedAt.HasValue && e.StartedAt.Value < cutoff)
            .ToListAsync(ct);

        var result = encounters.Select(e =>
        {
            var started = e.StartedAt!.Value;
            return new Over12hAlertDto(e.Id, "", null, started,
                (DateTime.UtcNow - started).TotalHours, null);
        }).ToList();

        return Result<IReadOnlyList<Over12hAlertDto>>.Success(result.AsReadOnly());
    }
}
