using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Encounters.Addenda;

// ────────────────────────────────────────────────
// [G03] GET lock-state
// ────────────────────────────────────────────────
public class GetEncounterLockStateQueryHandler
    : IRequestHandler<GetEncounterLockStateQuery, Result<EncounterLockInfo>>
{
    private readonly IEncounterLockGuard _guard;

    public GetEncounterLockStateQueryHandler(IEncounterLockGuard guard) => _guard = guard;

    public Task<Result<EncounterLockInfo>> Handle(GetEncounterLockStateQuery q, CancellationToken ct)
        => _guard.GetLockStateAsync(q.EncounterId, ct);
}

// ────────────────────────────────────────────────
// [G03] POST tao ban dinh chinh
// ────────────────────────────────────────────────
public class CreateEncounterAddendumCommandHandler
    : IRequestHandler<CreateEncounterAddendumCommand, Result<EncounterAddendumResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IEncounterLockGuard _guard;
    private readonly IPermissionChecker _permissions;

    public CreateEncounterAddendumCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, IEncounterLockGuard guard, IPermissionChecker permissions)
    {
        _db = db; _tenant = tenant; _user = user; _audit = audit; _guard = guard; _permissions = permissions;
    }

    public async Task<Result<EncounterAddendumResponse>> Handle(
        CreateEncounterAddendumCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // 1. Quyen dinh chinh (defense-in-depth, controller da co [RequirePermission])
        if (!_permissions.HasPermission(EncounterLockErrors.AmendPermission))
            return Fail(EncounterLockErrors.Forbidden, EncounterLockErrors.ForbiddenMessage);

        // 2. Ly do BAT BUOC (TT 32/2023 — moi sua doi phai co vet + ly do)
        if (string.IsNullOrWhiteSpace(req.Reason))
            return Fail(EncounterLockErrors.AmendmentReasonRequired, EncounterLockErrors.ReasonMessage);

        // 3. Section / operation hop le
        if (!AddendumSection.IsValid(req.Section))
            return Fail(EncounterLockErrors.AddendumInvalidSection, "Phần đính chính không hợp lệ");

        var operation = string.IsNullOrWhiteSpace(req.Operation) ? AddendumOperation.Update : req.Operation!;
        if (!AddendumOperation.IsValid(operation))
            return Fail(EncounterLockErrors.AddendumInvalidSection, "Loại thao tác đính chính không hợp lệ");

        // 4. Encounter phai TON TAI va DANG KHOA (chua khoa thi sua truc tiep)
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == cmd.EncounterId, ct);
        if (enc is null)
            return Fail(EncounterLockErrors.EncounterNotFound, "Không tìm thấy lượt khám");

        if (!EncounterStatus.IsLockedStatus(enc.Status))
            return Fail(EncounterLockErrors.AddendumNotApplicable, "Bệnh án chưa khoá — hãy sửa trực tiếp");

        // 5. Snapshot content_before tu ban ghi goc (KHONG nhan tu client — chong gia mao vet)
        string? contentBefore = null;
        if (operation is AddendumOperation.Update or AddendumOperation.Remove)
        {
            if (string.IsNullOrWhiteSpace(req.TargetId))
                return Fail(EncounterLockErrors.AddendumTargetNotFound, "Không tìm thấy nội dung cần đính chính");

            contentBefore = await SnapshotAsync(cmd.EncounterId, req.Section, req.TargetId!, ct);
            if (contentBefore is null)
                return Fail(EncounterLockErrors.AddendumTargetNotFound, "Không tìm thấy nội dung cần đính chính");
        }

        // 6. Canh bao BHYT — KHONG chan, nhung neu da gui giam dinh thi phai xac nhan gui lai XML
        var lockState = await _guard.GetLockStateAsync(cmd.EncounterId, ct);
        var bhyt = lockState.IsSuccess ? lockState.Value!.BhytWarning : null;
        var bhytSubmitted = bhyt?.Submitted == true;

        if (bhytSubmitted && !req.AcknowledgeBhytResubmit)
            return Result<EncounterAddendumResponse>.Failure(
                EncounterLockErrors.BhytResubmitAckRequired,
                EncounterLockErrors.BhytWarnMessage,
                new { exportId = bhyt!.ExportId, periodMonth = bhyt.PeriodMonth, heuristic = bhyt.Heuristic });

        // 7. Ghi ban dinh chinh — INSERT ONLY, khong ghi de ban goc
        var now = DateTime.UtcNow;
        var addendum = new EncounterAddendum
        {
            Id                = Guid.NewGuid(),
            TenantId          = _tenant.TenantId,
            EncounterId       = cmd.EncounterId.ToString(),
            Section           = req.Section,
            Operation         = operation,
            TargetTable       = req.TargetTable,
            TargetId          = req.TargetId,
            ContentBefore     = contentBefore,
            ContentAfter      = req.ContentAfter is null ? null : JsonSerializer.Serialize(req.ContentAfter),
            Reason            = req.Reason.Trim(),
            BhytSubmittedFlag = bhytSubmitted,
            BhytExportId      = bhyt?.ExportId,
            CreatedAt         = now,
            CreatedBy         = _user.UserId,
            UpdatedAt         = now,
            UpdatedBy         = _user.UserId
        };

        _db.EncounterAddenda.Add(addendum);
        enc.AmendmentCount += 1;
        enc.UpdatedAt = now;
        enc.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);

        // 8. Audit DAY DU: ai, khi nao, noi dung truoc/sau, ly do
        await _audit.LogAsync(
            "AMEND", "Encounter", cmd.EncounterId.ToString(),
            AuditSeverity.WARN,
            crossTenantAttempt: false,
            requestId: null,
            details: new
            {
                addendumId    = addendum.Id,
                section       = addendum.Section,
                operation     = addendum.Operation,
                targetTable   = addendum.TargetTable,
                targetId      = addendum.TargetId,
                contentBefore = addendum.ContentBefore,
                contentAfter  = addendum.ContentAfter,
                reason        = addendum.Reason,
                bhytSubmitted = bhytSubmitted,
                bhytExportId  = addendum.BhytExportId
            },
            cancellationToken: ct);

        string? actorName = null;
        if (_user.UserId.HasValue)
        {
            actorName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == _user.UserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        return Result<EncounterAddendumResponse>.Success(
            AddendumMapper.Map(addendum, actorName));
    }

    /// <summary>Snapshot ban ghi goc theo section — chi lay trong pham vi encounter + tenant.</summary>
    private async Task<string?> SnapshotAsync(Guid encounterId, string section, string targetId, CancellationToken ct)
    {
        var encIdStr = encounterId.ToString();

        switch (section)
        {
            case AddendumSection.Diagnosis:
            {
                var d = await _db.EncounterDiagnoses.AsNoTracking()
                    .Where(x => x.EncounterId == encIdStr && x.Id.ToString() == targetId)
                    .Select(x => new { x.Icd10Code, x.Name, x.Type, x.Note })
                    .FirstOrDefaultAsync(ct);
                return d is null ? null : JsonSerializer.Serialize(d);
            }
            case AddendumSection.VitalSign:
            {
                var v = await _db.VitalSigns.AsNoTracking()
                    .Where(x => x.EncounterId == encIdStr && x.Id.ToString() == targetId)
                    .Select(x => new
                    {
                        x.TemperatureC, x.HeartRateBpm, x.RespiratoryRate, x.BpSystolic, x.BpDiastolic,
                        x.Spo2Percent, x.WeightKg, x.HeightCm, x.PainScale, x.GlucoseMgDl, x.Note
                    })
                    .FirstOrDefaultAsync(ct);
                return v is null ? null : JsonSerializer.Serialize(v);
            }
            case AddendumSection.Prescription:
            {
                var p = await _db.Prescriptions.AsNoTracking()
                    .Where(x => x.EncounterId == encounterId && x.Id.ToString() == targetId)
                    .Select(x => new { x.PrescriptionNo, x.Status, x.Note, x.DiagnosisIcd10 })
                    .FirstOrDefaultAsync(ct);
                return p is null ? null : JsonSerializer.Serialize(p);
            }
            case AddendumSection.ClinicalNote:
            {
                var e = await _db.EmrContents.AsNoTracking()
                    .Where(x => x.EncounterId == encIdStr && x.Id.ToString() == targetId)
                    .Select(x => new { x.ContentJson, x.Version, x.SignedAt })
                    .FirstOrDefaultAsync(ct);
                return e is null ? null : JsonSerializer.Serialize(e);
            }
            default:
                // CLS_ORDER / OTHER: khong co snapshot chuan hoa -> luu marker de van co vet.
                return JsonSerializer.Serialize(new { targetId, note = "Khong snapshot tu dong cho phan nay" });
        }
    }

    private static Result<EncounterAddendumResponse> Fail(string code, string message)
        => Result<EncounterAddendumResponse>.Failure(code, message);
}

// ────────────────────────────────────────────────
// [G03] GET danh sach ban dinh chinh
// ────────────────────────────────────────────────
public class ListEncounterAddendaQueryHandler
    : IRequestHandler<ListEncounterAddendaQuery, Result<PagedResult<EncounterAddendumResponse>>>
{
    private readonly IApplicationDbContext _db;

    public ListEncounterAddendaQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<EncounterAddendumResponse>>> Handle(
        ListEncounterAddendaQuery q, CancellationToken ct)
    {
        var encIdStr = q.EncounterId.ToString();

        // Global query filter da rang buoc tenant_id.
        var query = _db.EncounterAddenda.AsNoTracking()
            .Where(a => a.EncounterId == encIdStr);

        if (!string.IsNullOrWhiteSpace(q.Section))
            query = query.Where(a => a.Section == q.Section);

        var total = await query.CountAsync(ct);

        var page     = q.Page     <= 0 ? 1  : q.Page;
        var pageSize = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 100);

        var rows = await query
            .OrderBy(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var actorIds = rows.Where(r => r.CreatedBy.HasValue).Select(r => r.CreatedBy!.Value).Distinct().ToList();
        var actors = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        var items = rows
            .Select(r => AddendumMapper.Map(r,
                actors.FirstOrDefault(a => r.CreatedBy.HasValue && a.Id == r.CreatedBy.Value)?.FullName))
            .ToList();

        return Result<PagedResult<EncounterAddendumResponse>>.Success(
            PagedResult<EncounterAddendumResponse>.Create(items, page, pageSize, total));
    }
}

internal static class AddendumMapper
{
    public static EncounterAddendumResponse Map(EncounterAddendum a, string? actorName) => new(
        Id:                   a.Id,
        EncounterId:          Guid.TryParse(a.EncounterId, out var eid) ? eid : Guid.Empty,
        Section:              a.Section,
        Operation:            a.Operation,
        TargetTable:          a.TargetTable,
        TargetId:             a.TargetId,
        ContentBefore:        a.ContentBefore,
        ContentAfter:         a.ContentAfter,
        Reason:               a.Reason,
        CreatedAt:            a.CreatedAt,
        CreatedBy:            new AddendumActorDto(a.CreatedBy, actorName),
        BhytResubmitRequired: a.BhytSubmittedFlag && a.BhytResubmitAt is null,
        AuditLogId:           a.AuditLogId);
}
