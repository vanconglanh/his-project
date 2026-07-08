using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job — quet cohort benh man tinh (E10-E14) qua han tai kham
/// hoac qua han do HbA1c (theo phac do QD 5481) -> tao recall PENDING (idempotent).
/// Cron: 30 3 * * * (03:30 hang dem, sau PatientRiskStratificationJob).
/// </summary>
public class ChronicCareRecallJob
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<ChronicCareRecallJob> _logger;

    public ChronicCareRecallJob(IDapperConnectionFactory db, ILogger<ChronicCareRecallJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bat dau quet recall tai kham chu dong...");

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
                _logger.LogError(ex, "Loi quet recall cho tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Hoan thanh quet recall tai kham chu dong.");
    }

    private async Task ProcessTenantAsync(IDbConnection conn, int tenantId, CancellationToken ct)
    {
        var targets = await PatientRiskStratificationJob.GetTargetsAsync(conn, tenantId, ct);
        var visitIntervalDays = targets.TryGetValue("VISIT_INTERVAL_DAYS", out var vi) ? (int)vi : 90;
        var hba1cIntervalDays = targets.TryGetValue("HBA1C_INTERVAL_DAYS", out var hi) ? (int)hi : 90;

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

        var now = DateTime.UtcNow;

        foreach (var patientId in patientIds)
        {
            var lastVisitAt = await conn.ExecuteScalarAsync<DateTime?>(
                @"SELECT MAX(COALESCE(started_at, created_at)) FROM diab_his_enc_encounters
                  WHERE tenant_id = @tenantId AND patient_id = @patientId AND deleted_at IS NULL",
                new { tenantId, patientId });

            var lastHba1cAt = await conn.ExecuteScalarAsync<DateTime?>(
                @"SELECT MAX(assessed_at) FROM diab_his_cli_diabetes_assessments
                  WHERE tenant_id = @tenantId AND patient_id = @patientId AND hba1c IS NOT NULL AND deleted_at IS NULL",
                new { tenantId, patientId });

            var overdueVisit = !lastVisitAt.HasValue || (now - lastVisitAt.Value).TotalDays > visitIntervalDays;
            var overdueHba1c = !lastHba1cAt.HasValue || (now - lastHba1cAt.Value).TotalDays > hba1cIntervalDays;

            if (overdueVisit)
                await UpsertRecallAsync(conn, tenantId, patientId, "OVERDUE_VISIT",
                    DateOnly.FromDateTime(now), new { lastVisitAt, visitIntervalDays });

            if (overdueHba1c)
                await UpsertRecallAsync(conn, tenantId, patientId, "OVERDUE_HBA1C",
                    DateOnly.FromDateTime(now), new { lastHba1cAt, hba1cIntervalDays });
        }
    }

    private static async Task UpsertRecallAsync(
        IDbConnection conn, int tenantId, string patientId, string recallType, DateOnly dueDate, object reason)
    {
        var exists = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM diab_his_cli_followup_recall
              WHERE tenant_id = @tenantId AND patient_id = @patientId AND recall_type = @recallType
                AND status = 'PENDING' AND deleted_at IS NULL",
            new { tenantId, patientId, recallType });

        if (exists > 0) return;

        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_cli_followup_recall
                (id, tenant_id, patient_id, recall_type, due_date, reason_json, priority, status, created_at, updated_at)
              VALUES
                (UUID(), @tenantId, @patientId, @recallType, @dueDate, @reasonJson, 'NORMAL', 'PENDING', NOW(3), NOW(3))",
            new
            {
                tenantId, patientId, recallType, dueDate,
                reasonJson = System.Text.Json.JsonSerializer.Serialize(reason)
            });
    }
}
