using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.VitalSigns;

public static class VitalSignsValidator
{
    public static string? ValidateRanges(VitalSignsRequest req)
    {
        if (req.TemperatureC.HasValue && (req.TemperatureC < 30 || req.TemperatureC > 45))
            return "Nhiệt độ phải trong khoảng 30-45°C";
        if (req.HeartRateBpm.HasValue && (req.HeartRateBpm < 20 || req.HeartRateBpm > 300))
            return "Nhịp tim phải trong khoảng 20-300 bpm";
        if (req.BpSystolic.HasValue && (req.BpSystolic < 50 || req.BpSystolic > 300))
            return "Huyết áp tâm thu phải 50-300 mmHg";
        if (req.BpDiastolic.HasValue && (req.BpDiastolic < 30 || req.BpDiastolic > 200))
            return "Huyết áp tâm trương phải 30-200 mmHg";
        if (req.Spo2Percent.HasValue && (req.Spo2Percent < 0 || req.Spo2Percent > 100))
            return "SpO2 phải 0-100%";
        if (req.GlucoseMgDl.HasValue && (req.GlucoseMgDl < 20 || req.GlucoseMgDl > 1000))
            return "Đường huyết phải trong khoảng 20-1000 mg/dL";
        if (req.RespiratoryRate.HasValue && (req.RespiratoryRate < 5 || req.RespiratoryRate > 60))
            return "Nhịp thở phải 5-60 lần/phút";
        return null;
    }

    public static decimal? ComputeBmi(decimal? weight, decimal? height)
    {
        if (!weight.HasValue || !height.HasValue || height == 0) return null;
        var heightM = height.Value / 100m;
        return Math.Round(weight.Value / (heightM * heightM), 1);
    }
}

file static class VitalSignsMapper
{
    public static VitalSignsResponse Map(Domain.Entities.VitalSigns e, string? recorderName = null) =>
        new VitalSignsResponse(
            e.Id,
            Guid.TryParse(e.EncounterId, out var eid) ? eid : Guid.Empty,
            e.RecordedAt,
            e.RecordedBy,
            recorderName,
            e.TemperatureC,
            e.HeartRateBpm,
            e.RespiratoryRate,
            e.BpSystolic,
            e.BpDiastolic,
            e.Spo2Percent,
            e.WeightKg,
            e.HeightCm,
            e.PainScale,
            e.GlucoseMgDl,
            e.Note,
            VitalSignsValidator.ComputeBmi(e.WeightKg, e.HeightCm),
            e.RecordSequence,
            e.CreatedAt);
}

// ─────────────────────────────────────────────────
// CREATE
// ─────────────────────────────────────────────────
public class CreateVitalSignsCommandHandler : IRequestHandler<CreateVitalSignsCommand, Result<VitalSignsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateVitalSignsCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<VitalSignsResponse>> Handle(CreateVitalSignsCommand cmd, CancellationToken ct)
    {
        var err = VitalSignsValidator.ValidateRanges(cmd.Request);
        if (err is not null) return Result<VitalSignsResponse>.Failure("VITAL_INVALID_RANGE", err);

        var encIdStr = cmd.EncounterId.ToString();
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id.ToString() == encIdStr, ct);
        if (enc is null)
            return Result<VitalSignsResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var maxSeq = await _db.VitalSigns
            .Where(v => v.EncounterId == encIdStr)
            .MaxAsync(v => (int?)v.RecordSequence, ct) ?? 0;
        var seq = maxSeq + 1;

        var req = cmd.Request;
        var entity = new Domain.Entities.VitalSigns
        {
            TenantId       = _tenant.TenantId,
            EncounterId    = encIdStr,
            PatientId      = enc.PatientId,
            RecordedAt     = req.RecordedAt ?? DateTime.UtcNow,
            RecordedBy     = _user.UserId?.ToString(),
            RecordSequence = seq,
            TemperatureC   = req.TemperatureC,
            HeartRateBpm   = req.HeartRateBpm,
            RespiratoryRate = req.RespiratoryRate,
            BpSystolic     = req.BpSystolic,
            BpDiastolic    = req.BpDiastolic,
            Spo2Percent    = req.Spo2Percent,
            WeightKg       = req.WeightKg,
            HeightCm       = req.HeightCm,
            PainScale      = req.PainScale,
            GlucoseMgDl    = req.GlucoseMgDl,
            Note           = req.Note,
            CreatedBy      = _user.UserId,
        };

        _db.VitalSigns.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("CREATE", "VitalSigns", entity.Id.ToString(), new { encounterId = cmd.EncounterId }, ct);

        string? recorderName = null;
        if (_user.UserId.HasValue)
        {
            var u = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == _user.UserId.Value, ct);
            recorderName = u?.FullName;
        }

        return Result<VitalSignsResponse>.Success(VitalSignsMapper.Map(entity, recorderName));
    }
}

// ─────────────────────────────────────────────────
// BATCH CREATE
// ─────────────────────────────────────────────────
public class BatchCreateVitalSignsCommandHandler
    : IRequestHandler<BatchCreateVitalSignsCommand, Result<IReadOnlyList<VitalSignsResponse>>>
{
    private readonly IMediator _mediator;

    public BatchCreateVitalSignsCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<Result<IReadOnlyList<VitalSignsResponse>>> Handle(BatchCreateVitalSignsCommand cmd, CancellationToken ct)
    {
        var results = new List<VitalSignsResponse>();
        foreach (var record in cmd.Records)
        {
            var r = await _mediator.Send(new CreateVitalSignsCommand(cmd.EncounterId, record), ct);
            if (!r.IsSuccess) return Result<IReadOnlyList<VitalSignsResponse>>.Failure(r.ErrorCode!, r.ErrorMessage!);
            results.Add(r.Value!);
        }
        return Result<IReadOnlyList<VitalSignsResponse>>.Success(results.AsReadOnly());
    }
}

// ─────────────────────────────────────────────────
// UPDATE
// ─────────────────────────────────────────────────
public class UpdateVitalSignsCommandHandler : IRequestHandler<UpdateVitalSignsCommand, Result<VitalSignsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateVitalSignsCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<VitalSignsResponse>> Handle(UpdateVitalSignsCommand cmd, CancellationToken ct)
    {
        var err = VitalSignsValidator.ValidateRanges(cmd.Request);
        if (err is not null) return Result<VitalSignsResponse>.Failure("VITAL_INVALID_RANGE", err);

        var entity = await _db.VitalSigns.FirstOrDefaultAsync(e => e.Id == cmd.VitalSignId, ct);
        if (entity is null)
            return Result<VitalSignsResponse>.Failure("VITAL_NOT_FOUND", "Không tìm thấy bản ghi sinh hiệu");

        if ((DateTime.UtcNow - entity.CreatedAt).TotalHours > 24)
            return Result<VitalSignsResponse>.Failure("VITAL_EDIT_TIMEOUT", "Chỉ được chỉnh sửa sinh hiệu trong vòng 24h");

        if (entity.RecordedBy != _user.UserId?.ToString())
            return Result<VitalSignsResponse>.Failure("VITAL_EDIT_TIMEOUT", "Chỉ người nhập mới được sửa sinh hiệu");

        var req = cmd.Request;
        if (req.RecordedAt.HasValue)  entity.RecordedAt    = req.RecordedAt.Value;
        entity.TemperatureC   = req.TemperatureC;
        entity.HeartRateBpm   = req.HeartRateBpm;
        entity.RespiratoryRate = req.RespiratoryRate;
        entity.BpSystolic     = req.BpSystolic;
        entity.BpDiastolic    = req.BpDiastolic;
        entity.Spo2Percent    = req.Spo2Percent;
        entity.WeightKg       = req.WeightKg;
        entity.HeightCm       = req.HeightCm;
        entity.PainScale      = req.PainScale;
        entity.GlucoseMgDl    = req.GlucoseMgDl;
        entity.Note           = req.Note;
        entity.UpdatedBy      = _user.UserId;

        await _db.SaveChangesAsync(ct);

        string? recorderName = null;
        if (!string.IsNullOrEmpty(entity.RecordedBy))
        {
            var u = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id.ToString() == entity.RecordedBy, ct);
            recorderName = u?.FullName;
        }

        return Result<VitalSignsResponse>.Success(VitalSignsMapper.Map(entity, recorderName));
    }
}

// ─────────────────────────────────────────────────
// DELETE
// ─────────────────────────────────────────────────
public class DeleteVitalSignsCommandHandler : IRequestHandler<DeleteVitalSignsCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public DeleteVitalSignsCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<bool>> Handle(DeleteVitalSignsCommand cmd, CancellationToken ct)
    {
        var entity = await _db.VitalSigns.FirstOrDefaultAsync(e => e.Id == cmd.VitalSignId, ct);
        if (entity is null)
            return Result<bool>.Failure("VITAL_NOT_FOUND", "Không tìm thấy bản ghi sinh hiệu");

        if ((DateTime.UtcNow - entity.CreatedAt).TotalHours > 24)
            return Result<bool>.Failure("VITAL_EDIT_TIMEOUT", "Chỉ được xóa sinh hiệu trong vòng 24h");

        if (entity.RecordedBy != _user.UserId?.ToString())
            return Result<bool>.Failure("VITAL_EDIT_TIMEOUT", "Chỉ người nhập mới được xóa sinh hiệu");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

// ─────────────────────────────────────────────────
// LIST BY ENCOUNTER
// ─────────────────────────────────────────────────
public class ListVitalSignsByEncounterQueryHandler
    : IRequestHandler<ListVitalSignsByEncounterQuery, Result<IReadOnlyList<VitalSignsResponse>>>
{
    private readonly IApplicationDbContext _db;

    public ListVitalSignsByEncounterQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<VitalSignsResponse>>> Handle(ListVitalSignsByEncounterQuery q, CancellationToken ct)
    {
        var encIdStr = q.EncounterId.ToString();
        var items = await _db.VitalSigns
            .Where(v => v.EncounterId == encIdStr)
            .OrderBy(v => v.RecordedAt)
            .ToListAsync(ct);

        var responses = new List<VitalSignsResponse>();
        foreach (var v in items)
        {
            string? recorderName = null;
            if (!string.IsNullOrEmpty(v.RecordedBy))
            {
                var u = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == v.RecordedBy, ct);
                recorderName = u?.FullName;
            }
            responses.Add(VitalSignsMapper.Map(v, recorderName));
        }

        return Result<IReadOnlyList<VitalSignsResponse>>.Success(responses.AsReadOnly());
    }
}

// ─────────────────────────────────────────────────
// GET LATEST
// ─────────────────────────────────────────────────
public class GetLatestVitalSignsQueryHandler
    : IRequestHandler<GetLatestVitalSignsQuery, Result<VitalSignsResponse?>>
{
    private readonly IApplicationDbContext _db;

    public GetLatestVitalSignsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<VitalSignsResponse?>> Handle(GetLatestVitalSignsQuery q, CancellationToken ct)
    {
        var encIdStr = q.EncounterId.ToString();
        var entity = await _db.VitalSigns
            .Where(v => v.EncounterId == encIdStr)
            .OrderByDescending(v => v.RecordedAt)
            .FirstOrDefaultAsync(ct);

        if (entity is null) return Result<VitalSignsResponse?>.Success(null);

        string? recorderName = null;
        if (!string.IsNullOrEmpty(entity.RecordedBy))
        {
            var u = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id.ToString() == entity.RecordedBy, ct);
            recorderName = u?.FullName;
        }

        return Result<VitalSignsResponse?>.Success(VitalSignsMapper.Map(entity, recorderName));
    }
}

// ─────────────────────────────────────────────────
// HISTORY (by patient)
// ─────────────────────────────────────────────────
public class GetVitalSignsHistoryQueryHandler
    : IRequestHandler<GetVitalSignsHistoryQuery, Result<IReadOnlyList<VitalSignsResponse>>>
{
    private readonly IApplicationDbContext _db;

    public GetVitalSignsHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<VitalSignsResponse>>> Handle(GetVitalSignsHistoryQuery q, CancellationToken ct)
    {
        var patIdStr = q.PatientId.ToString();
        var query = _db.VitalSigns.Where(v => v.PatientId == patIdStr);

        if (q.DateFrom.HasValue)
        {
            var fromDt = q.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(v => v.RecordedAt >= fromDt);
        }
        if (q.DateTo.HasValue)
        {
            var toDt = q.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(v => v.RecordedAt <= toDt);
        }

        var items = await query
            .OrderByDescending(v => v.RecordedAt)
            .ToListAsync(ct);

        var responses = new List<VitalSignsResponse>();
        foreach (var v in items)
        {
            string? recorderName = null;
            if (!string.IsNullOrEmpty(v.RecordedBy))
            {
                var u = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == v.RecordedBy, ct);
                recorderName = u?.FullName;
            }
            responses.Add(VitalSignsMapper.Map(v, recorderName));
        }

        return Result<IReadOnlyList<VitalSignsResponse>>.Success(responses.AsReadOnly());
    }
}
