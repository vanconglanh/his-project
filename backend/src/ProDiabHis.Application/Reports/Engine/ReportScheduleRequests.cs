using System.Text.RegularExpressions;

namespace ProDiabHis.Application.Reports.Engine;

// ---- Request/Command cho CRUD Lich gui bao cao qua email (Report Builder P3.3) ---- //

public record SaveReportScheduleRequest(
    string ReportCode,
    string Title,
    string Frequency,
    int Hour,
    int? DayOfWeek,
    int? DayOfMonth,
    string Period,
    string Format,
    List<string> Recipients,
    bool Enabled = true);

/// <summary>Chuyen doi request DTO (Api) -> ReportScheduleInput (Application) + validate tap trung
/// (hour/day_of_week/day_of_month/recipients) — nem REPORT_SCHEDULE_INVALID (400) neu sai.</summary>
public static class ReportScheduleRequestMapper
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ReportScheduleInput ToInput(SaveReportScheduleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ReportCode))
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Phải chọn báo cáo cho lịch gửi");

        if (string.IsNullOrWhiteSpace(req.Title))
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Tên lịch gửi không được để trống");

        if (req.Hour is < 0 or > 23)
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Giờ chạy phải trong khoảng 0-23");

        var frequency = ReportScheduleCodes.FrequencyFromCode(req.Frequency);

        int? dayOfWeek = null;
        int? dayOfMonth = null;

        if (frequency == ReportScheduleFrequency.Weekly)
        {
            if (req.DayOfWeek is null or < 0 or > 6)
                throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Lịch tuần (WEEKLY) phải chỉ định day_of_week (0=CN..6=T7)");
            dayOfWeek = req.DayOfWeek;
        }

        if (frequency == ReportScheduleFrequency.Monthly)
        {
            if (req.DayOfMonth is null or < 1 or > 28)
                throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Lịch tháng (MONTHLY) phải chỉ định day_of_month (1-28)");
            dayOfMonth = req.DayOfMonth;
        }

        var period = ReportScheduleCodes.PeriodFromCode(req.Period);
        var format = ReportScheduleCodes.FormatFromCode(req.Format);

        var recipients = (req.Recipients ?? new())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", "Phải có ít nhất 1 người nhận (recipients)");

        const int maxRecipients = 20;
        if (recipients.Count > maxRecipients)
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Số người nhận vượt giới hạn cho phép ({maxRecipients})");

        var invalid = recipients.FirstOrDefault(r => !EmailRegex.IsMatch(r));
        if (invalid is not null)
            throw new ReportValidationException("REPORT_SCHEDULE_INVALID", $"Địa chỉ email không hợp lệ: '{invalid}'");

        return new ReportScheduleInput(
            req.ReportCode.Trim(), req.Title.Trim(), frequency, req.Hour, dayOfWeek, dayOfMonth,
            period, format, recipients, req.Enabled);
    }
}
