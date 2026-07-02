using System;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.VitalSigns;

/// <summary>
/// Query (CQRS — lớp Application) yêu cầu thống kê XU HƯỚNG của MỘT chỉ số sinh hiệu cho một bệnh nhân
/// trong một khoảng ngày. Được gửi bởi <c>VitalSignsController.Trend</c> qua MediatR và xử lý bởi
/// <see cref="GetVitalSignsTrendQueryHandler"/>.
///
/// Vì sao tồn tại: <see cref="GetVitalSignsHistoryQuery"/> đã có sẵn tham số <c>Metric</c> nhưng handler
/// lịch sử KHÔNG dùng tới — phần tổng hợp/thống kê được dự tính mà chưa hiện thực. Query này lấp đúng
/// khoảng trống đó, tách riêng khỏi history (history trả danh sách bản ghi, trend trả thống kê + chuỗi số).
/// </summary>
/// <param name="PatientId">Bệnh nhân cần xem xu hướng.</param>
/// <param name="Metric">
/// Tên chỉ số dạng snake_case (khớp quy ước JSON/DB), ví dụ <c>glucose_mg_dl</c>, <c>bp_systolic</c>.
/// Ngoài whitelist -> handler trả <c>VITAL_TREND_INVALID_METRIC</c>.
/// </param>
/// <param name="DateFrom">Ngày bắt đầu (bao gồm, tính từ 00:00). Null = không chặn dưới.</param>
/// <param name="DateTo">Ngày kết thúc (bao gồm, tới 23:59:59.9999999). Null = không chặn trên.</param>
public record GetVitalSignsTrendQuery(
    Guid PatientId,
    string Metric,
    DateOnly? DateFrom,
    DateOnly? DateTo)
    : IRequest<Result<VitalSignsTrendResponse>>;
