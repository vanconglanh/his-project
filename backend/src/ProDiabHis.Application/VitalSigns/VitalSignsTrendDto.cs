using System;
using System.Collections.Generic;

namespace ProDiabHis.Application.VitalSigns;

/// <summary>
/// DTO trả về của luồng trend sinh hiệu (lớp Application). Trường PascalCase sẽ được serialize snake_case
/// theo chính sách JSON toàn cục của dự án (khớp <see cref="VitalSignsResponse"/>).
///
/// Ngữ nghĩa các thống kê được tính TRÊN CÁC MẪU KHÔNG NULL của chỉ số đã chọn (bản ghi không đo chỉ số
/// đó bị loại khỏi mọi phép tính lẫn <see cref="Series"/>):
/// </summary>
/// <param name="Metric">Tên chỉ số đã chuẩn hoá (snake_case, thường) mà thống kê áp lên.</param>
/// <param name="Count">Số mẫu KHÔNG null tham gia thống kê (0 nếu không có dữ liệu).</param>
/// <param name="Min">Giá trị nhỏ nhất; null khi Count = 0.</param>
/// <param name="Max">Giá trị lớn nhất; null khi Count = 0.</param>
/// <param name="Average">Trung bình, làm tròn 2 chữ số thập phân; null khi Count = 0.</param>
/// <param name="Latest">Giá trị của bản ghi MỚI nhất theo RecordedAt; null khi Count = 0.</param>
/// <param name="First">Giá trị của bản ghi SỚM nhất theo RecordedAt; null khi Count = 0.</param>
/// <param name="Series">Chuỗi thời gian {RecordedAt, Value} đã sắp tăng dần theo RecordedAt (rỗng khi Count = 0).</param>
public record VitalSignsTrendResponse(
    string Metric,
    int Count,
    decimal? Min,
    decimal? Max,
    decimal? Average,
    decimal? Latest,
    decimal? First,
    IReadOnlyList<VitalSignsTrendPoint> Series);

/// <summary>Một điểm trong chuỗi thời gian xu hướng: thời điểm đo + giá trị chỉ số (đã loại null).</summary>
/// <param name="RecordedAt">Thời điểm ghi nhận bản ghi sinh hiệu.</param>
/// <param name="Value">Giá trị chỉ số tại thời điểm đó (không null vì mẫu null đã bị loại).</param>
public record VitalSignsTrendPoint(
    DateTime RecordedAt,
    decimal Value);
