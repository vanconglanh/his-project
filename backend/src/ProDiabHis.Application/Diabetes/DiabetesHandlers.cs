using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using System.Text.Json;

namespace ProDiabHis.Application.Diabetes;

// ────────────── Commands ──────────────
public record CreateDiabetesAssessmentCommand(Guid EncounterId, DiabetesAssessmentRequest Request)
    : IRequest<Result<DiabetesAssessmentResponse>>;

public record UpdateDiabetesAssessmentCommand(Guid EncounterId, DiabetesAssessmentRequest Request)
    : IRequest<Result<bool>>;

public record GetDiabetesAssessmentQuery(Guid EncounterId)
    : IRequest<Result<DiabetesAssessmentResponse?>>;

public record GetDiabetesHistoryQuery(Guid PatientId, DateOnly? DateFrom, DateOnly? DateTo)
    : IRequest<Result<IReadOnlyList<DiabetesAssessmentResponse>>>;

public record ListDiabetesTemplatesQuery() : IRequest<Result<IReadOnlyList<DiabetesTemplateResponse>>>;
public record CreateDiabetesTemplateCommand(DiabetesTemplateRequest Request) : IRequest<Result<bool>>;
public record UpdateDiabetesTemplateCommand(Guid TemplateId, DiabetesTemplateRequest Request) : IRequest<Result<bool>>;

// ────────────────────────────────────────────────
// Create Assessment
// ────────────────────────────────────────────────
public class CreateDiabetesAssessmentCommandHandler
    : IRequestHandler<CreateDiabetesAssessmentCommand, Result<DiabetesAssessmentResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public CreateDiabetesAssessmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<DiabetesAssessmentResponse>> Handle(CreateDiabetesAssessmentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // Validate HbA1c range
        if (req.Hba1c.HasValue && (req.Hba1c < 3 || req.Hba1c > 20))
            return Result<DiabetesAssessmentResponse>.Failure("DIABETES_INVALID_HBA1C", "HbA1c phải trong khoảng 3-20%");

        using var conn = _db.CreateConnection();

        // Validate encounter
        var enc = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, patient_id FROM cli_visits WHERE id=@Id AND tenant_id=@TId AND deleted_at IS NULL",
            new { Id = cmd.EncounterId.ToString(), TId = _tenant.TenantId });
        if (enc is null) return Result<DiabetesAssessmentResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        // Check 1 per encounter
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM diab_his_cli_diabetes_assessments WHERE encounter_id=@EId AND deleted_at IS NULL",
            new { EId = cmd.EncounterId.ToString() });
        if (exists > 0) return Result<DiabetesAssessmentResponse>.Failure("DIABETES_ASSESSMENT_EXISTS", "Đã có đánh giá ĐTĐ cho lượt khám này");

        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var userId = _user.UserId?.ToString();
        var compJson = req.Complications is null ? null : JsonSerializer.Serialize(req.Complications);
        var targetJson = req.TreatmentTarget is null ? null : JsonSerializer.Serialize(req.TreatmentTarget);

        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_cli_diabetes_assessments
                (id, tenant_id, encounter_id, patient_id,
                 hba1c, fasting_glucose, postprandial_glucose, random_glucose,
                 egfr, serum_creatinine, urine_acr, bp_systolic, bp_diastolic,
                 bmi, waist_circumference, diabetes_type, complications, treatment_target, note,
                 assessed_at, assessed_by, created_at, created_by, updated_at)
            VALUES
                (@Id, @TId, @EId, @PatId,
                 @Hba1c, @FG, @PPG, @RG,
                 @Egfr, @Creat, @Acr, @SBP, @DBP,
                 @Bmi, @Waist, @DType, @Comp, @Target, @Note,
                 @Now, @UserId, @Now, @UserId, @Now)",
            new
            {
                Id = id, TId = _tenant.TenantId, EId = cmd.EncounterId.ToString(), PatId = (string)enc.patient_id,
                Hba1c = req.Hba1c, FG = req.FastingGlucose, PPG = req.PostprandialGlucose, RG = req.RandomGlucose,
                Egfr = req.Egfr, Creat = req.SerumCreatinine, Acr = req.UrineAcr,
                SBP = req.BpSystolic, DBP = req.BpDiastolic,
                Bmi = req.Bmi, Waist = req.WaistCircumference, DType = req.DiabetesType,
                Comp = compJson, Target = targetJson, Note = req.Note,
                Now = now, UserId = userId
            });

        await _audit.LogAsync("CREATE", "DiabetesAssessment", id, new { encounterId = cmd.EncounterId }, ct);

        return Result<DiabetesAssessmentResponse>.Success(MapRow(id, cmd.EncounterId.ToString(),
            (string)enc.patient_id, req, now, userId));
    }

    private static DiabetesAssessmentResponse MapRow(string id, string encId, string patId,
        DiabetesAssessmentRequest req, DateTime assessedAt, string? assessedBy)
        => new DiabetesAssessmentResponse(
            Guid.Parse(id), Guid.Parse(encId), Guid.Parse(patId),
            req.Hba1c, req.FastingGlucose, req.PostprandialGlucose, req.RandomGlucose,
            req.Egfr, req.SerumCreatinine, req.UrineAcr, req.BpSystolic, req.BpDiastolic,
            req.Bmi, req.WaistCircumference, req.DiabetesType,
            req.Complications, req.TreatmentTarget, req.Note,
            assessedAt,
            string.IsNullOrEmpty(assessedBy) ? null : Guid.Parse(assessedBy), null);
}

// ────────────────────────────────────────────────
// Get Assessment
// ────────────────────────────────────────────────
public class GetDiabetesAssessmentQueryHandler
    : IRequestHandler<GetDiabetesAssessmentQuery, Result<DiabetesAssessmentResponse?>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetDiabetesAssessmentQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<DiabetesAssessmentResponse?>> Handle(GetDiabetesAssessmentQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT da.*, u.full_name AS assessed_by_name
            FROM diab_his_cli_diabetes_assessments da
            LEFT JOIN sec_users u ON u.id COLLATE utf8mb4_unicode_ci=da.assessed_by
            WHERE da.encounter_id=@EId AND da.tenant_id=@TId AND da.deleted_at IS NULL",
            new { EId = q.EncounterId.ToString(), TId = _tenant.TenantId });

        if (row is null) return Result<DiabetesAssessmentResponse?>.Success(null);
        return Result<DiabetesAssessmentResponse?>.Success(MapDbRow(row));
    }

    internal static DiabetesAssessmentResponse MapDbRow(dynamic row)
    {
        ComplicationsDto? comp = null;
        if ((string?)row.complications is not null)
        {
            try { comp = JsonSerializer.Deserialize<ComplicationsDto>((string)row.complications); } catch { }
        }
        TreatmentTargetDto? target = null;
        if ((string?)row.treatment_target is not null)
        {
            try { target = JsonSerializer.Deserialize<TreatmentTargetDto>((string)row.treatment_target); } catch { }
        }

        return new DiabetesAssessmentResponse(
            Guid.Parse((string)row.id), Guid.Parse((string)row.encounter_id), Guid.Parse((string)row.patient_id),
            (decimal?)row.hba1c, (decimal?)row.fasting_glucose, (decimal?)row.postprandial_glucose, (decimal?)row.random_glucose,
            (decimal?)row.egfr, (decimal?)row.serum_creatinine, (decimal?)row.urine_acr,
            (int?)row.bp_systolic, (int?)row.bp_diastolic, (decimal?)row.bmi, (decimal?)row.waist_circumference,
            (string?)row.diabetes_type, comp, target, (string?)row.note,
            (DateTime)row.assessed_at,
            string.IsNullOrEmpty((string?)row.assessed_by) ? null : Guid.Parse((string)row.assessed_by),
            (string?)row.assessed_by_name);
    }
}

// ────────────────────────────────────────────────
// Update Assessment
// ────────────────────────────────────────────────
public class UpdateDiabetesAssessmentCommandHandler : IRequestHandler<UpdateDiabetesAssessmentCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IAuditService _audit;

    public UpdateDiabetesAssessmentCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, IAuditService audit)
    { _db = db; _tenant = tenant; _audit = audit; }

    public async Task<Result<bool>> Handle(UpdateDiabetesAssessmentCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (req.Hba1c.HasValue && (req.Hba1c < 3 || req.Hba1c > 20))
            return Result<bool>.Failure("DIABETES_INVALID_HBA1C", "HbA1c phải trong khoảng 3-20%");

        using var conn = _db.CreateConnection();
        var compJson = req.Complications is null ? null : JsonSerializer.Serialize(req.Complications);
        var targetJson = req.TreatmentTarget is null ? null : JsonSerializer.Serialize(req.TreatmentTarget);

        var affected = await conn.ExecuteAsync(@"
            UPDATE diab_his_cli_diabetes_assessments SET
                hba1c=@Hba1c, fasting_glucose=@FG, postprandial_glucose=@PPG, random_glucose=@RG,
                egfr=@Egfr, serum_creatinine=@Creat, urine_acr=@Acr,
                bp_systolic=@SBP, bp_diastolic=@DBP, bmi=@Bmi, waist_circumference=@Waist,
                diabetes_type=@DType, complications=@Comp, treatment_target=@Target, note=@Note,
                updated_at=@Now
            WHERE encounter_id=@EId AND tenant_id=@TId AND deleted_at IS NULL",
            new
            {
                EId = cmd.EncounterId.ToString(), TId = _tenant.TenantId,
                Hba1c = req.Hba1c, FG = req.FastingGlucose, PPG = req.PostprandialGlucose, RG = req.RandomGlucose,
                Egfr = req.Egfr, Creat = req.SerumCreatinine, Acr = req.UrineAcr,
                SBP = req.BpSystolic, DBP = req.BpDiastolic, Bmi = req.Bmi, Waist = req.WaistCircumference,
                DType = req.DiabetesType, Comp = compJson, Target = targetJson, Note = req.Note,
                Now = DateTime.UtcNow
            });

        if (affected == 0) return Result<bool>.Failure("DIABETES_ASSESSMENT_NOT_FOUND", "Không tìm thấy đánh giá ĐTĐ");
        await _audit.LogAsync("UPDATE", "DiabetesAssessment", cmd.EncounterId.ToString(), null, ct);
        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// History
// ────────────────────────────────────────────────
public class GetDiabetesHistoryQueryHandler
    : IRequestHandler<GetDiabetesHistoryQuery, Result<IReadOnlyList<DiabetesAssessmentResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public GetDiabetesHistoryQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<DiabetesAssessmentResponse>>> Handle(GetDiabetesHistoryQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var where = "WHERE da.patient_id=@PatId AND da.tenant_id=@TId AND da.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("PatId", q.PatientId.ToString()); p.Add("TId", _tenant.TenantId);

        if (q.DateFrom.HasValue) { where += " AND DATE(da.assessed_at) >= @DFrom"; p.Add("DFrom", q.DateFrom.Value.ToDateTime(TimeOnly.MinValue)); }
        if (q.DateTo.HasValue)   { where += " AND DATE(da.assessed_at) <= @DTo";   p.Add("DTo",   q.DateTo.Value.ToDateTime(TimeOnly.MinValue));   }

        var rows = await conn.QueryAsync<dynamic>($@"
            SELECT da.*, u.full_name AS assessed_by_name
            FROM diab_his_cli_diabetes_assessments da
            LEFT JOIN sec_users u ON u.id COLLATE utf8mb4_unicode_ci=da.assessed_by
            {where} ORDER BY da.assessed_at DESC", p);

        return Result<IReadOnlyList<DiabetesAssessmentResponse>>.Success(
            rows.Select(GetDiabetesAssessmentQueryHandler.MapDbRow).ToList().AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Templates
// ────────────────────────────────────────────────
public class ListDiabetesTemplatesQueryHandler
    : IRequestHandler<ListDiabetesTemplatesQuery, Result<IReadOnlyList<DiabetesTemplateResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListDiabetesTemplatesQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<DiabetesTemplateResponse>>> Handle(ListDiabetesTemplatesQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "SELECT * FROM diab_his_cli_diabetes_templates WHERE (tenant_id IS NULL OR tenant_id=@TId) AND deleted_at IS NULL ORDER BY is_system DESC, name",
            new { TId = _tenant.TenantId });

        var result = rows.Select(r =>
        {
            IReadOnlyList<string>? checklist = null;
            if ((string?)r.checklist is not null)
                try { checklist = JsonSerializer.Deserialize<List<string>>((string)r.checklist); } catch { }

            return new DiabetesTemplateResponse(
                Guid.Parse((string)r.id), (int?)r.tenant_id, (string)r.name,
                (string?)r.default_values, checklist, (bool)((sbyte)r.is_system == 1), (DateTime)r.created_at);
        }).ToList();

        return Result<IReadOnlyList<DiabetesTemplateResponse>>.Success(result.AsReadOnly());
    }
}

public class CreateDiabetesTemplateCommandHandler : IRequestHandler<CreateDiabetesTemplateCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public CreateDiabetesTemplateCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<bool>> Handle(CreateDiabetesTemplateCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO diab_his_cli_diabetes_templates (id, tenant_id, name, default_values, checklist, is_system, created_at, created_by, updated_at)
            VALUES (@Id, @TId, @Name, @Dv, @Cl, 0, @Now, @UserId, @Now)",
            new
            {
                Id = Guid.NewGuid().ToString(), TId = _tenant.TenantId, Name = cmd.Request.Name,
                Dv = cmd.Request.DefaultValues is null ? null : JsonSerializer.Serialize(cmd.Request.DefaultValues),
                Cl = cmd.Request.Checklist is null ? null : JsonSerializer.Serialize(cmd.Request.Checklist),
                Now = DateTime.UtcNow, UserId = _user.UserId?.ToString()
            });
        return Result<bool>.Success(true);
    }
}

public class UpdateDiabetesTemplateCommandHandler : IRequestHandler<UpdateDiabetesTemplateCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public UpdateDiabetesTemplateCommandHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<bool>> Handle(UpdateDiabetesTemplateCommand cmd, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE diab_his_cli_diabetes_templates SET name=@Name, default_values=@Dv, checklist=@Cl, updated_at=@Now
            WHERE id=@Id",
            new
            {
                Id = cmd.TemplateId.ToString(), Name = cmd.Request.Name,
                Dv = cmd.Request.DefaultValues is null ? null : JsonSerializer.Serialize(cmd.Request.DefaultValues),
                Cl = cmd.Request.Checklist is null ? null : JsonSerializer.Serialize(cmd.Request.Checklist),
                Now = DateTime.UtcNow
            });
        return Result<bool>.Success(true);
    }
}
