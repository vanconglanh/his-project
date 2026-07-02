using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;    // IApplicationDbContext
using ProDiabHis.Application.Common;  // Result<T>

namespace ProDiabHis.Application.VitalSigns;

/// <summary>
/// Handler cho <see cref="GetVitalSignsTrendQuery"/> — lớp Application/CQRS trong kiến trúc Clean.
///
/// Vai trò: tính thống kê xu hướng (count/min/max/average/latest/first + chuỗi thời gian) cho MỘT chỉ số
/// sinh hiệu của một bệnh nhân trong khoảng ngày. Được gọi bởi VitalSignsController qua MediatR.
///
/// Invariant / ràng buộc:
///  - CHỈ ĐỌC: không ghi DB, không side-effect (không audit như các command).
///  - Đa-tenant + soft-delete tự áp qua GLOBAL QUERY FILTER trên <c>_db.VitalSigns</c> (giống các handler
///    VitalSigns khác) nên KHÔNG lọc tenant thủ công — bản ghi tenant khác/đã xoá mềm tự bị loại.
///  - Mẫu NULL (bản ghi không đo chỉ số đã chọn) bị loại khỏi mọi thống kê lẫn Series.
///  - <c>Metric</c> phải nằm trong whitelist snake_case; ngoài whitelist -> Failure VITAL_TREND_INVALID_METRIC.
/// </summary>
public class GetVitalSignsTrendQueryHandler
    : IRequestHandler<GetVitalSignsTrendQuery, Result<VitalSignsTrendResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetVitalSignsTrendQueryHandler(IApplicationDbContext db) => _db = db;

    /// <summary>
    /// Bảng chọn giá trị theo tên chỉ số (snake_case, khớp quy ước JSON/DB). Đây cũng là WHITELIST hợp lệ.
    ///
    /// Vì sao <c>decimal?</c> là kiểu chung: gộp cột kiểu <c>int?</c> (nhịp tim, huyết áp, SpO2, nhịp thở)
    /// và <c>decimal?</c> (nhiệt độ, cân nặng, đường huyết) về MỘT trục số để min/max/avg đồng nhất —
    /// <c>int?</c> -> <c>decimal?</c> là chuyển đổi ngầm (lifted) không mất mát.
    ///
    /// Cố ý loại height_cm và pain_scale: chúng không phải chỉ số theo dõi xu hướng lâm sàng như nhóm này;
    /// thêm khi có nhu cầu chỉ cần thêm 1 dòng.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Func<Domain.Entities.VitalSigns, decimal?>> MetricSelectors =
        new Dictionary<string, Func<Domain.Entities.VitalSigns, decimal?>>
        {
            ["temperature_c"]    = v => v.TemperatureC,
            ["heart_rate_bpm"]   = v => v.HeartRateBpm,
            ["respiratory_rate"] = v => v.RespiratoryRate,
            ["bp_systolic"]      = v => v.BpSystolic,
            ["bp_diastolic"]     = v => v.BpDiastolic,
            ["spo2_percent"]     = v => v.Spo2Percent,
            ["weight_kg"]        = v => v.WeightKg,
            ["glucose_mg_dl"]    = v => v.GlucoseMgDl,
        };

    /// <summary>
    /// Xử lý query: chuẩn hoá + kiểm metric, lọc theo bệnh nhân + khoảng ngày, rồi tính thống kê trong bộ nhớ.
    /// KHÔNG throw cho lỗi dự kiến (metric sai) — gói vào <see cref="Result{T}"/> theo quy ước dự án.
    /// </summary>
    public async Task<Result<VitalSignsTrendResponse>> Handle(GetVitalSignsTrendQuery q, CancellationToken ct)
    {
        // Chuẩn hoá tên chỉ số (trim + thường) để dung sai hoa/thường + khoảng trắng từ query string.
        var metricKey = (q.Metric ?? string.Empty).Trim().ToLowerInvariant();
        if (!MetricSelectors.TryGetValue(metricKey, out var selector))
        {
            // Chỉ số ngoài whitelist là lỗi nghiệp vụ (controller map -> 422). Kèm danh sách hợp lệ để client sửa nhanh.
            var allowed = string.Join(", ", MetricSelectors.Keys);
            return Result<VitalSignsTrendResponse>.Failure(
                "VITAL_TREND_INVALID_METRIC",
                $"Chỉ số '{q.Metric}' không hợp lệ. Cho phép: {allowed}");
        }

        var patIdStr = q.PatientId.ToString();
        var query = _db.VitalSigns.Where(v => v.PatientId == patIdStr);

        // Lọc khoảng ngày ĐỒNG NHẤT với GetVitalSignsHistoryQuery: DateFrom từ 00:00, DateTo tới cuối ngày.
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

        // Vật chất hoá rồi tính trong bộ nhớ: selector là Func<> (không dịch được sang SQL) và tập trend nhỏ.
        // Sắp tăng theo RecordedAt; TIE-BREAK bằng CreatedAt rồi Id (khoá chính, DUY NHẤT) để First/Latest và
        // thứ tự Series XÁC ĐỊNH ngay cả khi nhiều bản ghi trùng RecordedAt (batch/nhập cùng mốc). Không dùng
        // RecordSequence làm khoá tie-break vì nó đánh số theo TỪNG encounter — trend gộp theo bệnh nhân (nhiều
        // encounter) nên RecordSequence không đơn điệu toàn cục; Id (PK) mới bảo đảm thứ tự tổng deterministic.
        var rows = await query
            .OrderBy(v => v.RecordedAt)
            .ThenBy(v => v.CreatedAt)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);

        // Loại mẫu null: chỉ số không đo ở bản ghi đó không được tính vào count/thống kê/series.
        var samples = rows
            .Select(v => new { v.RecordedAt, Value = selector(v) })
            .Where(x => x.Value.HasValue)
            .Select(x => new VitalSignsTrendPoint(x.RecordedAt, x.Value!.Value))
            .ToList();

        if (samples.Count == 0)
        {
            // Rỗng vẫn là THÀNH CÔNG (200): count=0, mọi thống kê null, series rỗng — client phân biệt "không có dữ liệu"
            // với lỗi. Trả về tên metric đã chuẩn hoá để client đối chiếu.
            return Result<VitalSignsTrendResponse>.Success(new VitalSignsTrendResponse(
                metricKey, 0, null, null, null, null, null, Array.Empty<VitalSignsTrendPoint>()));
        }

        var response = new VitalSignsTrendResponse(
            metricKey,
            samples.Count,
            samples.Min(s => s.Value),
            samples.Max(s => s.Value),
            Math.Round(samples.Average(s => s.Value), 2), // 2 chữ số thập phân: khớp quy ước làm tròn của dự án
            samples[^1].Value,  // latest = bản ghi MỚI nhất (samples đã sắp tăng theo RecordedAt)
            samples[0].Value,   // first  = bản ghi SỚM nhất
            samples);

        return Result<VitalSignsTrendResponse>.Success(response);
    }
}
