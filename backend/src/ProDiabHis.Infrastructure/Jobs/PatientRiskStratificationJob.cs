using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — tinh phan tang nguy co (risk stratification) cho toan bo
/// benh nhan co chan doan DTD (E10-E14) hoac co assessment DTD, UPSERT vao
/// diab_his_cli_patient_risk_flag. Cron: 0 3 * * * (03:00 hang dem).
/// </summary>
public class PatientRiskStratificationJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<PatientRiskStratificationJob> _logger;

    public PatientRiskStratificationJob(IDapperConnectionFactory db, ILogger<PatientRiskStratificationJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bat dau phan tang nguy co benh nhan DTD...");

        using var conn = (IDbConnection)_db.CreateConnection();

        var tenants = (await conn.QueryAsync<int>(
            "SELECT id FROM diab_his_sys_tenants WHERE status = 'ACTIVE' AND deleted_at IS NULL")).ToList();

        foreach (var tenantId in tenants)
        {
            try
            {
                await ProcessTenantAsync(conn, tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi phan tang nguy co cho tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Hoan thanh phan tang nguy co benh nhan DTD.");
    }

    private async Task ProcessTenantAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        // Cohort: benh nhan co chan doan E10-E14 (encounter) HOAC co assessment DTD
        var patientIds = (await conn.QueryAsync<string>(
            @"SELECT DISTINCT patient_id FROM (
                  SELECT e.patient_id
                  FROM diab_his_enc_diagnoses d
                  JOIN diab_his_enc_encounters e ON e.id = d.encounter_id
                  WHERE d.tenant_id = @tenantId AND d.deleted_at IS NULL
                    AND (d.icd10_code LIKE 'E10%' OR d.icd10_code LIKE 'E11%'
                         OR d.icd10_code LIKE 'E12%' OR d.icd10_code LIKE 'E13%' OR d.icd10_code LIKE 'E14%')
                  UNION
                  SELECT a.patient_id
                  FROM diab_his_cli_diabetes_assessments a
                  WHERE a.tenant_id = @tenantId AND a.deleted_at IS NULL
              ) x", new { tenantId })).ToList();

        if (patientIds.Count == 0) return;

        var targets = await GetTargetsAsync(conn, tenantId, ct);
        var hba1cTarget = targets.TryGetValue("HBA1C", out var ht) ? ht : 7.0m;
        var visitIntervalDays = targets.TryGetValue("VISIT_INTERVAL_DAYS", out var vi) ? (int)vi : 90;

        foreach (var patientId in patientIds)
        {
            var assessments = (await conn.QueryAsync<(DateTime AssessedAt, decimal? Hba1c, decimal? Egfr, int? BpSys, int? BpDia)>(
                @"SELECT assessed_at AS AssessedAt, hba1c AS Hba1c, egfr AS Egfr, bp_systolic AS BpSys, bp_diastolic AS BpDia
                  FROM diab_his_cli_diabetes_assessments
                  WHERE tenant_id = @tenantId AND patient_id = @patientId AND deleted_at IS NULL
                  ORDER BY assessed_at DESC LIMIT 6",
                new { tenantId, patientId })).ToList();

            var lastVisitAt = await conn.ExecuteScalarAsync<DateTime?>(
                @"SELECT MAX(COALESCE(started_at, created_at)) FROM diab_his_enc_encounters
                  WHERE tenant_id = @tenantId AND patient_id = @patientId AND deleted_at IS NULL",
                new { tenantId, patientId });

            decimal? latestHba1c = assessments.Count > 0 ? assessments[0].Hba1c : null;
            decimal? latestEgfr = assessments.Count > 0 ? assessments[0].Egfr : null;
            int? latestBpSys = assessments.Count > 0 ? assessments[0].BpSys : null;
            int? latestBpDia = assessments.Count > 0 ? assessments[0].BpDia : null;
            var firstWithHba1c = assessments.FirstOrDefault(a => a.Hba1c.HasValue);
            DateTime? lastHba1cAt = firstWithHba1c.Hba1c.HasValue ? firstWithHba1c.AssessedAt : null;

            var points = assessments
                .Select(a => new DiabetesTrendCalculator.AssessmentPoint(a.AssessedAt, a.Hba1c, a.BpSys, a.BpDia))
                .ToList();
            var flags = DiabetesTrendCalculator.DetectDeterioration(points, hba1cTarget, 130, 80);
            var risingTrend = flags.Any(f => f.Code == "HBA1C_RISING");

            var trend = "STABLE";
            var withHba1c = points.Where(p => p.Hba1c.HasValue).OrderBy(p => p.AssessedAt).ToList();
            if (withHba1c.Count >= 2)
            {
                var diff = withHba1c[^1].Hba1c!.Value - withHba1c[^2].Hba1c!.Value;
                trend = diff >= 0.5m ? "RISING" : diff <= -0.5m ? "FALLING" : "STABLE";
            }

            var overdueVisit = lastVisitAt.HasValue && (DateTime.UtcNow - lastVisitAt.Value).TotalDays > visitIntervalDays;

            var score = DiabetesTrendCalculator.ComputeRiskScore(latestHba1c, latestEgfr, latestBpSys, risingTrend, overdueVisit);
            var riskLevel = DiabetesTrendCalculator.ClassifyRisk(score);

            var reasons = flags.Select(f => f.Message).ToList();
            if (overdueVisit) reasons.Add("Quá hạn tái khám theo phác đồ");

            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_cli_patient_risk_flag
                    (id, tenant_id, patient_id, risk_level, risk_score, reasons_json,
                     latest_hba1c, latest_egfr, latest_bp_sys, latest_bp_dia, hba1c_trend,
                     last_visit_at, last_hba1c_at, computed_at)
                  VALUES
                    (UUID(), @tenantId, @patientId, @riskLevel, @score, @reasonsJson,
                     @latestHba1c, @latestEgfr, @latestBpSys, @latestBpDia, @trend,
                     @lastVisitAt, @lastHba1cAt, NOW(3))
                  ON DUPLICATE KEY UPDATE
                    risk_level = VALUES(risk_level), risk_score = VALUES(risk_score),
                    reasons_json = VALUES(reasons_json), latest_hba1c = VALUES(latest_hba1c),
                    latest_egfr = VALUES(latest_egfr), latest_bp_sys = VALUES(latest_bp_sys),
                    latest_bp_dia = VALUES(latest_bp_dia), hba1c_trend = VALUES(hba1c_trend),
                    last_visit_at = VALUES(last_visit_at), last_hba1c_at = VALUES(last_hba1c_at),
                    computed_at = VALUES(computed_at)",
                new
                {
                    tenantId, patientId, riskLevel, score,
                    reasonsJson = System.Text.Json.JsonSerializer.Serialize(reasons),
                    latestHba1c, latestEgfr, latestBpSys, latestBpDia, trend,
                    lastVisitAt, lastHba1cAt
                });
        }
    }

    internal static async Task<Dictionary<string, decimal>> GetTargetsAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var rows = await conn.QueryAsync<(string Param, decimal TargetValue, int? TenantId)>(
            @"SELECT param AS Param, target_value AS TargetValue, tenant_id AS TenantId
              FROM diab_his_cli_care_pathway_target
              WHERE code = 'DM_T2_5481' AND (tenant_id = @tenantId OR tenant_id IS NULL)
              ORDER BY (tenant_id IS NULL) ASC", new { tenantId });

        var dict = new Dictionary<string, decimal>();
        foreach (var r in rows)
        {
            if (!dict.ContainsKey(r.Param)) dict[r.Param] = r.TargetValue;
        }
        return dict;
    }
}
