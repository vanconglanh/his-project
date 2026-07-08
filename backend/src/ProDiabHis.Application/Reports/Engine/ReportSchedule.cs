namespace ProDiabHis.Application.Reports.Engine;

public enum ReportScheduleFrequency { Daily, Weekly, Monthly }

/// <summary>Khoang ngay tuong doi tinh tai thoi diem job chay (Report Builder P3.3).</summary>
public enum ReportSchedulePeriod { Today, Yesterday, ThisWeek, ThisMonth, LastMonth }

public enum ReportScheduleFormat { Pdf, Excel }

/// <summary>DTO 1 lich gui bao cao qua email (map truc tiep den bang diab_his_rep_schedules, tenant-scoped).</summary>
public record ReportSchedule(
    string Id,
    int TenantId,
    string ReportCode,
    string Title,
    ReportScheduleFrequency Frequency,
    int Hour,
    int? DayOfWeek,
    int? DayOfMonth,
    ReportSchedulePeriod Period,
    ReportScheduleFormat Format,
    IReadOnlyList<string> Recipients,
    bool Enabled,
    DateTime? LastRunAt,
    string? CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime UpdatedAt);

/// <summary>Input tao/sua 1 ReportSchedule (chua tenant/code/audit — do handler/store tu gan).</summary>
public record ReportScheduleInput(
    string ReportCode,
    string Title,
    ReportScheduleFrequency Frequency,
    int Hour,
    int? DayOfWeek,
    int? DayOfMonth,
    ReportSchedulePeriod Period,
    ReportScheduleFormat Format,
    IReadOnlyList<string> Recipients,
    bool Enabled);

/// <summary>CRUD tenant-scoped + truy van "den han" cho ReportScheduleDispatchJob (diab_his_rep_schedules).</summary>
public interface IReportScheduleStore
{
    Task<IReadOnlyList<ReportSchedule>> GetAllAsync(int tenantId, CancellationToken ct);

    Task<ReportSchedule?> GetByIdAsync(int tenantId, string id, CancellationToken ct);

    Task<ReportSchedule> CreateAsync(int tenantId, string createdBy, ReportScheduleInput input, CancellationToken ct);

    Task<ReportSchedule> UpdateAsync(int tenantId, string id, string updatedBy, ReportScheduleInput input, CancellationToken ct);

    Task DeleteAsync(int tenantId, string id, CancellationToken ct);

    /// <summary>
    /// Dung boi Hangfire recurring job (khong tenant-scoped — quet toan he thong): tra ve schedule
    /// enabled dang toi han chay tai <paramref name="nowUtc"/> (khop hour/day_of_week/day_of_month theo
    /// frequency) VA chua chay trong ngay hom nay (last_run_at IS NULL hoac khac ngay) — dam bao idempotent
    /// khi job Hangfire chay lai nhieu lan trong cung 1 gio.
    /// </summary>
    Task<IReadOnlyList<ReportSchedule>> GetDueAsync(DateTime nowUtc, CancellationToken ct);

    Task MarkRunAsync(string id, DateTime ranAtUtc, CancellationToken ct);
}

/// <summary>Chuyen doi enum <-> ma chuoi luu DB/JSON cho ReportSchedule — dung chung Api/Infrastructure.</summary>
public static class ReportScheduleCodes
{
    public static string ToCode(ReportScheduleFrequency f) => f switch
    {
        ReportScheduleFrequency.Daily => "DAILY",
        ReportScheduleFrequency.Weekly => "WEEKLY",
        ReportScheduleFrequency.Monthly => "MONTHLY",
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Tần suất '{f}' không hợp lệ")
    };

    public static ReportScheduleFrequency FrequencyFromCode(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "DAILY" => ReportScheduleFrequency.Daily,
        "WEEKLY" => ReportScheduleFrequency.Weekly,
        "MONTHLY" => ReportScheduleFrequency.Monthly,
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Tần suất '{code}' không hợp lệ — chấp nhận DAILY|WEEKLY|MONTHLY")
    };

    public static string ToCode(ReportSchedulePeriod p) => p switch
    {
        ReportSchedulePeriod.Today => "TODAY",
        ReportSchedulePeriod.Yesterday => "YESTERDAY",
        ReportSchedulePeriod.ThisWeek => "THIS_WEEK",
        ReportSchedulePeriod.ThisMonth => "THIS_MONTH",
        ReportSchedulePeriod.LastMonth => "LAST_MONTH",
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Khoảng kỳ '{p}' không hợp lệ")
    };

    public static ReportSchedulePeriod PeriodFromCode(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "TODAY" => ReportSchedulePeriod.Today,
        "YESTERDAY" => ReportSchedulePeriod.Yesterday,
        "THIS_WEEK" => ReportSchedulePeriod.ThisWeek,
        "THIS_MONTH" => ReportSchedulePeriod.ThisMonth,
        "LAST_MONTH" => ReportSchedulePeriod.LastMonth,
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID",
            $"Khoảng kỳ '{code}' không hợp lệ — chấp nhận TODAY|YESTERDAY|THIS_WEEK|THIS_MONTH|LAST_MONTH")
    };

    public static string ToCode(ReportScheduleFormat f) => f switch
    {
        ReportScheduleFormat.Pdf => "PDF",
        ReportScheduleFormat.Excel => "EXCEL",
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Định dạng '{f}' không hợp lệ")
    };

    public static ReportScheduleFormat FormatFromCode(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "PDF" => ReportScheduleFormat.Pdf,
        "EXCEL" => ReportScheduleFormat.Excel,
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Định dạng '{code}' không hợp lệ — chấp nhận PDF|EXCEL")
    };

    /// <summary>Tinh khoang ngay [From, To] tuong doi tu 1 Period, tai thoi diem <paramref name="today"/>.</summary>
    public static (DateOnly From, DateOnly To) ResolveDateRange(ReportSchedulePeriod period, DateOnly today) => period switch
    {
        ReportSchedulePeriod.Today => (today, today),
        ReportSchedulePeriod.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
        ReportSchedulePeriod.ThisWeek => (today.AddDays(-(int)today.DayOfWeek), today),
        ReportSchedulePeriod.ThisMonth => (new DateOnly(today.Year, today.Month, 1), today),
        ReportSchedulePeriod.LastMonth => LastMonthRange(today),
        _ => throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Khoảng kỳ '{period}' không hợp lệ")
    };

    private static (DateOnly From, DateOnly To) LastMonthRange(DateOnly today)
    {
        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
        var lastMonthEnd = firstOfThisMonth.AddDays(-1);
        var lastMonthStart = new DateOnly(lastMonthEnd.Year, lastMonthEnd.Month, 1);
        return (lastMonthStart, lastMonthEnd);
    }
}
