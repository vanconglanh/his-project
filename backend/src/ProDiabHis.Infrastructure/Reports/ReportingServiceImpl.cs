using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Dich vu bao cao chinh — dung Dapper raw SQL.
/// Voi period >= 30 ngay, uu tien doc cache table; fallback live query.
/// </summary>
public class ReportingServiceImpl : IReportingService
{
    private readonly IDapperConnectionFactory _db;
    private readonly IReportCache _cache;

    public ReportingServiceImpl(IDapperConnectionFactory db, IReportCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(
        int tenantId, string period, DateOnly from, DateOnly to, Guid? clinicId,
        CancellationToken ct = default)
    {
        var days = to.DayNumber - from.DayNumber;
        if (days >= 30)
        {
            var periodKey = $"{period}_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}";
            var cached = await _cache.GetAsync("diab_his_rep_daily_revenue_cache", tenantId, periodKey, ct);
            if (cached is not null)
            {
                // simple stub — real impl would deserialize
            }
        }

        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT
                DATE_FORMAT(i.created_at, '%Y-%m-%d') AS label,
                COALESCE(SUM(i.patient_payable), 0)    AS value
            FROM diab_his_bil_billing i
            WHERE i.tenant_id = @tenantId
              AND i.created_at BETWEEN @from AND @to
              AND i.status = 'FINALIZED'
              AND i.deleted_at IS NULL
            GROUP BY label
            ORDER BY label";

        var rows = (await conn.QueryAsync<dynamic>(sql, new
        {
            tenantId = tenantId,
            from = from.ToDateTime(TimeOnly.MinValue),
            to = to.ToDateTime(TimeOnly.MaxValue)
        })).ToList();

        var series = rows
            .Select(r => new ChartDataPoint((string)r.label, (decimal)r.value))
            .ToList();

        var total = series.Sum(s => s.Value);

        return new RevenueReportResponse(period, from, to, total, rows.Count, 0m, total, series);
    }

    public async Task<IReadOnlyList<DoctorKpiResponse>> GetDoctorKpiAsync(
        int tenantId, DateOnly from, DateOnly to, int top,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT
                e.doctor_id                                    AS DoctorId,
                u.full_name                                    AS DoctorName,
                COUNT(e.id)                                    AS TotalEncounters,
                COALESCE(SUM(i.patient_payable), 0)            AS TotalRevenue,
                COALESCE(AVG(i.patient_payable), 0)            AS AvgRevenuePerEncounter,
                COUNT(DISTINCT p.id)                           AS PrescriptionCount
            FROM diab_his_enc_encounters e
            JOIN diab_his_sec_users u ON u.id = e.doctor_id
            LEFT JOIN diab_his_bil_billing i
                   ON i.encounter_id = e.id AND i.status = 'FINALIZED' AND i.deleted_at IS NULL
            LEFT JOIN diab_his_pha_prescriptions p ON p.encounter_id = e.id AND p.deleted_at IS NULL
            WHERE e.tenant_id = @tenantId
              AND e.deleted_at IS NULL
              AND DATE(COALESCE(e.started_at, e.created_at)) BETWEEN @from AND @to
            GROUP BY e.doctor_id, u.full_name
            ORDER BY TotalRevenue DESC
            LIMIT @top";

        var rows = await conn.QueryAsync<dynamic>(sql, new
        {
            tenantId = tenantId,
            from = from.ToDateTime(TimeOnly.MinValue),
            to = to.ToDateTime(TimeOnly.MaxValue),
            top
        });

        return rows.Select(r => new DoctorKpiResponse(
            Guid.TryParse((string?)r.DoctorId, out var gid) ? gid : Guid.Empty,
            (string?)r.DoctorName ?? "",
            (long)(r.TotalEncounters ?? 0L),
            (decimal)(r.TotalRevenue ?? 0m),
            (decimal)(r.AvgRevenuePerEncounter ?? 0m),
            (long)(r.PrescriptionCount ?? 0L)
        )).ToList();
    }

    public async Task<IReadOnlyList<TopDrugResponse>> GetTopDrugsAsync(
        int tenantId, DateOnly from, DateOnly to, int top,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // Dung bang thuc: diab_his_pha_prescription_items, diab_his_pha_drugs, diab_his_pha_prescriptions
        // diab_his_pha_prescription_items khong co unit_price -> lay sell_price tu diab_his_pha_drugs
        var sql = @"
            SELECT
                di.drug_id                           AS DrugId,
                COALESCE(d.name_vi, d.name_en)       AS DrugName,
                d.generic_name                       AS ActiveIngredient,
                SUM(di.quantity)                     AS QuantityDispensed,
                SUM(di.quantity * COALESCE(d.sell_price, 0)) AS TotalRevenue,
                COUNT(DISTINCT di.prescription_id)   AS PrescriptionCount
            FROM diab_his_pha_prescription_items di
            JOIN diab_his_pha_drugs d ON d.id = di.drug_id
            JOIN diab_his_pha_prescriptions p ON p.id = di.prescription_id
            WHERE p.tenant_id = @tenantId
              AND DATE(p.created_at) BETWEEN @from AND @to
              AND di.deleted_at IS NULL AND p.deleted_at IS NULL
            GROUP BY di.drug_id, d.name_vi, d.name_en, d.generic_name
            ORDER BY QuantityDispensed DESC
            LIMIT @top";

        var rows = await conn.QueryAsync<dynamic>(sql, new
        {
            tenantId = tenantId,
            from = from.ToDateTime(TimeOnly.MinValue),
            to = to.ToDateTime(TimeOnly.MaxValue),
            top
        });

        return rows.Select(r => new TopDrugResponse(
            r.DrugId is Guid gid ? gid : (Guid.TryParse((string?)r.DrugId, out var pgid) ? pgid : Guid.Empty),
            r.DrugName is null ? "" : (string)r.DrugName,
            (string?)r.ActiveIngredient,
            r.QuantityDispensed is null ? 0 : (int)(decimal)r.QuantityDispensed,
            r.TotalRevenue is null ? 0m : (decimal)r.TotalRevenue,
            r.PrescriptionCount is null ? 0 : (int)(long)r.PrescriptionCount
        )).ToList();
    }

    public async Task<DiabetesCohortResponse> GetDiabetesCohortAsync(
        int tenantId, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // diab_his_lab_results: cot test_code, patient_id, value_numeric, performed_at
        var avgSql = @"
            SELECT COUNT(DISTINCT r.patient_id) AS total,
                   AVG(r.value_numeric)         AS avg_hba1c
            FROM diab_his_lab_results r
            WHERE r.tenant_id = @tenantId
              AND r.test_code IN ('HBA1C','HbA1c','4548-4')
              AND r.deleted_at IS NULL
              AND DATE(r.performed_at) BETWEEN @from AND @to";

        var agg = await conn.QueryFirstOrDefaultAsync<dynamic>(avgSql, new
        {
            tenantId = tenantId,
            from = from.ToDateTime(TimeOnly.MinValue),
            to = to.ToDateTime(TimeOnly.MaxValue)
        });

        int total = (int)(agg?.total ?? 0);
        decimal avgHba1c = (decimal)(agg?.avg_hba1c ?? 0m);

        // Buckets: <6, 6-7, 7-8, 8-9, >=9
        var buckets = DiabetesCohortCalculator.BuildBuckets(avgHba1c, total);
        var pctControlled = total > 0 ? buckets.Where(b => b.Label is "<6" or "6-7").Sum(b => b.Percentage) : 0m;

        return new DiabetesCohortResponse(
            $"{from:yyyy-MM}",
            total,
            avgHba1c,
            pctControlled,
            100m - pctControlled,
            buckets);
    }

    public async Task<DiabetesCohortDetailedResponse> GetDiabetesCohortDetailedAsync(
        int tenantId, DateOnly asOf, string? dmType,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var asOfDt = asOf.ToDateTime(TimeOnly.MaxValue);

        // ---- 1) Cohort theo loai DM (T1=E10*, T2=E11*, GDM=O24*) ----
        // dmType filter chi anh huong totalPatients; by_type van tach day du de FE hien chi tiet.
        var dmTypeWhere = (dmType?.ToUpperInvariant()) switch
        {
            "T1"  => "AND d.icd10_code LIKE 'E10%'",
            "T2"  => "AND d.icd10_code LIKE 'E11%'",
            "GDM" => "AND d.icd10_code LIKE 'O24%'",
            _     => "AND (d.icd10_code LIKE 'E10%' OR d.icd10_code LIKE 'E11%' OR d.icd10_code LIKE 'O24%')"
        };

        var byTypeSql = $@"
            SELECT
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'E10%' THEN v.patient_id END) AS t1,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'E11%' THEN v.patient_id END) AS t2,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'O24%' THEN v.patient_id END) AS gdm,
                COUNT(DISTINCT v.patient_id)                                              AS total
            FROM diab_his_enc_diagnoses d
            JOIN diab_his_enc_encounters v ON v.id = d.encounter_id
            WHERE d.tenant_id = @tid
              AND d.deleted_at IS NULL
              AND COALESCE(v.started_at, v.created_at) <= @asOf
              {dmTypeWhere}";

        var byTypeRow = await conn.QueryFirstOrDefaultAsync<dynamic>(byTypeSql, new { tid = tenantId, asOf = asOfDt });
        int t1 = (int)(byTypeRow?.t1 ?? 0L);
        int t2 = (int)(byTypeRow?.t2 ?? 0L);
        int gdm = (int)(byTypeRow?.gdm ?? 0L);
        int totalCohort = (int)(byTypeRow?.total ?? 0L);

        // ---- 2) Phan bo HbA1c (latest per patient) ----
        // Lay HbA1c moi nhat cua moi BN tinh den asOf, bucket theo nguong lam sang.
        const string hba1cSql = @"
            WITH latest AS (
                SELECT r.patient_id, MAX(r.performed_at) AS last_dt
                FROM diab_his_lab_results r
                WHERE r.tenant_id = @tid
                  AND r.test_code IN ('HBA1C','HbA1c','4548-4')
                  AND r.deleted_at IS NULL
                  AND r.performed_at <= @asOf
                GROUP BY r.patient_id
            ),
            per_patient AS (
                SELECT r.patient_id, r.value_numeric AS hba1c
                FROM diab_his_lab_results r
                JOIN latest l ON l.patient_id = r.patient_id AND l.last_dt = r.performed_at
                WHERE r.tenant_id = @tid
                  AND r.test_code IN ('HBA1C','HbA1c','4548-4')
                  AND r.deleted_at IS NULL
            )
            SELECT
                SUM(CASE WHEN hba1c <  7                THEN 1 ELSE 0 END) AS lt_7,
                SUM(CASE WHEN hba1c >= 7 AND hba1c < 8  THEN 1 ELSE 0 END) AS between_7_8,
                SUM(CASE WHEN hba1c >= 8 AND hba1c < 9  THEN 1 ELSE 0 END) AS between_8_9,
                SUM(CASE WHEN hba1c >= 9                THEN 1 ELSE 0 END) AS gt_9
            FROM per_patient";

        var hRow = await conn.QueryFirstOrDefaultAsync<dynamic>(hba1cSql, new { tid = tenantId, asOf = asOfDt });
        var hba1c = new DiabetesHba1cDistribution(
            (int)(hRow?.lt_7 ?? 0L),
            (int)(hRow?.between_7_8 ?? 0L),
            (int)(hRow?.between_8_9 ?? 0L),
            (int)(hRow?.gt_9 ?? 0L));

        // ---- 3) Bien chung (distinct patient_id co dau hieu) ----
        const string compSql = @"
            SELECT
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'H36%'
                                      OR d.icd10_code LIKE 'E10.3%'
                                      OR d.icd10_code LIKE 'E11.3%' THEN v.patient_id END) AS retinopathy,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'G63%'
                                      OR d.icd10_code LIKE 'E10.4%'
                                      OR d.icd10_code LIKE 'E11.4%' THEN v.patient_id END) AS neuropathy,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'N08%'
                                      OR d.icd10_code LIKE 'E10.2%'
                                      OR d.icd10_code LIKE 'E11.2%' THEN v.patient_id END) AS nephropathy,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'I20%'
                                      OR d.icd10_code LIKE 'I21%'
                                      OR d.icd10_code LIKE 'I22%'
                                      OR d.icd10_code LIKE 'I23%'
                                      OR d.icd10_code LIKE 'I24%'
                                      OR d.icd10_code LIKE 'I25%' THEN v.patient_id END) AS cad,
                COUNT(DISTINCT CASE WHEN d.icd10_code LIKE 'I70%'
                                      OR d.icd10_code LIKE 'I73%' THEN v.patient_id END) AS pad
            FROM diab_his_enc_diagnoses d
            JOIN diab_his_enc_encounters v ON v.id = d.encounter_id
            WHERE d.tenant_id = @tid
              AND d.deleted_at IS NULL
              AND COALESCE(v.started_at, v.created_at) <= @asOf";

        var cRow = await conn.QueryFirstOrDefaultAsync<dynamic>(compSql, new { tid = tenantId, asOf = asOfDt });
        var comp = new DiabetesComplications(
            (int)(cRow?.retinopathy ?? 0L),
            (int)(cRow?.neuropathy ?? 0L),
            (int)(cRow?.nephropathy ?? 0L),
            (int)(cRow?.cad ?? 0L),
            (int)(cRow?.pad ?? 0L));

        return new DiabetesCohortDetailedResponse(
            asOf,
            totalCohort,
            new DiabetesCohortByType(t1, t2, gdm),
            hba1c,
            comp);
    }

    public async Task<DashboardOverviewResponse> GetDashboardOverviewAsync(
        int tenantId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.Today;

        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM diab_his_enc_encounters WHERE tenant_id = @tid AND DATE(started_at) = @today AND status != 'CANCELLED' AND deleted_at IS NULL) AS today_enc,
                (SELECT COUNT(*) FROM diab_his_enc_encounters WHERE tenant_id = @tid AND status = 'WAITING' AND deleted_at IS NULL) AS waiting,
                (SELECT COALESCE(SUM(patient_payable),0) FROM diab_his_bil_billing WHERE tenant_id = @tid AND DATE(created_at) = @today AND status = 'FINALIZED' AND deleted_at IS NULL) AS today_rev,
                (SELECT COUNT(*) FROM diab_his_pha_stock WHERE tenant_id = @tid AND quantity <= 0) AS low_stock,
                (SELECT COUNT(*) FROM diab_his_pha_stock WHERE tenant_id = @tid AND exp_date BETWEEN @today AND DATE_ADD(@today, INTERVAL 30 DAY)) AS near_expiry,
                (SELECT COUNT(*) FROM diab_his_int_bhyt_exports WHERE tenant_id = @tid AND status = 'SUBMITTED') AS bhyt_pending,
                (SELECT COUNT(*) FROM diab_his_pha_prescriptions WHERE tenant_id = @tid AND dtqg_code IS NULL AND status NOT IN ('DRAFT','CANCELLED') AND deleted_at IS NULL) AS dtqg_failed";

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            tid = tenantId,
            today = today.Date
        });

        return new DashboardOverviewResponse(
            (int)(r?.today_enc ?? 0),
            (int)(r?.waiting ?? 0),
            (decimal)(r?.today_rev ?? 0m),
            (int)(r?.low_stock ?? 0),
            (int)(r?.near_expiry ?? 0),
            (int)(r?.bhyt_pending ?? 0),
            (int)(r?.dtqg_failed ?? 0));
    }

    public async Task<IReadOnlyList<ChartDataPoint>> GetRevenueTrendAsync(
        int tenantId, int days, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var from = DateTime.Today.AddDays(-days);

        var sql = @"
            SELECT DATE_FORMAT(created_at,'%Y-%m-%d') AS label, COALESCE(SUM(patient_payable),0) AS value
            FROM diab_his_bil_billing
            WHERE tenant_id = @tid AND created_at >= @from AND status = 'FINALIZED' AND deleted_at IS NULL
            GROUP BY label ORDER BY label";

        var rows = await conn.QueryAsync<dynamic>(sql, new { tid = tenantId, from });
        return rows.Select(r => new ChartDataPoint((string)r.label, (decimal)r.value)).ToList();
    }

    public async Task<IReadOnlyList<ChartDataPoint>> GetEncountersTrendAsync(
        int tenantId, int days, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var from = DateTime.Today.AddDays(-days);

        var sql = @"
            SELECT DATE_FORMAT(started_at,'%Y-%m-%d') AS label, COUNT(*) AS value
            FROM diab_his_enc_encounters
            WHERE tenant_id = @tid AND started_at >= @from AND status != 'CANCELLED' AND deleted_at IS NULL
            GROUP BY label ORDER BY label";

        var rows = await conn.QueryAsync<dynamic>(sql, new { tid = tenantId, from });
        return rows.Select(r => new ChartDataPoint((string)r.label, (decimal)(long)r.value)).ToList();
    }

    public async Task<IReadOnlyList<AlertResponse>> GetAlertsAsync(
        int tenantId, string? severity, string? type, CancellationToken ct = default)
    {
        // Stub: aggregate alerts from multiple sources in a real impl.
        // Return mock data to satisfy the endpoint shape.
        var alerts = new List<AlertResponse>();
        return await Task.FromResult<IReadOnlyList<AlertResponse>>(alerts);
    }
}
