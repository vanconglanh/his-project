# PRD — Danh mục Báo cáo (Report Catalog) & Mẫu chuẩn

> Module: **Report/BI** · Tham chiếu nghiệp vụ: HIS SUNS (bản đang chạy) + đối chiếu HIS thị trường (VNPT-HIS, FPT.eHospital, Hồng Ngọc, eClinic).
> Trạng thái hạ tầng hiện có: QuestPDF exporter đã dựng 3 loại (`Financial` / `Clinical` / `Pharmacy`), header diaB màu `#01645A` đã duyệt, barcode CODE128, Excel exporter, cache.
> Mục tiêu PRD này: (1) chuẩn hoá **danh mục ~23 báo cáo** in/xuất, (2) đặc tả **1 mẫu chuẩn** — *BC Doanh Thu Ngày* — để dev implement và nhân bản cho các báo cáo còn lại.

---

## 1. Nguyên tắc chung cho MỌI báo cáo

Mọi báo cáo trong module đều tuân theo một khung thống nhất (nhân bản từ mẫu ở §4):

### 1.1. Màn hình "Xuất báo cáo" (Report Export Screen)
Luồng: **Chọn filter → `Lấy dữ liệu` → xem trên lưới → `In Phiếu` (PDF) / `Xuất Excel`**.

| Vùng | Thành phần |
|------|-----------|
| Thanh công cụ | Nút `In Phiếu` (PDF A4), `Xuất ra Excel` (.xlsx) |
| Bộ lọc | `Từ ngày` – `Đến ngày` (bắt buộc, mặc định = tháng hiện tại) + các filter riêng theo báo cáo |
| Hành động | Nút `Lấy dữ liệu` (fetch/refresh lưới) |
| Kết quả | Lưới có nhóm (group-by), dòng tổng cộng cuối bảng, phân trang (mặc định 100 dòng/trang) |

### 1.2. Letterhead (header) chuẩn — dùng chung
Tái sử dụng `RenderLetterhead` hiện có (`QuestPdfReportExporter.cs`). Nguồn dữ liệu: bảng `diab_his_sys_tenants` → `LetterheadDto`.

```
┌──────────────────────────────────────────────────────────────┐
│ [LOGO]   DIA-B — Nguyễn Ngọc Minh Chi                         │  ← nền #01645A, chữ trắng
│  diaB    <Tên công ty / phòng khám>                           │
│          <Địa chỉ>                                            │
│          Mã CSKCB: xxxx • ĐT: xxxx • email@...               │
└──────────────────────────────────────────────────────────────┘
                     TÊN BÁO CÁO (uppercase, #01645A)
                     [barcode CODE128]  Mã: DIAB260707-xxxx
   Kỳ báo cáo: dd/MM/yyyy – dd/MM/yyyy          Người xuất: <fullname>
   Ngày xuất: dd/MM/yyyy                        Mã báo cáo: <ReportCode>
```

**Footer chuẩn** (đã có): đường kẻ + khối chữ ký "NGƯỜI LẬP BÁO CÁO (Ký, ghi rõ họ tên)" + "Ngày … tháng … năm …" + `Trang x / y`.
> Gợi ý tham khảo SUNS: có thể bổ sung dải liên hệ hỗ trợ cuối trang (email hỗ trợ / hotline / website) từ các field `EmailSupport`, `Phone` đã có trong `LetterheadDto`.

### 1.3. Quy ước hiển thị
- Tiền tệ: `#,##0` VND, canh phải. Ngày: `dd/MM/yyyy`. Số lượng: canh phải.
- Bảng: header nền `#01645A` chữ trắng, zebra rows `#F3F8F7`, dòng **TỔNG CỘNG** nền `#014A42` chữ trắng.
- Mọi query bắt buộc `WHERE tenant_id = @tenantId` (multi-tenant app-layer).
- KPI cards (tùy báo cáo): 3 thẻ tóm tắt đầu trang (ví dụ Tổng thu / Số phiếu / TB/phiếu).

---

## 2. Danh mục Báo cáo (23 báo cáo)

Phân thành **5 nhóm**. Cột "Loại render" ánh xạ tới exporter cần dùng/mở rộng.

### Nhóm A — Tài chính / Thu ngân (Financial)

| # | Tên báo cáo | Mục đích | Filter chính | Cột chính | Loại render |
|---|-------------|----------|--------------|-----------|-------------|
| A1 | **BC Doanh Thu Ngày** ⭐ | Doanh thu thực thu theo ngày, gộp theo nhân viên/quầy thu | Từ–Đến ngày, Người thu, Quầy thu, Tiền chênh lệch | Ngày, Số phiếu, Mã BN, Họ tên, Diễn giải, Tổng CP, Giảm, Tăng, Hoàn trả, Tạm ứng, Tiền mặt, Chuyển khoản, Thẻ, Thực thu, Nguồn khách | Financial (mở rộng) |
| A2 | **BC Hoàn Trả Phiếu Thu** | Danh sách phiếu thu đã hoàn trả | Từ–Đến ngày, Người thu, Quầy thu | Ngày, Số phiếu gốc, Mã BN, Họ tên, Lý do hoàn, Số tiền hoàn, Người thực hiện | Financial |
| A3 | **BC Hủy Phiếu Thu** | Danh sách phiếu thu đã hủy | Từ–Đến ngày, Người thu, Quầy thu | Ngày, Số phiếu, Mã BN, Họ tên, Lý do hủy, Số tiền, Người hủy | Financial |
| A4 | **BC Tạm Ứng** | Theo dõi khoản tạm ứng của bệnh nhân | Từ–Đến ngày, Trạng thái (còn/đã trừ) | Ngày, Số phiếu, Mã BN, Họ tên, Số tạm ứng, Đã trừ, Còn lại | Financial |
| A5 | **BC Chi Tiết Viện Phí** | Bóc tách viện phí theo dịch vụ của từng lượt | Từ–Đến ngày, Bệnh nhân, Nhóm dịch vụ | Số phiếu, BN, Nhóm DV, Tên DV, SL, Đơn giá, Thành tiền, BHYT chi trả, BN tự trả | Financial |
| A6 | **BC Tổng Hợp Xét Nghiệm** | Tổng hợp doanh thu XN theo loại | Từ–Đến ngày, Loại XN | Nhóm XN, Tên XN, Số lượt, Đơn giá, Doanh thu | Financial |

### Nhóm B — Chi tiết dịch vụ bệnh nhân (CTDV BN)

> Cùng một khuôn (Clinical + số liệu dịch vụ), khác nhau ở loại dịch vụ lọc.

| # | Tên báo cáo | Loại dịch vụ | Cột chính | Loại render |
|---|-------------|--------------|-----------|-------------|
| B1 | **BC CTDV BN Khám Bệnh** | Khám bệnh | Ngày, Mã BN, Họ tên, Bác sĩ, Chẩn đoán ICD-10, Phòng khám, Thành tiền | Clinical (mở rộng) |
| B2 | **BC CTDV BN Siêu Âm** | Siêu âm | Ngày, Mã BN, Họ tên, BS chỉ định, Vị trí SÂ, Kết luận, Thành tiền | Clinical |
| B3 | **BC CTDV BN XQuang** | X-Quang | Ngày, Mã BN, Họ tên, BS chỉ định, Vùng chụp, Kết luận, Thành tiền | Clinical |
| B4 | **BC CTDV BN Nội Soi** | Nội soi | Ngày, Mã BN, Họ tên, BS thực hiện, Loại nội soi, Kết luận, Thành tiền | Clinical |
| B5 | **BC CTDV BN Thủ Thuật** | Thủ thuật | Ngày, Mã BN, Họ tên, BS thực hiện, Tên thủ thuật, Thành tiền | Clinical |
| B6 | **BC CTDV BN Xét Nghiệm** | Xét nghiệm | Ngày, Mã BN, Họ tên, BS chỉ định, Nhóm XN, Số chỉ số, Thành tiền | Clinical |

### Nhóm C — Sổ (Register / Logbook)

> "Sổ" = danh sách liệt kê theo trình tự thời gian phục vụ đối chiếu/lưu trữ (không nhấn mạnh doanh thu). Layout gọn, nhiều dòng/trang.

| # | Tên báo cáo | Cột chính | Loại render |
|---|-------------|-----------|-------------|
| C1 | **BC Sổ Khám Bệnh** | STT, Ngày, Mã BN, Họ tên, Tuổi, Giới, Địa chỉ, Chẩn đoán, Bác sĩ | Clinical (register) |
| C2 | **BC Sổ Siêu Âm** | STT, Ngày, Mã BN, Họ tên, Chỉ định, Kết luận, BS đọc | Clinical (register) |
| C3 | **BC Sổ XQuang** | STT, Ngày, Mã BN, Họ tên, Vùng chụp, Kết luận, BS đọc | Clinical (register) |
| C4 | **BC Sổ Nội Soi** | STT, Ngày, Mã BN, Họ tên, Loại NS, Kết luận, BS thực hiện | Clinical (register) |
| C5 | **BC Sổ Thủ Thuật** | STT, Ngày, Mã BN, Họ tên, Tên thủ thuật, BS thực hiện | Clinical (register) |
| C6 | **BC Sổ Xét Nghiệm** | STT, Ngày, Mã BN, Họ tên, Nhóm XN, Số chỉ số, KTV | Clinical (register) |
| C7 | **BC Sổ Điện Tim** | STT, Ngày, Mã BN, Họ tên, Kết luận ECG, BS đọc | Clinical (register) |

### Nhóm D — Thống kê lượt khám (Statistics)

| # | Tên báo cáo | Mục đích | Cột chính | Loại render |
|---|-------------|----------|-----------|-------------|
| D1 | **BC Lượt Khám Theo BS** | Số lượt khám gộp theo bác sĩ | Bác sĩ, Số lượt, Số BN mới, Số toa, Doanh thu, TB/lượt | Clinical KPI (đã có `DoctorKpiResponse`) |
| D2 | **BC Lượt Khám Theo PK** | Số lượt khám gộp theo phòng khám | Phòng khám, Số lượt, Tỷ trọng %, Doanh thu | Clinical KPI (mở rộng) |

### Nhóm E — BHXH / Lâm sàng đặc thù

| # | Tên báo cáo | Mục đích | Cột chính | Loại render |
|---|-------------|----------|-----------|-------------|
| E1 | **BC Bệnh Diễn Tiến** | Theo dõi diễn tiến bệnh của BN mạn tính (phù hợp bối cảnh ĐTĐ) | Ngày khám, Chẩn đoán, HbA1c, Glucose, HA, BMI, Ghi chú | Clinical (diễn tiến) |
| E2 | **BC Nghỉ Hưởng BHXH** | Danh sách giấy chứng nhận nghỉ việc hưởng BHXH (mẫu C65/C84) | Số GCN, Mã BN, Họ tên, Số thẻ BHXH, Chẩn đoán, Số ngày nghỉ, Từ–Đến, BS ký | Chuyên biệt (mẫu BHXH) |

> **Ghi chú ánh xạ exporter:** `Financial` (Nhóm A) và `Clinical` (Nhóm B, C, E) đã có sẵn khung — chỉ cần bổ sung cột/DTO. Nhóm D dùng KPI. E2 cần mẫu riêng theo quy định BHXH (ưu tiên thấp).

---

## 3. Thứ tự triển khai đề xuất (roadmap)

| Đợt | Báo cáo | Lý do |
|-----|---------|-------|
| **1 (mẫu chuẩn)** | A1 Doanh Thu Ngày | Nghiệp vụ lõi, nhiều cột nhất → validate được khung |
| 2 | A2, A3, A4 (hoàn trả/hủy/tạm ứng) | Cùng nhóm Financial, tái dùng A1 |
| 3 | B1–B6 (CTDV) | Cùng khuôn Clinical, khác filter loại DV |
| 4 | C1–C7 (Sổ) | Register layout, tái dùng Clinical |
| 5 | D1, D2 (lượt khám) | Đã có nền KPI |
| 6 | A5, A6, E1, E2 | Chuyên biệt, làm sau |

---

## 4. ĐẶC TẢ MẪU CHUẨN — BC Doanh Thu Ngày (A1)

Đây là mẫu để dev implement trước, các báo cáo khác nhân bản theo.

### 4.1. Màn hình xuất báo cáo (FE)

**Route:** `frontend/app/(dashboard)/reports/revenue-daily/page.tsx`

**Bộ lọc:**
| Field | Kiểu | Bắt buộc | Mặc định | Ghi chú |
|-------|------|:---:|----------|---------|
| `fromDate` | date | ✓ | đầu tháng | |
| `toDate` | date | ✓ | hôm nay | ≥ fromDate |
| `collectorId` | select (có checkbox bật/tắt) | – | Tất cả | "Người thu" — user role KeToan/LeTan |
| `counterId` | select (có checkbox bật/tắt) | – | Tất cả | "Quầy thu" — danh mục quầy |
| `varianceFilter` | select | – | Tất cả | "Tiền chênh lệch": Tất cả / Có chênh lệch / Không |

**Hành động:** `Lấy dữ liệu` → gọi API; `In Phiếu` → PDF; `Xuất ra Excel` → xlsx.

**Lưới kết quả:** group theo **Nhân viên (người thu)**, mỗi nhóm có dòng đếm số phiếu; footer tổng toàn báo cáo.

### 4.2. Cột dữ liệu (khớp SUNS)

| # | Cột | Field | Kiểu | Canh | Ghi chú |
|---|-----|-------|------|------|---------|
| 1 | Ngày | `date` | datetime | trái | `dd/MM/yyyy HH:mm` |
| 2 | Số Phiếu | `receiptNo` | int | trái | |
| 3 | Mã | `patientCode` | string | trái | vd `DIAB260702...` |
| 4 | Họ tên | `patientName` | string | trái | |
| 5 | Diễn giải | `description` | string | trái | vd `Thu phí dịch vụ(Khám Bệnh: 350,000)` |
| 6 | Tổng CP | `totalCost` | money | phải | Tổng chi phí |
| 7 | Số giảm | `discount` | money | phải | |
| 8 | Số tăng | `surcharge` | money | phải | |
| 9 | Hoàn trả | `refund` | money | phải | |
| 10 | Tạm ứng | `advance` | money | phải | |
| 11 | Tiền mặt | `cash` | money | phải | |
| 12 | Chuyển khoản | `bankTransfer` | money | phải | |
| 13 | Thẻ Visa/Master | `card` | money | phải | |
| 14 | Thực thu | `netCollected` | money | phải | **cột nhấn mạnh** (bold) |
| 15 | Nguồn khách | `patientSource` | string | trái | kênh đến của BN |

**Dòng tổng nhóm (mỗi nhân viên):** đếm số phiếu.
**Dòng TỔNG CỘNG (cuối báo cáo):** cộng cột 6–14. (Ví dụ SUNS: 14 phiếu · Tổng CP 4.895.000 · Giảm 350.000 · Tiền mặt 2.795.000 · CK 1.750.000 · Thực thu 4.545.000.)

### 4.3. KPI cards (đầu trang PDF)
`TỔNG THỰC THU` (teal) · `SỐ PHIẾU THU` (amber) · `TB / PHIẾU` (teal) — tính từ dữ liệu thật (không bịa).

### 4.4. Backend

**DTO mới** (`ReportDtos.cs`):
```csharp
/// <summary>Hàng báo cáo doanh thu ngày (khớp lưới thu ngân SUNS).</summary>
public record RevenueDailyRowDto(
    int Stt,
    DateTime Date,
    string ReceiptNo,
    string PatientCode,
    string PatientName,
    string Description,
    decimal TotalCost,
    decimal Discount,
    decimal Surcharge,
    decimal Refund,
    decimal Advance,
    decimal Cash,
    decimal BankTransfer,
    decimal Card,
    decimal NetCollected,
    string? PatientSource,
    string CollectorName);   // để group-by nhân viên
```

**Truy vấn (Dapper, read):** join phiếu thu ↔ chi tiết thanh toán ↔ bệnh nhân ↔ user thu ngân. Bắt buộc:
```sql
WHERE t.tenant_id = @tenantId
  AND t.paid_at BETWEEN @from AND @to
  AND (@collectorId IS NULL OR t.collector_id = @collectorId)
  AND (@counterId  IS NULL OR t.counter_id  = @counterId)
ORDER BY t.collector_id, t.paid_at
```

**Exporter:** thêm `ReportType.RevenueDaily`; PDF dùng **A4 Landscape** (15 cột) — tái dùng `RenderLetterhead / RenderTitle / RenderBarcode / RenderMeta / RenderFooter`, thêm `RenderRevenueDailyBody` (group theo `CollectorName`, dòng tổng nhóm + TỔNG CỘNG như §1.3).

**Endpoint** (`ReportsController.cs`):
```
GET /api/v1/reports/revenue-daily?from=&to=&collectorId=&counterId=&variance=   → JSON (lưới)
GET /api/v1/reports/revenue-daily/export?...&format=pdf|excel                    → file
```

### 4.5. Acceptance Criteria (mẫu)
- [ ] Chọn khoảng ngày + `Lấy dữ liệu` → lưới hiển thị đúng, group theo nhân viên, có dòng đếm & TỔNG CỘNG.
- [ ] Lọc `Người thu` / `Quầy thu` / `Tiền chênh lệch` áp dụng đúng.
- [ ] `In Phiếu` → PDF A4 Landscape có header diaB (#01645A), barcode mã báo cáo, 15 cột, KPI, footer chữ ký, số trang.
- [ ] `Xuất Excel` → .xlsx đủ cột + dòng tổng.
- [ ] Tổng cột 6–14 khớp tổng từng nhóm; số liệu đúng tenant (không rò rỉ cross-tenant).
- [ ] Không có dữ liệu → empty state "Không có dữ liệu trong kỳ".

---

## 5. Các quyết định đã chốt (design decisions)

### 5.1. "Quầy thu" — LÀ một chiều của doanh thu, có bảng danh mục ✅
- **"Quầy thu" là điểm thu tiền** (collection counter): Quầy dịch vụ / Quầy nhà thuốc / Quầy CLS. Doanh thu được gom theo quầy — đúng như SUNS lọc `QUẦY THU DỊCH VỤ`. Đây là **chiều bắt buộc giữ** trong báo cáo A1.
- **Migration:** `db/migrations/9035_create_bil_counters.sql`:
  - Bảng danh mục `diab_his_bil_counters` (tenant-scoped: `code`, `name`, `sort_order`, `status`).
  - Seed 3 quầy mặc định/tenant: `DICH_VU` Quầy thu dịch vụ · `NHA_THUOC` Quầy thu nhà thuốc · `CLS` Quầy thu CLS (XN/CĐHA).
  - Thêm cột `counter_id CHAR(36)` vào `diab_his_bil_billing` (+ index `(tenant_id, counter_id)`) — mỗi phiếu thu thuộc 1 quầy.
- **Trong báo cáo A1:** filter `counterId` (Quầy thu) + vẫn group theo **Người thu** (`billing.created_by` / cashier user). Hai chiều độc lập: lọc theo quầy, gom theo nhân viên (khớp lưới SUNS).

### 5.2. "Nguồn khách" (patient source) — ĐÃ thêm field ✅
- **Migration:** `db/migrations/9034_pat_add_patient_source.sql` — thêm cột `patient_source VARCHAR(50)` vào `diab_his_pat_patients` + index `(tenant_id, patient_source)`.
- **Enum (application-layer, lưu string):**
  `WALK_IN` Vãng lai · `REFERRAL` Giới thiệu · `RETURN` Tái khám · `ONLINE` Đặt khám online · `INSURANCE` BHYT/bảo lãnh · `MARKETING` Marketing/quảng cáo · `OTHER` Khác.
- Cột "Nguồn khách" trong báo cáo A1 join từ `patient.patient_source` → label tiếng Việt.
- **TODO FE:** thêm select "Nguồn khách" vào form tạo/sửa hồ sơ BN (mặc định `WALK_IN`).

### 5.3. "Tiền chênh lệch" — công thức chuẩn HIS ✅
Áp dụng định nghĩa phổ biến của các HIS (VNPT-HIS / eHospital): chênh lệch giữa **số phải thu** và **số thực thu** trên từng phiếu.

```
Số phải thu   = Tổng CP - Số giảm + Số tăng
Chênh lệch    = Số phải thu - Thực thu
```
- `Chênh lệch = 0` → thu đủ.
- `Chênh lệch > 0` → còn thiếu (BN nợ / ghi công nợ).
- `Chênh lệch < 0` → thu thừa / tạm ứng dư.

**Filter `varianceFilter`:** `Tất cả` · `Có chênh lệch` (≠ 0) · `Không chênh lệch` (= 0).
> Lưu ý phân biệt: chênh lệch **ca thu ngân** (`cashier_shifts.difference = actual_cash - expected_cash`, tiền mặt cuối ca) là khái niệm khác — không dùng cho cột này của báo cáo phiếu thu.

### 5.4. E2 Nghỉ hưởng BHXH — theo mẫu chuẩn TT 56/2017/TT-BYT ✅
Áp dụng **Giấy chứng nhận nghỉ việc hưởng BHXH mẫu C65-HD1** (Phụ lục 7, TT 56/2017/TT-BYT). Báo cáo E2 = danh sách các GCN đã cấp:
- Cột: Số seri/Số GCN, Mã BN, Họ tên, Số thẻ BHXH/BHYT, Chẩn đoán (ICD-10), Số ngày nghỉ, Từ ngày – Đến ngày, Bác sĩ ký, Ngày cấp.
- Ưu tiên: **Đợt 6** (chuyên biệt). Cần bảng lưu GCN nghỉ việc (`diab_his_cli_sick_leaves`) — đặc tả riêng khi tới đợt này.

---

## 6. Việc còn mở (theo dõi)
- [ ] FE: thêm control chọn "Nguồn khách" vào form hồ sơ bệnh nhân (dùng enum §5.2).
- [ ] Xác nhận `diab_his_bil_billing` có đủ cột tách `discount` / `surcharge` / `advance` / theo phương thức (`cash`/`transfer`/`card`) hay cần bổ sung migration khi implement A1.
- [ ] Đợt 6: đặc tả bảng GCN nghỉ việc hưởng BHXH (E2).

---

## 7. Đối chiếu thị trường & khoảng trống (Gap analysis)

> Bối cảnh đánh giá: **phòng khám đa khoa nhỏ (2–5 BS), trọng tâm đái tháo đường (ĐTĐ), Cloud SaaS đa tenant**. Không phải mọi báo cáo của bệnh viện lớn đều áp dụng cho phòng khám tư quy mô nhỏ. Cột "Trạng thái" đánh giá theo **thực tế nghiệp vụ phòng khám tư**, không sao chép nguyên checklist bệnh viện công.
>
> Ký hiệu trạng thái: **ĐÃ CÓ** (map tới code hiện có) · **THIẾU** (cần bổ sung) · **KHÔNG ÁP DỤNG** (không phù hợp quy mô/mô hình phòng khám nhỏ, hoặc chỉ áp dụng khi có dịch vụ tương ứng).
> Ký hiệu loại: **PL** = Bắt buộc pháp lý · **QT** = Quản trị/vận hành · **BI** = Business Intelligence.
> Lưu ý: một số số hiệu văn bản trong checklist research **cần tra lại** trước khi implement — xem §7.E.

### 7.A. Bảng gap theo 9 domain

#### Domain 1 — Tiếp đón / Reception
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Sổ đăng ký khám (tiếp đón) | THIẾU | QT | Khác `so-kham-benh` (C1, sổ lâm sàng sau khám). Cần sổ danh sách BN đăng ký/lấy số theo ngày. | Nội bộ |
| Thống kê lượt theo giờ (peak hour) | THIẾU | BI | Report Engine descriptor: group `HOUR(reception_at)`. | Nội bộ |
| Danh sách chờ (live queue) | THIẾU | QT | Màn hình realtime, **không phải báo cáo in** — thuộc module Reception, ngoài phạm vi Report Catalog. | Nội bộ |
| Thống kê nguồn bệnh nhân | ĐÃ CÓ (một phần) | BI | Field `patient_source` đã có (§5.2); còn thiếu descriptor tổng hợp theo nguồn. | Nội bộ |
| Tỷ lệ no-show | THIẾU | BI | Cần trạng thái lịch hẹn (booked vs arrived). Phụ thuộc module đặt hẹn. | Nội bộ |

#### Domain 2 — Lâm sàng / Clinical
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Sổ khám bệnh | ĐÃ CÓ | PL/QT | `so-kham-benh` (C1). | TT 52/2017/TT-BYT (cần xác minh, xem §7.E) |
| Thống kê ICD-10 / mô hình bệnh tật | THIẾU | PL | Định kỳ báo cáo cơ quan y tế. Descriptor group theo `icd10_code`. | TT 37/2019, TT 20/2019 (cần xác minh) |
| Báo cáo bệnh truyền nhiễm | THIẾU (có điều kiện) | PL | Bắt buộc **khi phát hiện ca bệnh truyền nhiễm thuộc danh mục khai báo**. Phòng khám ĐTĐ ít phát sinh nhưng vẫn có nghĩa vụ khai báo khi gặp. | TT 54/2015/TT-BYT |
| Sổ theo dõi bệnh mạn tính | ĐÃ CÓ (một phần) | QT | `benh-dien-tien` (E1) đã phục vụ theo dõi ĐTĐ (HbA1c/Glucose/HA/BMI). Đây là **thế mạnh** cho trọng tâm ĐTĐ. | Nội bộ |
| Giấy khám sức khỏe | THIẾU (có điều kiện) | PL | Chỉ áp dụng nếu phòng khám **có cấp GKSK**. Là biểu mẫu cấp phát, không phải báo cáo tổng hợp. | TT 14/2013/TTLT-BYT-BLĐTBXH (cần xác minh — có thể đã bị thay thế) |
| Giấy ra viện | KHÔNG ÁP DỤNG | PL | Phòng khám ngoại trú không có nội trú/ra viện. Chỉ cần khi có lưu bệnh. | TT 56/2017/TT-BYT |
| Đơn thuốc (in) | ĐÃ CÓ | PL | Đã có PDF đơn thuốc QuestPDF (commit d8e3075) + tích hợp ĐTQG. | TT 27/2021/TT-BYT |

#### Domain 3 — Cận lâm sàng / CLS
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Sổ xét nghiệm | ĐÃ CÓ | QT | `so-xet-nghiem` (C6). | Nội bộ |
| Sổ CĐHA (SÂ/XQ/nội soi/điện tim) | ĐÃ CÓ | QT | C2/C3/C4/C7. | TT 50/2017/TT-BYT (cần xác minh) |
| Thống kê chỉ định CLS | THIẾU | QT/BI | Số lượt/loại chỉ định CLS theo BS/kỳ. Descriptor group theo `modality`/`lab_group`. | Nội bộ |
| Quản lý mẫu (specimen tracking) | KHÔNG ÁP DỤNG | QT | Phòng khám nhỏ thường gửi mẫu ra lab ngoài; LIS đầy đủ quá tầm. | Nội bộ |
| TAT trả kết quả (turnaround time) | THIẾU | BI | Cần mốc `ordered_at` → `resulted_at`. | Nội bộ |

#### Domain 4 — Dược / Kho
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Xuất – Nhập – Tồn kho | THIẾU | QT | **Chưa có báo cáo kho trong 23 BC.** Cần descriptor tổng hợp nhập/xuất/tồn theo kỳ. | Nội bộ / kế toán kho |
| Thẻ kho theo lô / HSD | THIẾU | QT | Chi tiết biến động 1 mặt hàng theo lô, HSD. | Nội bộ |
| Thuốc cận date / hết hạn | THIẾU | QT | Cảnh báo HSD ≤ N ngày. Descriptor lọc `expiry_date`. | Nội bộ |
| Biên bản kiểm kê kho | THIẾU | QT | Đối chiếu sổ sách vs thực tế. Biểu mẫu định kỳ. | Nội bộ / Luật Kế toán |
| Danh mục thuốc BHYT | THIẾU (có điều kiện) | PL | Chỉ khi phòng khám có hợp đồng BHYT. Xem §7.E (danh mục theo TT hiện hành). | TT danh mục thuốc BHYT (cần xác minh số hiệu mới nhất) |
| **Báo cáo thuốc gây nghiện / hướng thần / tiền chất** | THIẾU (có điều kiện) | PL | Bắt buộc **nếu phòng khám có kê/cấp phát thuốc kiểm soát đặc biệt** — báo cáo định kỳ (hạn 15/01) + đột xuất (48h). Phòng khám ĐTĐ thường ít dùng, nhưng nếu có thì đây là **rủi ro compliance cao**. | TT 20/2017/TT-BYT |
| Thống kê sử dụng kháng sinh | THIẾU | BI | Giám sát AMR. Không bắt buộc với phòng khám nhỏ. | Nội bộ / khuyến cáo |

#### Domain 5 — Thu ngân / Tài chính
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Doanh thu ngày/tháng | ĐÃ CÓ | QT | `revenue-daily` (A1). Cần bổ sung tổng hợp theo tháng (group `MONTH`). | Nội bộ |
| Doanh thu theo DV / BS / quầy | ĐÃ CÓ (một phần) | QT/BI | A1 group theo người thu + filter quầy; theo DV có `fee-detail` (A5), `lab-summary` (A6); theo BS có `luot-kham-theo-bs` (D1). | Nội bộ |
| Hoàn / hủy phiếu | ĐÃ CÓ | QT | `refund-receipts` (A2), `void-receipts` (A3). | Nội bộ |
| Tạm ứng | ĐÃ CÓ (khung) | QT | `advances` (A4) — **descriptor có nhưng chưa có bảng dữ liệu**, luôn trả rỗng (contract §6.3). | Nội bộ |
| Công nợ bệnh nhân | THIẾU | QT | BN nợ (chênh lệch > 0, §5.3). Descriptor lọc phiếu còn nợ. | Nội bộ |
| **Sổ quỹ tiền mặt** | THIẾU | PL | Sổ thu–chi tồn quỹ theo ngày. Bắt buộc theo Luật Kế toán nếu có thu tiền mặt (gần như luôn có). | Luật Kế toán 2015 |
| **Hóa đơn điện tử** | THIẾU (có điều kiện) | PL | Bắt buộc phát hành HĐĐT. **Tuy nhiên đây là chức năng phát hành/kết nối cơ quan thuế**, không thuần túy là báo cáo in — cần tích hợp nhà cung cấp HĐĐT (ngoài phạm vi Report Engine). Báo cáo bảng kê HĐĐT đã phát hành thì thuộc phạm vi. | NĐ 123/2020, TT 78/2021 |
| Đối soát POS / ngân hàng | THIEU | QT | Đối chiếu tiền `card`/`bankTransfer` vs sao kê. | Nội bộ |

#### Domain 6 — BHYT
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Export XML giám định | THIẾU (có điều kiện) | PL | Bắt buộc **nếu có hợp đồng KCB BHYT**. Thuộc module `Bhyt.ExportService` (CLAUDE.md §5), không thuần Report Engine. Xem §7.E về chuẩn XML. | QĐ 130/2023, sửa đổi (cần xác minh) |
| Bảng kê chi phí KCB BHYT | THIẾU (có điều kiện) | PL | Bảng kê chi phí từng lượt BHYT (mẫu theo QĐ hiện hành). | Xem §7.E (mẫu 79a/80a cần xác minh) |
| Thống kê thanh toán BHYT | THIẾU (có điều kiện) | PL/QT | Tổng hợp đề nghị thanh toán theo kỳ. | Nội bộ + BHYT |
| DS từ chối giám định | THIẾU (có điều kiện) | QT | Hồ sơ bị cổng giám định từ chối. | Nội bộ |
| Thống kê đúng / trái tuyến | THIẾU (có điều kiện) | QT | | TT 40/2015 (cần xác minh còn hiệu lực) |
| Giấy chuyển tuyến | THIẾU (có điều kiện) | PL | Biểu mẫu cấp phát khi chuyển tuyến. | TT 40/2015/TT-BYT (cần xác minh) |

> **Ghi chú domain BHYT:** toàn bộ chỉ áp dụng khi tenant **ký hợp đồng KCB BHYT**. Nhiều phòng khám ĐTĐ tư nhân hoạt động thu phí dịch vụ (không BHYT) → cả cụm này là **bật theo cấu hình tenant**, không mặc định P0 cho mọi tenant.

#### Domain 7 — BHXH / Pháp lý
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| GCN nghỉ việc hưởng BHXH (C65-HD) | THIẾU (khung) | PL | `nghi-huong-bhxh` (E2) — **descriptor có nhưng chưa có bảng** `diab_his_cli_sick_leaves` (contract §6.8). Bắt buộc nếu cấp GCN nghỉ. | TT 56/2017/TT-BYT (số mẫu cần xác minh, §7.E) |
| Giấy ra viện | KHÔNG ÁP DỤNG | PL | Ngoại trú, xem Domain 2. | TT 56/2017 |
| Giấy chứng sinh | KHÔNG ÁP DỤNG | PL | Không có sản khoa. | TT 56/2017 |
| Đơn thuốc in | ĐÃ CÓ | PL | Xem Domain 2. | TT 27/2021 |

#### Domain 8 — BI nội bộ
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| KPI bác sĩ | ĐÃ CÓ | BI | `luot-kham-theo-bs` (D1) + `DoctorKpiResponse`. | Nội bộ |
| Năng suất phòng khám | ĐÃ CÓ | BI | `luot-kham-theo-pk` (D2). | Nội bộ |
| Tỷ lệ tái khám / chuyển tuyến | THIẾU | BI | `patient_source=RETURN` đã có nền; cần descriptor tỷ lệ. | Nội bộ |
| Dashboard điều hành | THIẾU | BI | Recharts/Tremor (CLAUDE.md). Màn hình tương tác, ngoài Report Catalog in ấn. | Nội bộ |
| Top thuốc / dịch vụ | THIẾU | BI | Descriptor group theo thuốc/DV, sort DESC. | Nội bộ |
| Dự báo tồn kho | THIẾU | BI | Cần dữ liệu kho (Domain 4) trước. | Nội bộ |

#### Domain 9 — Thống kê BYT định kỳ
| Báo cáo thị trường | Trạng thái | Loại | Map / Ghi chú | Căn cứ |
|---|---|---|---|---|
| Báo cáo thống kê y tế định kỳ | THIẾU | PL | Cơ sở y tế tư nhân có nghĩa vụ báo cáo hoạt động KCB định kỳ. Phần lớn dựa trên ICD-10 + lượt khám (đã có nền). | TT 37/2019/TT-BYT (cần xác minh) |
| Chỉ tiêu thống kê ngành y tế | THIẾU | PL | Bộ chỉ tiêu tổng hợp. | TT 20/2019/TT-BYT (cần xác minh) |
| Báo cáo bệnh truyền nhiễm | THIẾU (có điều kiện) | PL | Xem Domain 2. | TT 54/2015 |
| Báo cáo hoạt động KCB tư nhân | THIẾU | PL | Báo cáo định kỳ gửi Sở Y tế. | Cần xác minh văn bản |

### 7.B. Danh sách KHOẢNG TRỐNG ưu tiên

#### P0 — Bắt buộc pháp lý còn thiếu (rủi ro compliance)
Đánh giá theo **phòng khám tư quy mô nhỏ, trọng tâm ĐTĐ**. Không phải mọi mục "bắt buộc pháp lý" của bệnh viện lớn đều là P0 tại đây — nhiều mục là **bắt buộc CÓ ĐIỀU KIỆN** (chỉ khi phát sinh dịch vụ tương ứng).

| # | Khoảng trống | Vì sao P0 (hay điều kiện) | Cách làm |
|---|---|---|---|
| P0-1 | **Sổ quỹ tiền mặt** | Gần như **mọi** phòng khám đều thu tiền mặt → Luật Kế toán yêu cầu sổ quỹ. Không điều kiện. Rủi ro thanh/kiểm tra. | Report Engine descriptor: nguồn `diab_his_bil_payments` (thu) + bảng chi (**có thể thiếu — xem bên dưới**). Cần bảng phiếu chi `diab_his_bil_cash_out` (id, tenant_id, amount, reason, paid_to, created_at, created_by) để sổ quỹ đủ 2 chiều thu–chi. Nếu tạm thời chỉ có thu → descriptor một chiều trước. |
| P0-2 | **Thống kê ICD-10 / mô hình bệnh tật** | Báo cáo thống kê y tế định kỳ gửi cơ quan quản lý; dữ liệu chẩn đoán đã có sẵn (`diab_his_enc_diagnoses`). Chi phí thấp, giá trị compliance cao. | **Report Engine descriptor thuần** — group theo `icd10_code`, đếm lượt, %. Không cần bảng mới. |
| P0-3 | **Báo cáo thuốc kiểm soát đặc biệt** (gây nghiện/hướng thần/tiền chất) | **Bắt buộc CÓ ĐIỀU KIỆN**: chỉ khi phòng khám kê/cấp phát nhóm này. Nếu có → rủi ro rất cao (hạn 15/01 + đột xuất 48h). Phòng khám ĐTĐ thường không dùng, nhưng cần cờ cấu hình. | Cần **bảng/schema mới**: `diab_his_pha_controlled_substances` (danh mục thuốc + phân loại `control_class`: NARCOTIC/PSYCHOTROPIC/PRECURSOR) + sổ theo dõi nhập–xuất–tồn riêng theo lô. Descriptor báo cáo sinh từ đó. Bật theo cờ tenant `has_controlled_drugs`. |
| P0-4 | **Bảng kê chi phí KCB BHYT + XML giám định** | **Bắt buộc CÓ ĐIỀU KIỆN**: chỉ khi tenant ký hợp đồng BHYT. Khi có → không thể thanh toán BHYT nếu thiếu. Thuộc module `Bhyt.ExportService`, không thuần Report Engine. | Cần schema BHYT (mapping chi phí → XML 4210/theo QĐ hiện hành) + bảng kê. **Xác minh chuẩn XML mới nhất trước (§7.E).** |
| P0-5 | **GCN nghỉ việc hưởng BHXH (E2)** | **Bắt buộc CÓ ĐIỀU KIỆN**: chỉ khi bác sĩ cấp GCN nghỉ. Descriptor E2 đã đăng ký nhưng luôn rỗng do thiếu bảng. | Cần **bảng mới** `diab_his_cli_sick_leaves` (đã gợi ý ở contract §6.8). Sau đó E2 chạy thật + thêm biểu mẫu in C65-HD. |

**Không xếp P0 cho phòng khám nhỏ (giải thích):**
- *Giấy ra viện / chứng sinh*: KHÔNG ÁP DỤNG — ngoại trú, không nội trú/sản khoa.
- *Giấy khám sức khỏe*: chỉ P0 nếu phòng khám có dịch vụ cấp GKSK; là biểu mẫu, không phải báo cáo tổng hợp.
- *Hóa đơn điện tử*: bắt buộc pháp lý nhưng là **chức năng phát hành/kết nối thuế** (tích hợp nhà cung cấp HĐĐT), không nằm trong Report Engine. Chỉ phần "bảng kê HĐĐT đã phát hành" mới thuộc catalog. Tách thành hạng mục tích hợp riêng.

#### P1 — Quản trị / vận hành giá trị cao
| # | Khoảng trống | Giá trị | Cách làm |
|---|---|---|---|
| P1-1 | **Xuất – Nhập – Tồn kho** + **thẻ kho theo lô/HSD** | Kho dược là nghiệp vụ lõi (CLAUDE.md module Pharmacy) nhưng **0 báo cáo kho** trong 23 BC. | Report Engine descriptors trên các bảng kho (`diab_his_pha_*`). **Cần kiểm tra schema kho hiện có**: nhập/xuất/lô/tồn. Nếu thiếu → bổ sung bảng phiếu nhập/xuất + thẻ kho. |
| P1-2 | **Thuốc cận date / hết hạn** | Tránh thất thoát, an toàn dùng thuốc. | Descriptor lọc `expiry_date <= today + N`. Phụ thuộc P1-1 (bảng lô/HSD). |
| P1-3 | **Công nợ bệnh nhân** | Theo dõi phiếu còn nợ (chênh lệch > 0, §5.3). Đã có công thức. | Report Engine descriptor thuần trên `diab_his_bil_billing`. Không cần bảng mới. |
| P1-4 | **Sổ đăng ký tiếp đón** | Chứng từ tiếp đón theo ngày; khác sổ khám C1. | Descriptor trên bảng reception/encounter (`reception_at`). Cần xác nhận có cột thời điểm tiếp đón. |
| P1-5 | **Thống kê chỉ định CLS** | Giám sát chỉ định XN/CĐHA theo BS/kỳ. | Descriptor group `modality`/nhóm XN trên `cli_rad_orders` + `cli_lab_orders`. Không cần bảng mới. |
| P1-6 | **Top thuốc / dịch vụ** | Ra quyết định nhập hàng, marketing. | Descriptor group theo thuốc/DV, sort DESC, trên `billing_items` + đơn thuốc. Không cần bảng mới. |
| P1-7 | **Doanh thu tổng hợp theo tháng** | A1 chỉ theo ngày; quản lý cần view tháng. | Mở rộng descriptor A1 (group `MONTH`). Không cần bảng mới. |
| P1-8 | **Kiểm kê kho (biên bản)** | Đối chiếu định kỳ. | Phụ thuộc P1-1; cần bảng phiên kiểm kê `diab_his_pha_stocktakes`. |

> *Ghi chú P1:* "Danh sách chờ" (live queue) và "Dashboard điều hành" là **màn hình tương tác realtime**, không phải báo cáo in — nên thuộc module Reception/BI, đưa ra khỏi Report Catalog nhưng vẫn ghi nhận là khoảng trống sản phẩm.

#### P2 — BI / nâng cao
| # | Khoảng trống | Cách làm |
|---|---|---|
| P2-1 | Tỷ lệ no-show | Cần trạng thái lịch hẹn (booked→arrived); phụ thuộc module đặt hẹn. |
| P2-2 | TAT trả kết quả CLS | Cần mốc `ordered_at`/`resulted_at`; descriptor tính chênh lệch. |
| P2-3 | Dự báo tồn kho | Phụ thuộc P1-1; mô hình đơn giản (tốc độ tiêu thụ). |
| P2-4 | Phễu chuyển đổi (đăng ký→khám→CLS→thu) | Descriptor đa bước; cần dữ liệu reception. |
| P2-5 | Thống kê sử dụng kháng sinh | Descriptor lọc nhóm thuốc kháng sinh trên đơn thuốc. |
| P2-6 | Tỷ lệ tái khám / chuyển tuyến | Dựa `patient_source=RETURN`; descriptor tỷ lệ. |
| P2-7 | Thống kê lượt theo giờ (peak hour) | Descriptor group `HOUR()`. |

### 7.C. Tận dụng Report Engine vs cần schema mới (tổng hợp)

**Làm ngay bằng Report Engine descriptor (KHÔNG cần bảng mới)** — chi phí thấp, ưu tiên gặt trước:
- P0-2 Thống kê ICD-10 · P1-3 Công nợ · P1-5 Thống kê chỉ định CLS · P1-6 Top thuốc/DV · P1-7 Doanh thu theo tháng · P2-6 Tái khám · P2-7 Peak hour · Thống kê nguồn BN (Domain 1).
- Điều kiện: dữ liệu nguồn đã tồn tại trong `enc_*` / `bil_*` / `cli_*` hiện hành.

**Cần BẢNG / SCHEMA MỚI trước khi làm báo cáo:**
| Khoảng trống | Bảng gợi ý (tận dụng đề xuất research) |
|---|---|
| P0-1 Sổ quỹ tiền mặt (chiều chi) | `diab_his_bil_cash_out` (id, tenant_id, amount, reason, paid_to, created_at, created_by) |
| P0-3 Thuốc kiểm soát đặc biệt | `diab_his_pha_controlled_substances` (+ `control_class`) + sổ nhập/xuất/tồn theo lô; cờ tenant `has_controlled_drugs` |
| P0-4 BHYT | Schema mapping chi phí→XML + bảng kê chi phí KCB BHYT (theo chuẩn xác minh §7.E) |
| P0-5 GCN nghỉ BHXH (E2) | `diab_his_cli_sick_leaves` (đã gợi ý contract §6.8) |
| P1-1/P1-2/P1-8 Kho | Kiểm tra `diab_his_pha_*`; nếu thiếu: phiếu nhập/xuất + thẻ kho theo lô/HSD + `diab_his_pha_stocktakes` |
| P1-4 Sổ tiếp đón | Xác nhận cột `reception_at` / bảng reception |
| A4 Tạm ứng (đã có descriptor, thiếu bảng) | `diab_his_bil_advances` (đã gợi ý contract §6.3) |

> **Gợi ý gộp biểu mẫu pháp lý (từ research):** các giấy tờ pháp lý dạng cấp phát (GCN nghỉ BHXH, giấy chuyển tuyến, GKSK, giấy ra viện nếu cần) có thể gộp **một bảng `diab_his_cli_certificates`** với cột phân loại `certificate_type` (SICK_LEAVE / REFERRAL / HEALTH_CHECK / DISCHARGE …) thay vì mỗi loại một bảng — giảm số migration, dễ mở rộng. Cân nhắc thay cho việc tạo riêng `diab_his_cli_sick_leaves` nếu roadmap sẽ cần nhiều loại giấy.

### 7.D. Khuyến nghị roadmap (đợt tiếp theo)

Nguyên tắc: **ưu tiên compliance pháp lý + nghiệp vụ kho lõi trước BI nâng cao.** Phần lớn P0 quan trọng hơn thêm báo cáo BI. Thứ tự đề xuất (nối tiếp roadmap §3):

- **Đợt 7 — "Gặt nhanh" (descriptor thuần, không schema mới):** P0-2 ICD-10, P1-3 Công nợ, P1-5 Chỉ định CLS, P1-6 Top thuốc/DV, P1-7 Doanh thu tháng. Giá trị cao, rủi ro thấp, tái dùng Report Engine 100%.
- **Đợt 8 — Kho dược (nền tảng còn trống):** P1-1 XNT + thẻ kho theo lô/HSD, P1-2 cận date, P1-8 kiểm kê. Cần audit schema `diab_his_pha_*` trước; đây là khoảng trống module lõi lớn nhất.
- **Đợt 9 — Compliance tài chính:** P0-1 Sổ quỹ tiền mặt (+ bảng phiếu chi); tách hạng mục tích hợp HĐĐT thành sprint riêng (không thuộc Report Engine).
- **Đợt 10 — Cụm có điều kiện (bật theo cấu hình tenant):** P0-4 BHYT (nếu có hợp đồng), P0-5 GCN nghỉ BHXH (bảng `sick_leaves`/`certificates`), P0-3 thuốc kiểm soát đặc biệt (nếu có kê nhóm này). Làm khi tenant thực tế bật cờ tương ứng — **xác minh pháp lý §7.E trước.**
- **Đợt 11 — BI nâng cao:** P2 (no-show, TAT, dự báo tồn kho, phễu chuyển đổi, kháng sinh) + dashboard điều hành realtime.

### 7.E. Cần xác minh pháp lý (trước khi implement)

Các điểm dưới đây **PO không tự quyết** — cần đội ngũ (hoặc research/legal) tra cứu văn bản gốc còn hiệu lực **trước khi** đặc tả chi tiết/implement, tránh làm theo mẫu đã hết hiệu lực:

1. **Chuẩn XML giám định BHYT mới nhất**: research nêu QĐ 130/2023 + sửa đổi QĐ 4750/2023 + QĐ 3176/2024, trong khi CLAUDE.md §5 ghi QĐ 4750. Cần chốt **chuẩn XML + bộ mã đang áp dụng tại thời điểm go-live** (2026).
2. **Mẫu bảng kê chi phí BHYT 79a/80a**: research cảnh báo đây **có thể là bản cũ**. Xác minh mẫu bảng kê hiện hành thay thế 79a/80a-HD.
3. **Số hiệu mẫu GCN nghỉ việc hưởng BHXH (C65-HD1/HD2)**: PRD §5.4 dùng C65-HD1 (TT 56/2017). Research lưu **số hiệu C65-HD có thể là QĐ của BHXH Việt Nam, không phải TT BYT** — xác minh cơ quan ban hành + mẫu hiện hành.
4. **Danh mục thuốc BHYT**: xác minh **số hiệu TT danh mục thuốc BHYT mới nhất** (thường thay đổi theo năm) trước khi làm báo cáo danh mục/đối chiếu.
5. **Sổ khám bệnh & sổ CĐHA**: xác minh TT 52/2017 (hồ sơ bệnh án) và TT 50/2017 (CĐHA) **còn hiệu lực / có thay thế** cho biểu mẫu sổ.
6. **Thống kê y tế định kỳ**: xác minh TT 37/2019 + TT 20/2019 về biểu mẫu/chỉ tiêu thống kê và **kỳ báo cáo, nơi nộp** áp dụng cho cơ sở tư nhân.
7. **Giấy khám sức khỏe**: TT 14/2013/TTLT có thể đã được thay thế — xác minh biểu mẫu GKSK hiện hành nếu triển khai.
8. **Giấy chuyển tuyến / đúng-trái tuyến (TT 40/2015)**: xác minh còn hiệu lực sau các thay đổi chính sách thông tuyến gần đây.

> Sau khi có kết luận xác minh, cập nhật lại căn cứ pháp lý trong bảng §7.A và các đặc tả bảng schema tương ứng.
