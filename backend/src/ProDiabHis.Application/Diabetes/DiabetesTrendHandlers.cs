using System.Data;
using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Diabetes;

// ─── Queries ────────────────────────────────────────────────────────────────
public record GetDiabetesTrajectoryQuery(Guid PatientId, DateOnly? From, DateOnly? To)
    : IRequest<Result<DiabetesTrajectoryResponse>>;

public record GetDeteriorationFlagsQuery(Guid PatientId) : IRequest<Result<DeteriorationFlagsResponse>>;

public record GetRiskListQuery(string? Level, string? Sort, int Page, int PageSize)
    : IRequest<Result<PagedResult<RiskListItem>>>;

// ─── Trajectory ─────────────────────────────────────────────────────────────
public class GetDiabetesTrajectoryQueryHandler
    : IRequestHandler<GetDiabetesTrajectoryQuery, Result<DiabetesTrajectoryResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDiabetesTrajectoryQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<DiabetesTrajectoryResponse>> Handle(GetDiabetesTrajectoryQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var where = "WHERE patient_id = @patientId AND tenant_id = @tenantId AND deleted_at IS NULL";
        var prm = new DynamicParameters();
        prm.Add("patientId", q.PatientId.ToString());
        prm.Add("tenantId", tenantId);

        if (q.From.HasValue) { where += " AND DATE(assessed_at) >= @from"; prm.Add("from", q.From.Value.ToDateTime(TimeOnly.MinValue)); }
        if (q.To.HasValue) { where += " AND DATE(assessed_at) <= @to"; prm.Add("to", q.To.Value.ToDateTime(TimeOnly.MinValue)); }

        var rows = await conn.QueryAsync<TrendPoint>(
            $@"SELECT assessed_at AS AssessedAt, hba1c AS Hba1c, fasting_glucose AS FastingGlucose,
                      egfr AS Egfr, bp_systolic AS BpSystolic, bp_diastolic AS BpDiastolic, bmi AS Bmi
               FROM diab_his_cli_diabetes_assessments {where}
               ORDER BY assessed_at", prm);

        var targets = await GetTargetsAsync(conn, tenantId, "DM_T2_5481", ct);

        return Result<DiabetesTrajectoryResponse>.Success(
            new DiabetesTrajectoryResponse(q.PatientId, rows.ToList().AsReadOnly(), targets));
    }

    internal static async Task<IReadOnlyList<CarePathwayTargetItem>> GetTargetsAsync(
        IDbConnection conn, int tenantId, string code, CancellationToken ct)
    {
        var rows = await conn.QueryAsync<(string Param, string TargetOp, decimal TargetValue, string? Unit, int? TenantId)>(
            @"SELECT param AS Param, target_op AS TargetOp, target_value AS TargetValue, unit AS Unit, tenant_id AS TenantId
              FROM diab_his_cli_care_pathway_target
              WHERE code = @code AND (tenant_id = @tenantId OR tenant_id IS NULL)
              ORDER BY (tenant_id IS NULL) ASC",
            new { code, tenantId });

        // Uu tien override cua tenant (tenant_id != NULL) neu co, giu 1 dong / param
        var byParam = new Dictionary<string, CarePathwayTargetItem>();
        foreach (var r in rows)
        {
            if (!byParam.ContainsKey(r.Param))
                byParam[r.Param] = new CarePathwayTargetItem(r.Param, r.TargetOp, r.TargetValue, r.Unit);
        }
        return byParam.Values.ToList().AsReadOnly();
    }
}

// ─── Deterioration flags ────────────────────────────────────────────────────
public class GetDeteriorationFlagsQueryHandler
    : IRequestHandler<GetDeteriorationFlagsQuery, Result<DeteriorationFlagsResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetDeteriorationFlagsQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<DeteriorationFlagsResponse>> Handle(GetDeteriorationFlagsQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var rows = await conn.QueryAsync<(DateTime AssessedAt, decimal? Hba1c, int? BpSystolic, int? BpDiastolic)>(
            @"SELECT assessed_at AS AssessedAt, hba1c AS Hba1c, bp_systolic AS BpSystolic, bp_diastolic AS BpDiastolic
              FROM diab_his_cli_diabetes_assessments
              WHERE patient_id = @patientId AND tenant_id = @tenantId AND deleted_at IS NULL
              ORDER BY assessed_at DESC LIMIT 6",
            new { patientId = q.PatientId.ToString(), tenantId });

        var points = rows.Select(r => new DiabetesTrendCalculator.AssessmentPoint(r.AssessedAt, r.Hba1c, r.BpSystolic, r.BpDiastolic)).ToList();

        var targets = await GetDiabetesTrajectoryQueryHandler.GetTargetsAsync(conn, tenantId, "DM_T2_5481", ct);
        var hba1cTarget = targets.FirstOrDefault(t => t.Param == "HBA1C")?.TargetValue ?? 7.0m;
        var bpSysTarget = (int)(targets.FirstOrDefault(t => t.Param == "BP_SYS")?.TargetValue ?? 130m);
        var bpDiaTarget = (int)(targets.FirstOrDefault(t => t.Param == "BP_DIA")?.TargetValue ?? 80m);

        var flags = DiabetesTrendCalculator.DetectDeterioration(points, hba1cTarget, bpSysTarget, bpDiaTarget);

        return Result<DeteriorationFlagsResponse>.Success(new DeteriorationFlagsResponse(
            q.PatientId, flags.Select(f => new DeteriorationFlagDto(f.Code, f.Message, f.Severity)).ToList().AsReadOnly()));
    }
}

// ─── Risk list ───────────────────────────────────────────────────────────────
public class GetRiskListQueryHandler : IRequestHandler<GetRiskListQuery, Result<PagedResult<RiskListItem>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetRiskListQueryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<RiskListItem>>> Handle(GetRiskListQuery q, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var tenantId = _currentUser.TenantId!.Value;

        var where = new List<string> { "rf.tenant_id = @tenantId" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);

        if (!string.IsNullOrWhiteSpace(q.Level))
        {
            where.Add("rf.risk_level = @level");
            prm.Add("level", q.Level.ToUpperInvariant());
        }

        var whereClause = string.Join(" AND ", where);
        var orderBy = q.Sort?.ToLowerInvariant() switch
        {
            "last_visit" => "rf.last_visit_at ASC",
            "hba1c" => "rf.latest_hba1c DESC",
            _ => "rf.risk_score DESC"
        };

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM diab_his_cli_patient_risk_flag rf WHERE {whereClause}", prm);

        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 20 : q.PageSize;
        prm.Add("offset", (page - 1) * pageSize);
        prm.Add("limit", pageSize);

        var rows = await conn.QueryAsync<RiskListRow>(
            $@"SELECT rf.patient_id AS PatientId, pat.code AS PatientCode, pat.full_name AS PatientFullName,
                      pat.phone AS Phone, rf.risk_level AS RiskLevel, rf.risk_score AS RiskScore,
                      rf.latest_hba1c AS LatestHba1c, rf.latest_egfr AS LatestEgfr,
                      rf.latest_bp_sys AS LatestBpSys, rf.latest_bp_dia AS LatestBpDia,
                      rf.hba1c_trend AS Hba1cTrend, rf.last_visit_at AS LastVisitAt, rf.computed_at AS ComputedAt
               FROM diab_his_cli_patient_risk_flag rf
               JOIN diab_his_pat_patients pat ON pat.id = rf.patient_id AND pat.tenant_id = rf.tenant_id
               WHERE {whereClause}
               ORDER BY {orderBy}
               LIMIT @limit OFFSET @offset", prm);

        var items = rows.Select(r => new RiskListItem(
            Guid.Parse(r.PatientId), r.PatientCode, r.PatientFullName, r.Phone,
            r.RiskLevel, r.RiskScore, r.LatestHba1c, r.LatestEgfr, r.LatestBpSys, r.LatestBpDia,
            r.Hba1cTrend, r.LastVisitAt, r.ComputedAt)).ToList();

        return Result<PagedResult<RiskListItem>>.Success(new PagedResult<RiskListItem>(items, page, pageSize, total));
    }

    private class RiskListRow
    {
        public string PatientId { get; set; } = "";
        public string PatientCode { get; set; } = "";
        public string PatientFullName { get; set; } = "";
        public string? Phone { get; set; }
        public string RiskLevel { get; set; } = "";
        public decimal RiskScore { get; set; }
        public decimal? LatestHba1c { get; set; }
        public decimal? LatestEgfr { get; set; }
        public int? LatestBpSys { get; set; }
        public int? LatestBpDia { get; set; }
        public string? Hba1cTrend { get; set; }
        public DateTime? LastVisitAt { get; set; }
        public DateTime ComputedAt { get; set; }
    }
}
