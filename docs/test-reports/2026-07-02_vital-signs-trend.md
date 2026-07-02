# Evidence — Vital Signs Trend query (UT)

> Bằng chứng kiểm thử cho tính năng **Vital Signs Trend/Statistics** (`GET /api/v1/patients/{patientId}/vital-signs/trend`).
> Sinh theo luồng **code → unit test → evidence**. Chạy offline (harness InMemory), KHÔNG cần MySQL/Docker.

## Thông tin chạy
| Mục | Giá trị |
|---|---|
| Ngày | 2026-07-02 |
| Loại | Unit test (xUnit 2.9.3 + FluentAssertions + EFCore.InMemory) |
| Project | `tests/ProDiabHis.UnitTests` |
| Filter | `FullyQualifiedName~VitalSignsTrend` |
| SDK / runtime | .NET SDK 10.0.301, chạy net8.0 qua `DOTNET_ROLL_FORWARD=LatestMajor` |
| Lệnh | `dotnet test tests/ProDiabHis.UnitTests/ProDiabHis.UnitTests.csproj --filter "FullyQualifiedName~VitalSignsTrend"` |

> Ghi chú môi trường: máy chỉ cài runtime 6.0 & 10.0 (không có net8) nên cần `DOTNET_ROLL_FORWARD=LatestMajor`.
> Tại thời điểm chạy, một tiến trình API đang chiếm `src/ProDiabHis.Api/bin` → build được redirect qua `-p:OutDir=<tmp>` để không đụng app đang chạy (không ảnh hưởng kết quả test).

## Kết quả
```
Passed!  - Failed: 0, Passed: 21, Skipped: 0, Total: 21, Duration: 4 s - ProDiabHis.UnitTests.dll (net8.0)
```

**Tổng: 21 PASS / 0 FAIL / 0 SKIP.** (Vòng đầu 9 case; +12 case sau **review đối kháng** — xem mục "Hardening".)

## Ma trận case → kỳ vọng (đã xanh)
| # | Test case | Tuyến kiểm chứng | Kỳ vọng |
|---|---|---|---|
| 1 | `Trend_Glucose_HappyPath_ComputesStatsAndOrderedSeries` | Happy path | count=3, min=100, max=200, average=150, first=100, latest=150, series đúng thứ tự |
| 2 | `Trend_DateRange_ExcludesRowsOutsideWindow` | Lọc khoảng ngày | Ngoài `[date_from, date_to]` bị loại; count=2, min=100, max=140 |
| 3 | `Trend_NullSamples_AreSkipped` | Bỏ mẫu null | Bản ghi không đo chỉ số → không tính; count=2 |
| 4 | `Trend_UnknownMetric_ReturnsFailure` | Metric ngoài whitelist | `IsSuccess=false`, `ErrorCode=VITAL_TREND_INVALID_METRIC` |
| 5 | `Trend_EmptyResult_ReturnsZeroCountNullStats` | Tập rỗng | `IsSuccess=true`, count=0, mọi thống kê null, series rỗng |
| 6 | `Trend_TenantFilter_ExcludesOtherTenantRows` | Cô lập đa-tenant | Bản ghi tenant khác bị global query filter loại; count=1, max=100 |
| 7 | `Trend_SelectorMap_ReadsCorrectColumnPerMetric` **[Theory ×8]** | Selector map | Đọc đúng cột cho **cả 8 metric** (int? lẫn decimal?): glucose/bp_systolic/bp_diastolic/heart_rate/respiratory/spo2/weight/temperature |
| 8 | `Trend_Series_OrderedByRecordedAt_RegardlessOfInsertionOrder` | Thứ tự Series | Seed **lệch thứ tự chèn** → Series tăng dần theo RecordedAt; first=100, latest=300 (giết mutation xoá OrderBy) |
| 9 | `Trend_Ties_SameRecordedAt_TieBreakByCreatedAt` | Tie-break | Trùng RecordedAt → thứ tự xác định theo CreatedAt; first=100, latest=200 |
| 10 | `Trend_Average_RoundsToTwoDecimals` | Làm tròn | (100+101+103)/3 → average=**101.33** (chốt Math.Round 2 chữ số) |
| 11 | `Trend_DateRange_BoundariesAreInclusive` | Biên ngày | Bản ghi tại 00:30 đầu `from` & 23:00 cuối `to` LỌT; `from-1`/`to+1` LOẠI; count=2 |
| 12 | `Trend_DateRange_OnlyDateFrom_FiltersLowerBoundOnly` | Chỉ chặn dưới | DateTo=null → chỉ lọc `>= from`; count=2 |
| 13 | `Trend_DateRange_OnlyDateTo_FiltersUpperBoundOnly` | Chỉ chặn trên | DateFrom=null → chỉ lọc `<= to`; count=2 |
| 14 | `Trend_MetricNormalization_TrimAndLowercase` | Chuẩn hoá metric | `"  Glucose_Mg_Dl  "` → success, count=3, `metric="glucose_mg_dl"` |

## Hardening (từ review đối kháng đa-lens)
Review 4 lens (correctness / convention / tenant-security / test-adequacy) + verify từng finding → **6 finding có thật**:
- **1 correctness (đã sửa source):** ordering thiếu tie-break → khi trùng `RecordedAt`, First/Latest/Series không xác định trên SQL thật. Fix: `OrderBy(RecordedAt).ThenBy(CreatedAt).ThenBy(Id)` (KHÔNG dùng RecordSequence — nó đánh số theo từng encounter, không đơn điệu theo bệnh nhân).
- **5 test-adequacy (đã bổ sung test):** thiếu case thứ-tự-chèn-lệch, ties, làm-tròn-phân-số, biên khoảng ngày, chuẩn-hoá-metric, và Theory chỉ phủ 3/8 metric → bổ sung thành case 7–14 ở trên.

## Nguồn (traceability)
- Source: `src/ProDiabHis.Application/VitalSigns/VitalSignsTrendQuery.cs`, `VitalSignsTrendDto.cs`, `VitalSignsTrendHandler.cs`
- API: `src/ProDiabHis.Api/Controllers/VitalSignsController.cs` (action `Trend`)
- Contract: `docs/api/openapi/vital-signs.yaml` (path `/patients/{patientId}/vital-signs/trend`)
- Test: `tests/ProDiabHis.UnitTests/VitalSigns/VitalSignsTrendHandlerTests.cs`

## Assert bám gì
Mọi assert bám **cấu trúc `Result<T>`/DTO + giá trị số** (seed cố định → min/max/avg/first/latest), không bám câu chữ. Giá trị seed chọn sao cho trung bình chia hết → làm tròn không mập mờ.
