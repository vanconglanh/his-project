using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Clinical;

/// <summary>
/// [G03] Kiem tra khoa benh an. Trang thai DONE/CANCELLED => toan bo du lieu lam sang READ-ONLY,
/// chi sua duoc qua ban dinh chinh (addendum). Can cu Luat KCB 2023 / TT 32-2023.
/// </summary>
public class EncounterLockGuard : IEncounterLockGuard
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IDapperConnectionFactory _dapper;
    private readonly ILogger<EncounterLockGuard> _logger;

    public EncounterLockGuard(IApplicationDbContext db, ITenantProvider tenant,
        IDapperConnectionFactory dapper, ILogger<EncounterLockGuard> logger)
    {
        _db = db; _tenant = tenant; _dapper = dapper; _logger = logger;
    }

    public async Task<Result> EnsureEditableAsync(Guid encounterId, CancellationToken ct)
    {
        // Global query filter da loc tenant_id + deleted_at.
        var enc = await _db.Encounters.AsNoTracking()
            .Where(e => e.Id == encounterId)
            .Select(e => new { e.Status, e.LockedAt, e.FinishedAt })
            .FirstOrDefaultAsync(ct);

        if (enc is null)
            return Result.Failure(EncounterLockErrors.EncounterNotFound, "Không tìm thấy lượt khám");

        if (!EncounterStatus.IsLockedStatus(enc.Status))
            return Result.Success();

        return Result.Failure(
            EncounterLockErrors.EncounterLocked,
            EncounterLockErrors.LockedMessage,
            new
            {
                encounterId,
                status     = enc.Status,
                lockedAt   = enc.LockedAt ?? enc.FinishedAt,
                finishedAt = enc.FinishedAt,
                canAmend   = enc.Status == EncounterStatus.Done
            });
    }

    public async Task<Result<EncounterLockInfo>> GetLockStateAsync(Guid encounterId, CancellationToken ct)
    {
        var enc = await _db.Encounters.AsNoTracking()
            .Where(e => e.Id == encounterId)
            .Select(e => new { e.Id, e.Status, e.LockedAt, e.LockedBy, e.FinishedAt, e.AmendmentCount })
            .FirstOrDefaultAsync(ct);

        if (enc is null)
            return Result<EncounterLockInfo>.Failure(EncounterLockErrors.EncounterNotFound, "Không tìm thấy lượt khám");

        string? lockedByName = null;
        if (enc.LockedBy.HasValue)
        {
            lockedByName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == enc.LockedBy.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        var isLocked = EncounterStatus.IsLockedStatus(enc.Status);
        var warning  = isLocked ? await GetBhytWarningAsync(encounterId, enc.FinishedAt, ct) : null;

        return Result<EncounterLockInfo>.Success(new EncounterLockInfo(
            EncounterId:    enc.Id,
            Status:         enc.Status,
            IsLocked:       isLocked,
            LockedAt:       enc.LockedAt ?? enc.FinishedAt,
            LockedById:     enc.LockedBy,
            LockedByName:   lockedByName,
            FinishedAt:     enc.FinishedAt,
            CanAmend:       enc.Status == EncounterStatus.Done,
            AmendmentCount: enc.AmendmentCount,
            BhytWarning:    warning));
    }

    /// <summary>
    /// Canh bao (KHONG chan) neu luot kham da nam trong ho so BHYT da gui giam dinh.
    /// Uu tien tra nguoc chinh xac qua diab_his_int_bhyt_export_items.source_encounter_id;
    /// neu ho so cu chua ghi cot nay thi fallback heuristic theo ky thang (Heuristic = true).
    /// </summary>
    public async Task<BhytWarningDto?> GetBhytWarningAsync(Guid encounterId, DateTime? finishedAt, CancellationToken ct)
    {
        try
        {
            using var conn = (IDbConnection?)_dapper.CreateConnection();
            if (conn is null) return null;

            var tenantId = _tenant.TenantId;

            var row = await conn.QueryFirstOrDefaultAsync<BhytExportRow>(new CommandDefinition(
                @"SELECT e.id AS ExportId, e.period_month AS PeriodMonth, e.status AS Status,
                         e.submitted_at AS SubmittedAt
                  FROM diab_his_int_bhyt_export_items i
                  JOIN diab_his_int_bhyt_exports e
                    ON e.id = i.export_id AND e.tenant_id = i.tenant_id AND e.deleted_at IS NULL
                  WHERE i.tenant_id = @tenantId
                    AND i.source_encounter_id = @encounterId
                    AND e.status IN ('SUBMITTED','APPROVED','PARTIALLY_REJECTED','REJECTED')
                  ORDER BY e.submitted_at DESC
                  LIMIT 1",
                new { tenantId, encounterId = encounterId.ToString() }, cancellationToken: ct));

            if (row is not null)
                return Map(row, heuristic: false);

            if (finishedAt is null) return null;

            var period = finishedAt.Value.ToString("yyyy-MM");
            var fb = await conn.QueryFirstOrDefaultAsync<BhytExportRow>(new CommandDefinition(
                @"SELECT e.id AS ExportId, e.period_month AS PeriodMonth, e.status AS Status,
                         e.submitted_at AS SubmittedAt
                  FROM diab_his_int_bhyt_exports e
                  WHERE e.tenant_id = @tenantId
                    AND e.period_month = @period
                    AND e.deleted_at IS NULL
                    AND e.status IN ('SUBMITTED','APPROVED','PARTIALLY_REJECTED','REJECTED')
                    AND NOT EXISTS (SELECT 1 FROM diab_his_int_bhyt_export_items i2
                                    WHERE i2.export_id = e.id AND i2.source_encounter_id IS NOT NULL)
                  ORDER BY e.submitted_at DESC
                  LIMIT 1",
                new { tenantId, period }, cancellationToken: ct));

            return fb is null ? null : Map(fb, heuristic: true);
        }
        catch (Exception ex)
        {
            // Canh bao BHYT la thong tin phu — khong duoc lam fail luong khoa/dinh chinh.
            _logger.LogWarning(ex, "EncounterLockGuard: khong doc duoc canh bao BHYT cho encounter {Id}", encounterId);
            return null;
        }
    }

    private static BhytWarningDto Map(BhytExportRow r, bool heuristic) => new(
        Submitted:    true,
        ExportId:     r.ExportId,
        PeriodMonth:  r.PeriodMonth,
        ExportStatus: r.Status,
        SubmittedAt:  r.SubmittedAt,
        Message:      EncounterLockErrors.BhytWarnMessage,
        Heuristic:    heuristic);

    public async Task<Guid?> ResolveEncounterIdAsync(string childKind, Guid childId, CancellationToken ct)
    {
        var table = childKind switch
        {
            EncounterChildKind.VitalSigns   => "diab_his_enc_vital_signs",
            EncounterChildKind.Prescription => "diab_his_pha_prescriptions",
            EncounterChildKind.LabOrder     => "diab_his_cli_lab_orders",
            EncounterChildKind.RadOrder     => "diab_his_cli_rad_orders",
            EncounterChildKind.Diagnosis    => "diab_his_enc_diagnoses",
            _ => null
        };
        if (table is null) return null;

        try
        {
            using var conn = (IDbConnection?)_dapper.CreateConnection();
            if (conn is null) return null;

            var sql = "SELECT encounter_id FROM `" + table + "` " +
                      "WHERE id = @childId AND tenant_id = @tenantId AND deleted_at IS NULL LIMIT 1";

            var raw = await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
                sql, new { childId = childId.ToString(), tenantId = _tenant.TenantId }, cancellationToken: ct));

            return Guid.TryParse(raw, out var g) ? g : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EncounterLockGuard: khong tra nguoc duoc encounter tu {Kind}/{Id}", childKind, childId);
            return null;
        }
    }

    private sealed class BhytExportRow
    {
        public int ExportId { get; set; }
        public string? PeriodMonth { get; set; }
        public string? Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
