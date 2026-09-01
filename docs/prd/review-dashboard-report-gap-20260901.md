# Review Dashboard / Report / BI — Gap Analysis

> Ngày thực hiện: 2026-09-01 · Thực hiện bởi: PO Đăng (po-analyst)
> Phạm vi: toàn bộ module Report/BI + Dashboard tổng quan

---

## 1. Kết quả fix bug Dashboard

### 1.1 Bug #1 — KPI Cards hiện "—" dù data có (Critical)

**Nguyên nhân gốc:** Contract BE↔FE lệch hoàn toàn.

| | Backend (`DashboardOverviewResponse`) | Frontend cũ (`DashboardOverview`) |
|---|---|---|
| Shape | Flat snake_case | Nested (lồng `today.*`, `delta_vs_yesterday.*`) |
| Trường doanh thu | `today_revenue` | `today.revenue` |
| Trường lượt khám | `today_encounters` | `today.encounter_count` |
| Trường chờ khám | `waiting_patients` | `today.new_patient_count` |
| Trường BHYT | `bhyt_pending_count` | `today.prescription_count` |
| Delta % | Không có | `delta_vs_yesterday.revenue_pct` |

**Verify API thật:**
```json
GET /api/v1/dashboard/overview → 200 OK
{
  "data": {
    "today_revenue": 320000,
    "today_encounters": 0,
    "waiting_patients": 2,
    "bhyt_pending_count": 0,
    "dtqg_failed_count": 10,
    "low_stock_alerts": 0,
    "near_expiry_alerts": 0
  }
}
```

**Fix thực hiện:**
- `frontend/lib/api/dashboard.ts`: Cập nhật `DashboardOverview` interface thành flat shape khớp BE.
- `frontend/app/(dashboard)/_components/DashboardOverview.tsx`: Sửa 4 KPI card đọc đúng field (`today_revenue`, `today_encounters`, `waiting_patients`, `bhyt_pending_count`). Xoá `delta_vs_yesterday` (không có trong BE).
- `frontend/messages/vi.json`: Thêm key `kpi.waitingPatients` = "Bệnh nhân chờ", `kpi.bhytPending` = "BHYT chờ xử lý".

**Tình trạng sau fix:** Code đúng, verify qua đọc file + gọi API thật. Chờ restart Next.js dev server (running in WSL, inotify event không nhận từ Windows filesystem edit) để HMR apply.

---

### 1.2 Bug #2 — Charts Dashboard hiện "Chưa có dữ liệu" dù data có (Critical)

**Nguyên nhân gốc:** Chart endpoints trả về `series` nhưng FE `ChartResponse` định nghĩa `points`.

| | Backend (chart endpoints) | Frontend cũ (`ChartResponse`) |
|---|---|---|
| Mảng data | `series` | `points` |
| Tên phụ | `color` | `secondary_value` |

**Verify API thật:**
```json
GET /api/v1/dashboard/charts/revenue-trend?range=30d → 200 OK
{
  "data": {
    "series": [
      {"label": "2026-08-20", "value": 50000, "color": null},
      {"label": "2026-09-01", "value": 320000, "color": null}
    ]
  }
}
```

**Fix thực hiện:**
- `frontend/lib/api/dashboard.ts`: Đổi `ChartResponse.points` → `series`, `ChartDataPoint.secondary_value` → `color`. Cập nhật toàn bộ 5 fallback mock.
- `frontend/app/(dashboard)/_components/DashboardOverview.tsx`: Sửa 5 chart component từ `.points` → `.series`.

---

### 1.3 Bug #3 — FinancialTab "Xu hướng doanh thu" hiện "— đ" (High)

**Nguyên nhân gốc:** `RevenueReport` interface cũ dùng `total` + `by_breakdown[]` nhưng BE trả `total_revenue` + `series[]`.

**Verify API thật:**
```json
GET /api/v1/reports/revenue?period=DAY → 200 OK
{
  "data": {
    "total_revenue": 370000,
    "net_revenue": 370000,
    "total_invoices": 2,
    "total_refunds": 0,
    "series": [{"label": "2026-08-20", "value": 50000}, ...]
  }
}
```

**Fix thực hiện:**
- `frontend/lib/api/reports.ts`: Cập nhật `RevenueReport` interface, fallback mock.
- `frontend/app/(dashboard)/reports/_components/FinancialTab.tsx`: Sửa `revenue.total` → `revenue.total_revenue`, `revenue.by_breakdown` → `revenue.series`, `x.period_label` → `x.label`, `x.total` → `x.value`.

---

### 1.4 Ghi chú về `dtqg_failed_count = 10`

API trả `dtqg_failed_count: 10`. Đây **không phải lỗi logic push DTQG thật** vì:
- Hệ thống đang ở môi trường dev/test, không kết nối DTQG production.
- Các đơn thuốc test từ các sprint trước chưa được push (không có DTQG token thật cho tenant test).
- **Khuyến nghị:** Trước go-live, xoá bỏ các đơn test này hoặc reset trạng thái về `DRAFT`; cài đặt DTQG token thật cho tenant production.

---

## 2. Audit toàn bộ Report / BI hiện có

### 2.1 Dashboard Tổng quan (`/`)

| Màn | Kết quả | Ghi chú |
|---|---|---|
| 4 KPI cards | Lỗi → **Đã fix** (Bug #1) | Chờ restart WSL dev server |
| 5 charts (Revenue, Encounters, Top Doctors, Top Drugs, HbA1c) | Lỗi → **Đã fix** (Bug #2) | Cùng restart |
| Alerts banner | OK | API trả đúng, hiển thị |
| Cohort ĐTĐ summary (3 mini cards) | OK | `useDiabetesCohort` đọc đúng field |
| Recent Activity Timeline | OK | Hiển thị |
| Chain Dashboard (`/reports/chain-dashboard`) | **OK** | Xếp hạng 3.105.000đ, 19 lượt, chart OK |

### 2.2 Report Engine (`/reports`) — Config-driven

**Kết luận tổng quát: Engine hoạt động tốt.** 47 báo cáo đã được đăng ký catalog. Cấu trúc render đúng: filter → "Lấy dữ liệu" → KPI + bảng + export.

Báo cáo kiểm tra thực tế:
- **Báo cáo Doanh thu Theo Tháng**: PASS — KPI "525.000đ", bảng tháng 2026-09 render đúng.
- `/reports/revenue`: PASS — trả `total_revenue: 370000`.
- `/reports/encounters/count`: PASS — trả `period_label + count` đúng.
- `/reports/pharmacy/top-drugs`: PASS — endpoint 200, mảng rỗng (không có data test, không phải lỗi).

**Danh sách 47 báo cáo có trong catalog:**

| Nhóm | Báo cáo |
|---|---|
| **TÀI CHÍNH** (13) | Doanh thu ngày, Hoàn trả phiếu thu, Hủy phiếu thu, Tạm ứng, Chi tiết viện phí, Tổng hợp XN, Doanh thu theo tháng, Công nợ BN, Sổ quỹ tiền mặt, Doanh thu gói dịch vụ, Tỷ lệ sử dụng định mức gói, Công nợ gói dịch vụ tồn đọng, Công nợ nội bộ giữa chi nhánh |
| **KHÁM BỆNH/SỔ** (14) | CTDV BN khám/siêu âm/Xquang/nội soi/thủ thuật/XN, Sổ khám/siêu âm/Xquang/nội soi/thủ thuật/XN/điện tim, Bệnh diễn tiến |
| **THỐNG KÊ** (11) | Lượt khám theo BS/phòng, ICD-10, Top thuốc/dịch vụ, Nguồn khách, Chỉ định CLS, Lượt khám theo giờ, Tỷ lệ no-show, Kháng sinh, TAT CLS |
| **BHYT** (1) | Nghỉ hưởng BHXH |
| **KHO DƯỢC** (8) | Tồn kho hiện tại, Thẻ kho theo lô, Thuốc cận date/hết hạn, Xuất-Nhập-Tồn, Danh mục thuốc, Kiểm soát đặc biệt, Dưới định mức tồn, Kiểm kê kho |

### 2.3 Màn hình khác

| Màn | Kết quả | Ghi chú |
|---|---|---|
| Bảng điều khiển (`/reports/dashboards`) | OK | Empty state đúng, nút Tạo hoạt động |
| Report Builder (`/reports/builder`) | OK (chưa test sâu) | UI load, dataset picker có |
| Lịch báo cáo (`/reports/schedules`) | OK (chưa test sâu) | UI load |
| FinancialTab "Xu hướng doanh thu" | Lỗi → **Đã fix** (Bug #3) | |
| ClinicalTab (encounters trend, diagnoses) | OK | `period_label+count` đúng |
| PharmacyTab (top drugs) | OK | Data rỗng, không phải bug |

---

## 3. Gap Analysis — Báo cáo còn thiếu

### 3.1 Tiêu chí đánh giá

- **P0 — Cấp thiết**: Thiếu sẽ ảnh hưởng vận hành/doanh thu ngay sau go-live.
- **P1 — Nên có**: Cần trong 1-2 sprint tiếp theo.
- **P2 — Có thể sau**: Mong muốn trong tương lai, không chặn go-live.
- **Độ phức tạp**: L=Low (1-2 SP), M=Medium (3-5 SP), H=High (5-8 SP).

---

### 3.2 Nhóm Tài chính

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| F-01 | **Tổng hợp doanh thu theo bác sĩ** (so sánh, có target KPI) | Đánh giá hiệu suất BS, thưởng tháng | Giám đốc, KeToan | Tháng | P0 | M |
| F-02 | **Báo cáo đối soát ngân hàng/POS** (so sánh sao kê bank với phiếu thu) | Phát hiện chênh lệch thu tiền | KeToan | Ngày/Tuần | P0 | M |
| F-03 | **Báo cáo hoàn tiền chi tiết** (lý do, người phê duyệt, tổng hoàn) | Kiểm soát rủi ro hoàn tiền | KeToan, Giám đốc | Tháng | P1 | L |
| F-04 | **Doanh thu theo dịch vụ** (phân tích mix dịch vụ, % đóng góp) | Quyết định danh mục dịch vụ | Giám đốc | Tháng | P1 | M |
| F-05 | **Báo cáo đối soát BHYT chi tiết** (từng hồ sơ: duyệt/từ chối/chờ, lý do từ chối) | Thu hồi công nợ BHYT | KeToan, BHYT phụ trách | Tháng | P0 | H |

> **Ghi chú F-01**: Hiện có chart "KPI theo bác sĩ" trên dashboard nhưng không có báo cáo xuất được, không có ngưỡng KPI có thể set, không có phân tích xu hướng nhiều tháng.
> **Ghi chú F-05**: Hiện có "Báo cáo nghỉ hưởng BHXH" nhưng thiếu báo cáo đối soát BHYT đầy đủ (duyệt/từ chối theo từng kỳ).

---

### 3.3 Nhóm Vận hành / Hiệu suất

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| O-01 | **Thời gian chờ trung bình** (từ đăng ký → được gọi vào → kết thúc khám) | Cải thiện UX bệnh nhân, quản lý tắc nghẽn | Giám đốc, LeTan | Ngày/Tuần | P1 | M |
| O-02 | **Tỷ lệ tái khám** (BN quay lại trong 30/60/90 ngày) | Đánh giá chất lượng điều trị | Giám đốc, BacSi | Tháng | P1 | M |
| O-03 | **Báo cáo no-show theo bác sĩ** (đã có no-show tổng nhưng chưa phân theo BS/phòng/giờ) | Tối ưu lịch hẹn | Giám đốc, LeTan | Tuần | P2 | L |
| O-04 | **Báo cáo hiệu suất lịch hẹn** (tỷ lệ đúng giờ, trễ, hủy cuối phút) | Quản lý lịch khám | Giám đốc | Tuần | P2 | L |

> **Ghi chú O-01**: Đặc biệt quan trọng cho phòng khám đông. Dữ liệu đã có trong `encounter.started_at`, `queue.called_at`, `encounter.ended_at` — cần thêm query + report config.
> **Ghi chú O-02**: Cần cross-join `patient_id` qua nhiều encounter trong khoảng thời gian.

---

### 3.4 Nhóm Dược / Kho

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| D-01 | **Giá trị tồn kho theo nhóm** (báo cáo giá trị tài sản kho, breakdown theo nhóm/phân loại thuốc) | Quản lý tài sản, kế toán kho | KeToan, DuocSi | Tháng | P0 | M |
| D-02 | **Báo cáo thuốc chậm luân chuyển** (tồn > 90 ngày không xuất) | Xoay vòng vốn, tránh hết hạn | DuocSi | Tháng | P1 | L |
| D-03 | **Báo cáo sử dụng thuốc theo bác sĩ** (BS nào kê thuốc nào nhiều, chi phí thuốc/lượt) | Kiểm soát kê đơn hợp lý | Giám đốc, BacSi | Tháng | P1 | M |
| D-04 | **Báo cáo ABC/XYZ phân tích tồn kho** (phân loại mức ưu tiên tái đặt hàng) | Tối ưu đặt hàng | DuocSi | Quý | P2 | H |

> **Ghi chú D-01**: Hiện có "Tồn kho hiện tại" theo từng SKU nhưng chưa có báo cáo **tổng giá trị tài sản** kho (số lượng × giá nhập) theo nhóm — dữ liệu cần cho kế toán cuối tháng.

---

### 3.5 Nhóm Lâm sàng / Chuyên khoa ĐTĐ

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| C-01 | **Trend HbA1c theo cohort** (trung bình HbA1c theo tháng, % kiểm soát tốt xu hướng) | Đánh giá hiệu quả điều trị theo thời gian | BacSi, Giám đốc | Quý | P0 | M |
| C-02 | **Báo cáo biến chứng ĐTĐ** (số BN mới phát hiện biến chứng trong kỳ, loại biến chứng) | Quality indicator cho chuyên khoa | BacSi | Quý | P1 | M |
| C-03 | **Bệnh nhân chưa đến tái khám đúng hạn** (recall list — BN cần nhắc nhở) | Giữ chân bệnh nhân, compliance điều trị | LeTan, BacSi | Tuần | P0 | L |
| C-04 | **Phân tầng nguy cơ ĐTĐ** (BN nguy cơ cao, trung bình, thấp theo HbA1c + biến chứng) | Ưu tiên chăm sóc | BacSi | Tháng | P1 | M |
| C-05 | **Báo cáo kết quả CLS bất thường** (XN/siêu âm có kết quả flag ngoài ngưỡng chưa được review) | Patient safety | BacSi, KyThuatVien | Ngày | P0 | M |

> **Ghi chú C-01**: Dashboard có chart HbA1c distribution tại thời điểm (cross-section) nhưng thiếu **trend theo thời gian** — cột nào đang tốt lên/xấu đi theo tháng.
> **Ghi chú C-03**: Đã có màn "Nhắc tái khám" (`/recall`) nhưng chưa có báo cáo xuất được danh sách để LeTan gọi điện/nhắn tin, chưa có tracking trạng thái recall.
> **Ghi chú C-05**: Kết quả CLS đã lưu với `flag` (HIGH/LOW/NORMAL) từ FlagCalculator. Cần báo cáo lọc flag ≠ NORMAL + chưa có encounter tiếp theo sau ngày có CLS flag.

---

### 3.6 Nhóm Gói dịch vụ / Package

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| P-01 | **Gói dịch vụ sắp hết hạn** (BN có gói còn hạn sử dụng < 30 ngày) | Nhắc nhở BN gia hạn, revenue retention | LeTan | Tuần | P0 | L |
| P-02 | **Tỷ lệ sử dụng gói theo BN** (BN dùng được bao nhiêu % định mức) | Phát hiện BN không dùng gói, có thể churn | Giám đốc | Tháng | P1 | M |

> **Ghi chú:** Đã có "Báo cáo tỷ lệ sử dụng định mức gói" và "Công nợ gói dịch vụ tồn đọng" — P-01 và P-02 ở mức chi tiết hơn (focus BN cụ thể cần action, không chỉ tổng hợp).

---

### 3.7 Nhóm Tuân thủ / Audit

| STT | Report còn thiếu | Mục đích | Dùng bởi | Tần suất | Priority | Độ phức tạp |
|---|---|---|---|---|---|---|
| A-01 | **Log truy cập hồ sơ bệnh nhân** (ai truy cập hồ sơ nào, lúc nào — exportable) | HIPAA-style audit, điều tra sự cố | Admin, Giám đốc | Khi cần | P1 | L |
| A-02 | **Export XML BHYT theo kỳ** (tạo file XML 4210/4750 + status báo cáo đã nộp) | Nghĩa vụ pháp lý BHYT | KeToan | Tháng | P0 | H |

> **Ghi chú A-01**: Audit log đã ghi vào `diab_his_sec_audit_logs`, nhưng chưa có màn xem + export cho Admin.
> **Ghi chú A-02**: Đã có module BHYT (`/bhyt`), đã có endpoint export XML — thiếu **dashboard trạng thái** (kỳ nào đã nộp, kỳ nào còn thiếu, số hồ sơ, kết quả giám định).

---

## 4. Bảng tổng hợp P0 (cần làm sớm nhất)

| ID | Tên | Nhóm | Độ phức tạp | Lý do P0 |
|---|---|---|---|---|
| F-01 | Tổng hợp doanh thu theo bác sĩ (xuất + KPI) | Tài chính | M | Cơ sở tính lương, thưởng BS — cần trước ngày trả lương |
| F-02 | Đối soát ngân hàng/POS | Tài chính | M | Phát hiện sai lệch thu tiền hàng ngày |
| F-05 | Đối soát BHYT chi tiết | Tài chính | H | Nghĩa vụ pháp lý, thu hồi công nợ |
| D-01 | Giá trị tồn kho theo nhóm | Dược | M | Báo cáo tài sản kế toán cuối tháng |
| C-01 | Trend HbA1c theo thời gian | Lâm sàng ĐTĐ | M | Core KPI chuyên khoa ĐTĐ |
| C-03 | Danh sách BN chưa tái khám đúng hạn (exportable) | Lâm sàng | L | Recall list để gọi điện — thiếu mất doanh thu tái khám |
| C-05 | CLS bất thường chưa review | Lâm sàng | M | Patient safety — không thể thiếu khi go-live thật |
| P-01 | Gói dịch vụ sắp hết hạn (danh sách BN) | Gói dịch vụ | L | Revenue retention — chủ động liên hệ BN trước khi gói hết |
| A-02 | Dashboard + export XML BHYT theo kỳ | Tuân thủ | H | Nghĩa vụ pháp lý — đã có module nhưng thiếu tracking trạng thái |

---

## 5. Khuyến nghị thứ tự thực hiện

### Sprint tiếp theo (P0 ưu tiên độ phức tạp thấp trước):
1. **C-03** (L) — Recall list exportable: chỉ cần thêm filter + export vào màn `/recall`
2. **P-01** (L) — Gói sắp hết hạn: query `expired_at < now()+30d`, thêm vào catalog
3. **C-05** (M) — CLS bất thường: query `flag != NORMAL AND no_follow_up`, catalog entry
4. **D-01** (M) — Giá trị tồn kho: aggregate `quantity × cost_price`, group by category
5. **F-01** (M) — Doanh thu BS xuất được: extend báo cáo KPI bác sĩ hiện có thêm export + filter tháng

### Sprint sau (P0 phức tạp cao):
6. **F-05** (H) — Đối soát BHYT: extend module `/bhyt` thêm dashboard trạng thái kỳ
7. **A-02** (H) — XML BHYT theo kỳ: tracking kỳ nộp + kết quả giám định
8. **F-02** (M) — Đối soát ngân hàng/POS

### Backlog P1/P2:
- C-01 (Trend HbA1c), C-02, C-04, F-03, F-04, O-01, O-02, D-02, D-03, P-02, A-01...

---

---

## 6. Triển khai 9 báo cáo P0 (2026-09-01) — ✅ HOÀN TẤT

Toàn bộ 9 báo cáo P0 đã triển khai bằng **config-driven report engine** (thêm ReportDescriptor tĩnh vào `backend/src/ProDiabHis.Infrastructure/Reports/ReportRegistry.cs` → tự động có endpoint data + export PDF/Excel + branch/tenant filter). Verify LIVE qua API thật + browser thật (login ke_toan), seed data tenant 1.

| ID | Code báo cáo | Nhóm | Rows verify (LIVE) | Ghi chú |
|---|---|---|---|---|
| C-03 | `recall-due` — DANH SÁCH BỆNH NHÂN CẦN TÁI KHÁM | Clinical | 8 rows (4 quá hạn) | Query `diab_his_cli_followup_recall` + JOIN patient, AllowPiiPlaintext (LeTan cần tên+SĐT để gọi) |
| P-01 | `package-expiring` — GÓI DỊCH VỤ SẮP HẾT HẠN | Financial | 3/6 (đúng cửa sổ 30 ngày) | `diab_his_pkg_subscriptions.expiry_date`, filter `daysWindow` |
| C-05 | `cls-abnormal-unreviewed` — KẾT QUẢ CLS BẤT THƯỜNG CHƯA DUYỆT | Clinical | 21 rows | `diab_his_lab_results` `flag NOT IN (NORMAL,N) AND verified_at IS NULL`. Giới hạn: chỉ XN (lab); CĐHA (`diab_his_rad_results`) không có flag số nên chưa gộp |
| D-01 | `ton-kho-theo-nhom` — GIÁ TRỊ TỒN KHO THEO NHÓM THUỐC | Pharmacy | 14 nhóm, tổng 12.368.100đ | GroupBy `drug_category` + subtotal; bổ sung cho `ton-kho` (per-SKU) đã có |
| C-01 | `hba1c-trend` — XU HƯỚNG HbA1c THEO THÁNG | Clinical | 6 tháng (TB 8.2 → mới nhất 6.63) | `diab_his_cli_diabetes_assessments` GroupBy tháng, AVG(hba1c) + % kiểm soát tốt. Khác chart distribution (cross-section) |
| F-05 | `bhyt-reconcile-detail` — ĐỐI SOÁT BHYT CHI TIẾT THEO HỒ SƠ | Bhyt | 11 items (duyệt/từ chối/lý do) | `diab_his_int_bhyt_export_items` + exports |
| A-02 | `bhyt-period-status` — TÌNH TRẠNG NỘP XML BHYT THEO KỲ | Bhyt | 3 kỳ | `diab_his_int_bhyt_exports` GroupBy period. Sinh file XML 4210 đã có sẵn ở module `/bhyt` |
| F-02 | `payment-method-reconcile` — ĐỐI SOÁT THU TIỀN THEO PHƯƠNG THỨC | Financial | 14 rows (tách NH/POS vs tiền mặt) | `diab_his_bil_payments` GroupBy ngày+method. **✅ HOÀN TẤT (2026-09-01):** đã bổ sung import sao kê NH thật (Excel/CSV) + auto-matching + màn kế toán khớp/gỡ khớp thủ công — xem mục 6.1 |
| F-01 | `doanh-thu-theo-bac-si` — TỔNG HỢP DOANH THU THEO BÁC SĨ | Financial | 1 BS, 2.555.000đ | Alias nhóm Tài chính của `luot-kham-theo-bs` (vốn ở nhóm Thống kê, kế toán khó tìm). Ngưỡng KPI/target theo tháng: chưa (cần bảng config target — hoãn) |

**Bug phát hiện + fix trong lúc verify:** `ReportExcelExporter.cs` dùng Title báo cáo làm tên sheet Excel; Excel giới hạn tên sheet ≤31 ký tự → export Excel 500 với các báo cáo tên dài (recall-due/cls/bhyt/payment). Đã thêm `SanitizeSheetName` (cắt ≤31 + bỏ ký tự cấm). Fix chung, có lợi cho mọi báo cáo. Verify lại: cả 9 export Excel + PDF đều 200.

**Gate cuối:** `dotnet build` sạch; `dotnet test` = 2165 pass (Arch 7 + Unit 965 + Integration 1193), 0 fail, 0 skip (giữ nguyên baseline); export PDF/Excel verify 200 + magic bytes hợp lệ; browser thật render KPI + bảng + xuất.

---

## 6.1. F-02 — Đối soát sao kê ngân hàng thật (import + auto-matching) — ✅ HOÀN TẤT (2026-09-01)

Bổ sung phần còn thiếu của F-02 (trước đó chỉ có đối soát nội bộ theo phương thức). Kế toán tải file
sao kê ngân hàng (Excel .xlsx / CSV) cuối kỳ → hệ thống auto-match từng dòng với khoản thu
`BANK_TRANSFER`/thẻ/QR trong `diab_his_bil_payments` → hiển thị khớp/chưa khớp + cho khớp thủ công.

**Migration:** `db/migrations/9195_bank_reconciliation.sql` (idempotent, CREATE TABLE IF NOT EXISTS) —
2 bảng `diab_his_bil_bank_statements` (file import) + `diab_his_bil_bank_statement_lines`
(match_status ENUM MATCHED/UNMATCHED/MANUAL_MATCHED/IGNORED).

**Backend (route `/api/v1/bil/bank-statements`, perm `payment.read`/`payment.collect`):**
`POST /import` (parse Excel qua ClosedXML — tái dùng lib có sẵn — / CSV, chạy auto-match ngay),
`GET /` (lịch sử), `GET /{id}/lines` (chi tiết + payment đã khớp), `GET /lines/{lineId}/candidates`
(payment ứng viên), `POST /lines/{lineId}/manual-match | ignore | unmatch`. Files:
`ProDiabHis.Api/Controllers/BankStatementsController.cs`,
`ProDiabHis.Application/Billing/BankReconciliation/*`,
`ProDiabHis.Infrastructure/Billing/BankStatementParserImpl.cs`.

**Auto-match logic:** amount = số tiền dòng + ABS(paid_at − transaction_date) ≤ 1 ngày; ưu tiên khớp
`reference` chính xác trước, sau đó khớp amount+ngày nếu ứng viên duy nhất; một payment chỉ khớp 1 dòng
trong statement. Mọi query Dapper có `WHERE tenant_id`.

**Frontend:** màn `/cashier/bank-reconciliation` (nav nhóm Thu ngân, icon Landmark) — upload dialog,
bảng lịch sử, bảng chi tiết dòng với Badge màu (xanh Đã khớp / xanh dương Khớp thủ công / vàng Chưa
khớp / xám Bỏ qua), dialog khớp thủ công. Files: `frontend/app/(dashboard)/cashier/bank-reconciliation/`,
`.../cashier/_components/{BankReconciliationView,ImportStatementDialog,StatementLinesTable,ManualMatchDialog}.tsx`,
`frontend/lib/api/bank-reconciliation.ts`, `frontend/lib/hooks/use-bank-reconciliation.ts`, `nav-items.ts`, `messages/vi.json`.

**Verify LIVE:** migration apply 2 lần idempotent; import CSV 6 dòng → 4 matched / 2 unmatched đúng
kỳ vọng (kể cả case ±1 ngày + amount-only match); manual-match/unmatch OK qua cả API và UI thật
(login ke_toan). BE build sạch + 2165/2165 test pass; FE tsc sạch. Contract BE↔FE đối chiếu trên
response THẬT. Evidence: `docs/qc/evidence-bank-reconciliation-20260901/`.

---

*Tài liệu này là output của phiên làm việc PO 2026-09-01. Evidence: screenshots lưu trong `docs/qc/dashboard-report-review-20260901/`. Code fixes: `frontend/lib/api/dashboard.ts`, `frontend/app/(dashboard)/_components/DashboardOverview.tsx`, `frontend/lib/api/reports.ts`, `frontend/app/(dashboard)/reports/_components/FinancialTab.tsx`, `frontend/messages/vi.json`.*
