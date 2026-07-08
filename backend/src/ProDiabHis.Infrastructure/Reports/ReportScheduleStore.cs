using System.Data;
using System.Text.Json;
using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// CRUD tenant-scoped + truy van "den han" cho lich gui bao cao qua email (diab_his_rep_schedules).
/// Moi truy van CRUD BAT BUOC co WHERE tenant_id = @tenantId; GetDueAsync (dung boi Hangfire job)
/// KHONG loc tenant — quet toan he thong theo thiet ke (job chay nen, khong co context request).
/// </summary>
public class ReportScheduleStore : IReportScheduleStore
{
    private readonly IDapperConnectionFactory _db;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Ep CAST(... AS SIGNED) cho cot TINYINT (hour/day_of_week/day_of_month) — MySqlConnector tra ve TINYINT
    // nhu System.SByte, khong khop constructor record (int/int?) khien Dapper khong tim duoc ham khoi tao
    // phu hop (InvalidOperationException "materialization"). CAST buoc MySQL tra ve INT (Int32) chuan.
    private const string SelectColumns =
        @"id, tenant_id AS TenantId, report_code AS ReportCode, title, frequency,
          CAST(hour AS SIGNED) AS hour,
          CAST(day_of_week AS SIGNED) AS DayOfWeek, CAST(day_of_month AS SIGNED) AS DayOfMonth,
          period, format, recipients_json AS RecipientsJson, enabled, last_run_at AS LastRunAt,
          created_by AS CreatedBy, created_at AS CreatedAt, updated_by AS UpdatedBy, updated_at AS UpdatedAt";

    public ReportScheduleStore(IDapperConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<ReportSchedule>> GetAllAsync(int tenantId, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var rows = await conn.QueryAsync<RepScheduleRow>(new CommandDefinition(
            $@"SELECT {SelectColumns} FROM diab_his_rep_schedules
               WHERE tenant_id = @tenantId AND deleted_at IS NULL
               ORDER BY created_at DESC",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(Map).ToList();
    }

    public async Task<ReportSchedule?> GetByIdAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RepScheduleRow>(new CommandDefinition(
            $@"SELECT {SelectColumns} FROM diab_his_rep_schedules
               WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL
               LIMIT 1",
            new { tenantId, id }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<ReportSchedule> CreateAsync(int tenantId, string createdBy, ReportScheduleInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        var id = Guid.NewGuid().ToString();

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO diab_his_rep_schedules
                (id, tenant_id, report_code, title, frequency, hour, day_of_week, day_of_month, period, format,
                 recipients_json, enabled, created_by, created_at, updated_by, updated_at)
              VALUES
                (@id, @tenantId, @reportCode, @title, @frequency, @hour, @dayOfWeek, @dayOfMonth, @period, @format,
                 CAST(@recipientsJson AS JSON), @enabled, @createdBy, NOW(), @createdBy, NOW())",
            new
            {
                id,
                tenantId,
                reportCode = input.ReportCode,
                title = input.Title,
                frequency = ReportScheduleCodes.ToCode(input.Frequency),
                hour = input.Hour,
                dayOfWeek = input.DayOfWeek,
                dayOfMonth = input.DayOfMonth,
                period = ReportScheduleCodes.ToCode(input.Period),
                format = ReportScheduleCodes.ToCode(input.Format),
                recipientsJson = JsonSerializer.Serialize(input.Recipients, JsonOpts),
                enabled = input.Enabled,
                createdBy
            }, cancellationToken: ct));

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task<ReportSchedule> UpdateAsync(int tenantId, string id, string updatedBy, ReportScheduleInput input, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE diab_his_rep_schedules
                 SET report_code = @reportCode, title = @title, frequency = @frequency, hour = @hour,
                     day_of_week = @dayOfWeek, day_of_month = @dayOfMonth, period = @period, format = @format,
                     recipients_json = CAST(@recipientsJson AS JSON), enabled = @enabled,
                     updated_by = @updatedBy, updated_at = NOW()
               WHERE tenant_id = @tenantId AND id = @id AND deleted_at IS NULL",
            new
            {
                tenantId,
                id,
                reportCode = input.ReportCode,
                title = input.Title,
                frequency = ReportScheduleCodes.ToCode(input.Frequency),
                hour = input.Hour,
                dayOfWeek = input.DayOfWeek,
                dayOfMonth = input.DayOfMonth,
                period = ReportScheduleCodes.ToCode(input.Period),
                format = ReportScheduleCodes.ToCode(input.Format),
                recipientsJson = JsonSerializer.Serialize(input.Recipients, JsonOpts),
                enabled = input.Enabled,
                updatedBy
            }, cancellationToken: ct));

        if (affected == 0)
            throw new ReportValidationException("REPORT_SCHEDULE_NOT_FOUND", "Không tìm thấy lịch gửi báo cáo");

        return (await GetByIdAsync(tenantId, id, ct))!;
    }

    public async Task DeleteAsync(int tenantId, string id, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE diab_his_rep_schedules SET deleted_at = NOW(), enabled = 0 WHERE tenant_id = @tenantId AND id = @id",
            new { tenantId, id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ReportSchedule>> GetDueAsync(DateTime nowUtc, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();

        var hour = nowUtc.Hour;
        var dow = (int)nowUtc.DayOfWeek; // 0=CN..6=T7, khop enum DayOfWeek cua .NET
        var dom = nowUtc.Day;
        var today = nowUtc.Date;

        var rows = await conn.QueryAsync<RepScheduleRow>(new CommandDefinition(
            $@"SELECT {SelectColumns} FROM diab_his_rep_schedules
               WHERE enabled = 1 AND deleted_at IS NULL
                 AND hour = @hour
                 AND (
                       frequency = 'DAILY'
                    OR (frequency = 'WEEKLY' AND day_of_week = @dow)
                    OR (frequency = 'MONTHLY' AND day_of_month = @dom)
                 )
                 AND (last_run_at IS NULL OR DATE(last_run_at) < @today)",
            new { hour, dow, dom, today }, cancellationToken: ct));

        return rows.Select(Map).ToList();
    }

    public async Task MarkRunAsync(string id, DateTime ranAtUtc, CancellationToken ct)
    {
        using var conn = (IDbConnection)_db.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE diab_his_rep_schedules SET last_run_at = @ranAtUtc WHERE id = @id",
            new { id, ranAtUtc }, cancellationToken: ct));
    }

    // ---- Mapping ---- //

    private static ReportSchedule Map(RepScheduleRow row) => new(
        row.Id, row.TenantId, row.ReportCode, row.Title,
        ReportScheduleCodes.FrequencyFromCode(row.Frequency), (int)row.Hour, (int?)row.DayOfWeek, (int?)row.DayOfMonth,
        ReportScheduleCodes.PeriodFromCode(row.Period), ReportScheduleCodes.FormatFromCode(row.Format),
        DeserializeRecipients(row.RecipientsJson), row.Enabled, row.LastRunAt,
        row.CreatedBy, row.CreatedAt, row.UpdatedBy, row.UpdatedAt);

    private static IReadOnlyList<string> DeserializeRecipients(string json)
        => JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? new List<string>();

    // Hour/DayOfWeek/DayOfMonth khai bao long/long? — khop kieu BIGINT ma MySQL CAST(... AS SIGNED) tra ve
    // qua MySqlConnector (khong the dung int/int? truc tiep vi cot goc la TINYINT -> SByte, khong khop
    // constructor record ma Dapper can de materialize; CAST ep ve 1 kieu on dinh, tranh phu thuoc driver).
    private record RepScheduleRow(
        string Id, int TenantId, string ReportCode, string Title, string Frequency, long Hour,
        long? DayOfWeek, long? DayOfMonth, string Period, string Format, string RecipientsJson, bool Enabled,
        DateTime? LastRunAt, string? CreatedBy, DateTime CreatedAt, string? UpdatedBy, DateTime UpdatedAt);
}
