using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Domain.Entities;

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

    // ================= F7: Report endpoint con stub — bo sung service that ================= //

    public async Task<IReadOnlyList<ServiceRevenueResponse>> GetRevenueByServiceAsync(
        int tenantId, DateOnly from, DateOnly to, int top,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue);

        // Khong tinh item_type = DRUG vi da co bao cao rieng pharmacy/top-drugs
        var sql = @"
            SELECT
                bi.code             AS ServiceCode,
                bi.name             AS ServiceName,
                bi.item_type        AS ItemType,
                COUNT(*)            AS Cnt,
                SUM(bi.line_total)  AS TotalRevenue
            FROM diab_his_bil_billing_items bi
            JOIN diab_his_bil_billing b ON b.id = bi.billing_id
            WHERE b.tenant_id = @tenantId
              AND b.deleted_at IS NULL
              AND b.status = 'FINALIZED'
              AND bi.item_type <> 'DRUG'
              AND b.created_at BETWEEN @from AND @to
            GROUP BY COALESCE(bi.code, bi.name), bi.code, bi.name, bi.item_type
            ORDER BY TotalRevenue DESC
            LIMIT @top";

        var rows = (await conn.QueryAsync<dynamic>(sql, new { tenantId, from = fromDt, to = toDt, top })).ToList();

        var grandTotal = await conn.ExecuteScalarAsync<decimal?>(
            @"SELECT COALESCE(SUM(bi.line_total), 0)
              FROM diab_his_bil_billing_items bi
              JOIN diab_his_bil_billing b ON b.id = bi.billing_id
              WHERE b.tenant_id = @tenantId
                AND b.deleted_at IS NULL
                AND b.status = 'FINALIZED'
                AND bi.item_type <> 'DRUG'
                AND b.created_at BETWEEN @from AND @to",
            new { tenantId, from = fromDt, to = toDt }) ?? 0m;

        return rows.Select(r =>
        {
            decimal revenue = (decimal)(r.TotalRevenue ?? 0m);
            var pct = grandTotal > 0 ? Math.Round(revenue / grandTotal * 100, 1) : 0m;
            return new ServiceRevenueResponse(
                (string?)r.ServiceCode,
                (string)r.ServiceName,
                (string)r.ItemType,
                (int)(long)r.Cnt,
                revenue,
                pct);
        }).ToList();
    }

    public async Task<IReadOnlyList<PaymentMethodBreakdownResponse>> GetRevenueByPaymentMethodAsync(
        int tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue);

        var rows = await conn.QueryAsync<(string Method, decimal Amount, string Status)>(
            @"SELECT method, amount, status
              FROM diab_his_bil_payments
              WHERE tenant_id = @tenantId
                AND COALESCE(paid_at, created_at) BETWEEN @from AND @to",
            new { tenantId, from = fromDt, to = toDt });

        return PaymentBreakdownCalculator.CalculateBreakdown(rows);
    }

    public async Task<CashierDailySummaryResponse> GetCashierDailySummaryAsync(
        int tenantId, DateOnly date, Guid? cashierId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd   = date.ToDateTime(TimeOnly.MaxValue);
        var cashierIdStr = cashierId?.ToString();

        var payments = (await conn.QueryAsync<(string Method, decimal Amount, string Status)>(
            @"SELECT method, amount, status
              FROM diab_his_bil_payments
              WHERE tenant_id = @tenantId
                AND COALESCE(paid_at, created_at) BETWEEN @dayStart AND @dayEnd
                AND (@cashierId IS NULL OR paid_by = @cashierId)",
            new { tenantId, dayStart, dayEnd, cashierId = cashierIdStr })).ToList();

        var (totalRevenue, _, totalRefunds) = PaymentBreakdownCalculator.Summarize(payments);
        var byMethod = PaymentBreakdownCalculator.CalculateBreakdown(payments);

        var totalInvoices = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT billing_id)
              FROM diab_his_bil_payments
              WHERE tenant_id = @tenantId
                AND status = @completed
                AND COALESCE(paid_at, created_at) BETWEEN @dayStart AND @dayEnd
                AND (@cashierId IS NULL OR paid_by = @cashierId)",
            new { tenantId, completed = PaymentStatus.Completed, dayStart, dayEnd, cashierId = cashierIdStr });

        var shiftAgg = await conn.QueryFirstOrDefaultAsync<(decimal? Opening, decimal? Closing)>(
            @"SELECT SUM(opening_balance) AS opening, SUM(closing_balance) AS closing
              FROM diab_his_bil_cashier_shifts
              WHERE tenant_id = @tenantId
                AND shift_date = @shiftDate
                AND (@cashierId IS NULL OR cashier_user_id = @cashierId)",
            // MySqlConnector khong bind duoc DateOnly -> truyen chuoi yyyy-MM-dd (khop cot DATE)
            new { tenantId, shiftDate = date.ToString("yyyy-MM-dd"), cashierId = cashierIdStr });

        return new CashierDailySummaryResponse(
            date,
            totalRevenue,
            totalInvoices,
            totalRefunds,
            byMethod,
            shiftAgg.Opening ?? 0m,
            shiftAgg.Closing ?? 0m);
    }

    public async Task<DebtsAgingResponse> GetDebtsAgingAsync(
        int tenantId, DateOnly asOf, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var asOfEnd = asOf.ToDateTime(TimeOnly.MaxValue);
        var asOfDate = asOf.ToDateTime(TimeOnly.MinValue);

        // Cong no = hoa don da chot (FINALIZED/PARTIAL_PAID) con balance > 0.
        // LIMIT 500 — cung mot quy uoc voi GetReportPdfHandler (bao cao chi tiet, khong phai so lieu ke toan chinh xac tuyet doi).
        var rows = (await conn.QueryAsync<(string Id, string? BillNo, string PatientId, decimal Balance, DateTime CreatedAt, DateTime? PaymentDueDate)>(
            @"SELECT b.id, b.bill_no, b.patient_id, b.balance, b.created_at, b.payment_due_date
              FROM diab_his_bil_billing b
              WHERE b.tenant_id = @tenantId
                AND b.deleted_at IS NULL
                AND b.status IN ('FINALIZED','PARTIAL_PAID')
                AND b.balance > 0
                AND b.created_at <= @asOfEnd
              ORDER BY b.balance DESC
              LIMIT 500",
            new { tenantId, asOfEnd })).ToList();

        if (rows.Count == 0)
            return new DebtsAgingResponse(0, 0, 0, 0, 0, Array.Empty<DebtDetailItem>());

        var patientIds = rows.Select(r => r.PatientId).Distinct().ToList();
        var patientNames = new Dictionary<string, string>();
        var names = await conn.QueryAsync<(string Id, string FullName)>(
            "SELECT id, full_name FROM diab_his_pat_patients WHERE tenant_id = @tenantId AND id IN @ids AND deleted_at IS NULL",
            new { tenantId, ids = patientIds });
        foreach (var (id, fn) in names) patientNames[id] = fn;

        var details = rows.Select(r =>
        {
            var days = (int)(asOfDate.Date - r.CreatedAt.Date).TotalDays;
            if (days < 0) days = 0;
            return new DebtDetailItem(
                r.BillNo ?? $"HD-{r.Id[..8]}",
                patientNames.TryGetValue(r.PatientId, out var pn) ? pn : "—",
                r.Balance,
                days,
                r.PaymentDueDate.HasValue ? DateOnly.FromDateTime(r.PaymentDueDate.Value) : null);
        }).ToList();

        return DebtAgingCalculator.Calculate(details);
    }

    public async Task<BhytSummaryResponse> GetBhytSummaryAsync(
        int tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var fromPeriod = from.ToString("yyyy-MM");
        var toPeriod   = to.ToString("yyyy-MM");

        // Nguon du lieu: diab_his_bhyt_exports (co cot that: export_period, total_records,
        // total_amount, status). Bang diab_his_int_bhyt_exports KHONG co cot so lieu (chi
        // luu file/trang thai) nen truoc day query bi loi Unknown column 'encounter_count'.
        var sql = @"
            SELECT
                COALESCE(SUM(total_records), 0)                                              AS total_cards,
                COALESCE(SUM(total_amount), 0)                                               AS claimed,
                COALESCE(SUM(CASE WHEN status = 'APPROVED' THEN total_amount ELSE 0 END), 0) AS paid,
                COALESCE(SUM(CASE WHEN status = 'REJECTED' THEN total_amount ELSE 0 END), 0) AS rejected,
                COALESCE(SUM(CASE WHEN status NOT IN ('APPROVED','REJECTED') THEN 1 ELSE 0 END), 0) AS pending
            FROM diab_his_bhyt_exports
            WHERE tenant_id = @tenantId
              AND deleted_at IS NULL
              AND export_period BETWEEN @fromPeriod AND @toPeriod";

        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { tenantId, fromPeriod, toPeriod });

        decimal claimed  = (decimal)(r?.claimed ?? 0m);
        decimal paid     = (decimal)(r?.paid ?? 0m);
        decimal rejected = (decimal)(r?.rejected ?? 0m);
        int totalCards   = Convert.ToInt32(r?.total_cards ?? 0);
        int pending      = Convert.ToInt32(r?.pending ?? 0);
        var rejectionRate = claimed > 0 ? Math.Round(rejected / claimed * 100, 1) : 0m;

        return new BhytSummaryResponse(totalCards, claimed, paid, rejected, rejectionRate, pending);
    }

    public async Task<InventoryValueResponse> GetInventoryValueAsync(
        int tenantId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // Gia tri ton kho = so luong con lai * gia nhap tung lo (import_price tren diab_his_pha_stock),
        // chi tinh thuoc con han (giong dieu kien dung trong GetReportPdfHandler.Pharmacy).
        var sql = @"
            SELECT
                COALESCE(NULLIF(TRIM(d.drug_category), ''), 'Khác') AS category,
                SUM(s.quantity * s.import_price)                    AS value,
                COUNT(DISTINCT d.id)                                AS sku_count
            FROM diab_his_pha_stock s
            JOIN diab_his_pha_drugs d ON d.id = s.drug_id
            WHERE d.tenant_id = @tenantId
              AND s.tenant_id = @tenantId
              AND d.deleted_at IS NULL
              AND d.is_active = 1
              AND s.quantity > 0
              AND (s.exp_date IS NULL OR s.exp_date >= CURDATE())
            GROUP BY category
            ORDER BY value DESC";

        var rows = (await conn.QueryAsync<(string Category, decimal Value, int SkuCount)>(sql, new { tenantId })).ToList();
        var byCategory = rows.Select(r => new InventoryCategoryItem(r.Category, r.Value, r.SkuCount)).ToList();
        var totalValue = byCategory.Sum(x => x.Value);

        var totalSkus = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT d.id)
              FROM diab_his_pha_stock s
              JOIN diab_his_pha_drugs d ON d.id = s.drug_id
              WHERE d.tenant_id = @tenantId
                AND s.tenant_id = @tenantId
                AND d.deleted_at IS NULL
                AND d.is_active = 1
                AND s.quantity > 0
                AND (s.exp_date IS NULL OR s.exp_date >= CURDATE())",
            new { tenantId });

        return new InventoryValueResponse(totalValue, totalSkus, byCategory);
    }
}
