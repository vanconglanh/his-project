# PRD: Audit & Cải thiện Module Dược (Pharmacy)

> Tác giả: Đăng (PO/BA) — Ngày: 2026-05-26
> Phạm vi: Phòng khám đa khoa 2-5 bác sĩ, multi-tenant SaaS.
> Tham chiếu: CLAUDE.md §4, §5; TT 27/2021/TT-BYT; QĐ 4750/QĐ-BYT; docs/design/research-his-ui-patterns.md.

---

## Phần 1 — Audit hiện trạng

### 1.1 Đang có gì (đã implement)

**Schema / Migration**
- `pha_drug_master` mở rộng: `atc_code`, `form`, `requires_prescription`, `is_psychotropic`, `is_narcotic`, `dtqg_drug_code`, `price`, `generic_name`, `status` (0036).
- `pha_stocks` mở rộng: `batch_no`, `expiry_date`, `manufacture_date`, `gtin`, `reorder_level`, `quantity_available`, `quantity_reserved` (0013 + 0038).
- Lịch sử biến động kho `diab_his_pha_stock_movements` đầy đủ 5 loại (IMPORT/EXPORT/TRANSFER/ADJUST/RETURN) — 0013.
- Đơn thuốc mở rộng state machine: `DRAFT → SIGNED → SUBMITTED_DTQG → DISPENSED/PARTIAL/CANCELLED`, kèm `dtqg_code`, `dtqg_status`, `signed_at`, `signature_data` (0035).
- Chi tiết kê đơn `diab_his_pha_prescription_items` (dosage, frequency, route, duration, batch_dispensed JSON) — 0035.
- Bảng quy tắc tương tác thuốc `diab_his_pha_ddi_rules` (4 mức severity) — 0035.
- Lịch sử in đơn `diab_his_pha_prescription_print_history` — 0035.
- Quản lý NCC + Đặt hàng + Nhập kho: `diab_his_pha_suppliers`, `purchase_orders`, `purchase_order_items`, `grn` — 0037.
- Phát thuốc: `diab_his_pha_dispense_records` + `dispense_items` (gắn batch_no, expiry_date, FEFO pick) — 0038.

**Backend (.NET 8)**
- Interface `IFefoStrategy` + impl FEFO (earliest expiry first) — pick lô tự động khi phát.
- Interface `IDdiChecker` + impl kiểm tra DDI theo cặp drug_id.
- Interface `IUsbTokenSigner` verify PKCS#7 (mock).
- Interface `IDtqgClient` submit/getStatus/cancel/ping ĐTQG (mock).
- Interface `IDtqgQrGenerator` (QRCoder) sinh QR PNG từ `ma_don_thuoc`.
- Interface `IExcelImporter` import drug master từ Excel.
- Interface `ICucQldLienThong` báo nhập/xuất Cục QLD (mock).
- Handlers: `PrescriptionHandlers`, `DispensingHandlers`, `DrugHandlers`, `WarehouseHandlers`, `DtqgHandlers`.

**Frontend (Next.js 15)**
- `/pharmacy` 5 tab: Tồn kho, Nhập kho, Phát thuốc, Kiểm kê, Cảnh báo.
- AlertsTab: lọc HSD 30/60/90 ngày + low-stock (đã có UI).
- `/prescriptions` list + detail, `/drugs` danh mục.

**Seed dev**
- 0063 seed 1 warehouse WH01, 2 lô bình thường/thuốc, 5 lô near-expiry, 3 lô low-stock — phục vụ test alert.

---

### 1.2 Đang thiếu / kém (gap khiến phòng khám không dùng được production)

| # | Gap | Mức độ | Nghiệp vụ ảnh hưởng |
|---|-----|--------|---------------------|
| G1 | **Chống chỉ định dị ứng / thai kỳ / suy gan-thận**: chưa có bảng `patient_allergies`, chưa có `pregnancy_category` check khi kê đơn. DDI chỉ check drug-drug, KHÔNG check drug-patient. | P0 | TT 52/2017/TT-BYT yêu cầu BS phải biết tiền sử dị ứng trước khi kê. Rủi ro pháp lý + an toàn BN. |
| G2 | **Đối chiếu kê đơn ↔ xuất kho ↔ thanh toán**: chưa có view/report cross-check 3 chiều. `dispense_record` có `total_amount` nhưng không link `cashier_invoice_id` → khó truy soát thất thoát. | P0 | Phòng khám không biết được "đơn nào đã phát mà chưa thu tiền". |
| G3 | **Tự động đề xuất Purchase Order từ low-stock**: có alert nhưng phải tạo PO thủ công. Cần wizard "Sinh PO từ danh sách thuốc dưới min" gom theo NCC ưu tiên. | P0 | Dược sĩ phải copy tay → sai sót, chậm. |
| G4 | **Báo cáo BI**: chưa có dashboard top thuốc bán chạy, doanh thu thuốc theo ngày/tháng, công nợ NCC, tỷ lệ phát/đơn, slow-moving stock. | P1 | Chủ phòng khám không có data ra quyết định nhập hàng. |
| G5 | **Sổ thuốc gây nghiện / hướng thần** theo TT 20/2017/TT-BYT (sổ riêng, ký xác nhận từng lần xuất, báo cáo định kỳ Sở Y tế). Hiện chỉ flag `is_narcotic` mà chưa có sổ. | P1 | Vi phạm pháp lý khi thanh tra. |
| G6 | **Kiểm kê (stocktake)**: tab "Kiểm kê" có UI nhưng chưa thấy bảng `stocktake_session`/`stocktake_lines` để chốt số liệu + sinh phiếu ADJUST tự động. | P1 | Không thể đối chiếu kho thực tế. |
| G7 | **Trả thuốc từ BN** (return): `dispense_items.is_returned` có flag nhưng workflow `RETURN` chưa rõ — chưa có lý do, chưa hoàn kho lô gốc. | P2 | Hiếm nhưng cần khi BN dị ứng. |
| G8 | **Tích hợp ĐTQG thực**: hiện toàn mock. Cần config token thật, retry queue khi portal down, lưu raw request/response để giám định. | P1 | Vi phạm TT 27/2021 nếu không liên thông. |
| G9 | **Cảnh báo trùng hoạt chất**: BS có thể kê 2 thuốc cùng `generic_name` khác biệt thương mại — chưa cảnh báo. | P1 | An toàn BN (overdose paracetamol). |
| G10 | **Cảnh báo liều theo cân nặng/tuổi (nhi)**: chưa có `max_dose_per_kg` ở drug_master. | P2 | Chủ yếu cho khoa Nhi, scope hẹp. |
| G11 | **In nhãn lô + barcode** khi phát thuốc lẻ: chưa có template. | P2 | Tiện lợi cho BN. |

### 1.3 Nguy cơ pháp lý

- **TT 27/2021/TT-BYT**: bắt buộc liên thông ĐTQG với thuốc kháng sinh/kiểm soát đặc biệt. Hiện code chỉ mock → không deploy production được.
- **TT 52/2017/TT-BYT** (kê đơn ngoại trú): bắt buộc khai báo tiền sử dị ứng trong đơn. Hiện không có field.
- **TT 20/2017/TT-BYT** (thuốc gây nghiện/hướng thần): bắt buộc sổ theo dõi xuất nhập riêng, có chữ ký.
- **NĐ 117/2020/NĐ-CP**: lưu trữ đơn thuốc tối thiểu 2 năm — cần kiểm tra retention policy.

---

## Phần 2 — Đề xuất cải thiện (Top 3 P0)

### US-PHA-01: Cảnh báo chống chỉ định dị ứng & thai kỳ khi kê đơn
**Là** bác sĩ **tôi muốn** hệ thống tự cảnh báo khi tôi kê thuốc mà bệnh nhân có tiền sử dị ứng hoặc thuộc nhóm chống chỉ định (thai kỳ, suy gan/thận) **để** tránh tai biến và đảm bảo tuân thủ TT 52/2017/TT-BYT.

**Acceptance Criteria:**
- [ ] **Given** BN có dị ứng `Penicillin` (lưu ở `diab_his_pat_allergies`), **when** BS chọn thuốc `Amoxicillin 500mg` trong form kê đơn, **then** UI hiển thị banner ĐỎ "Chống chỉ định: BN dị ứng Penicillin" và disable nút Lưu cho đến khi BS check "Tôi xác nhận tiếp tục" + nhập lý do.
- [ ] **Given** BN nữ đang mang thai (cờ `is_pregnant=1`), **when** BS chọn thuốc có `pregnancy_category IN ('D','X')`, **then** hiển thị cảnh báo CAM + log vào `pha_prescription_warnings_acknowledged`.
- [ ] **Given** BN có `egfr < 30 ml/phút`, **when** BS chọn `Metformin`, **then** hiển thị cảnh báo VÀNG "Suy thận nặng — cân nhắc giảm liều".
- [ ] Tất cả cảnh báo phải lưu audit: user_id, prescription_id, drug_id, warning_type, acknowledged_at, reason.
- [ ] API trả về < 300ms cho danh sách thuốc <= 10.

**API phụ thuộc:**
- `GET /api/v1/patients/{id}/allergies`
- `GET /api/v1/patients/{id}/clinical-flags` (pregnancy, eGFR, liver)
- `POST /api/v1/prescriptions/check-contraindications` body `{patient_id, drug_ids[]}`
- `POST /api/v1/prescriptions/{id}/warnings/acknowledge`

**UI:** `/prescriptions/new` — banner cảnh báo trong drug picker, modal xác nhận khi override.
**Ưu tiên:** P0
**Effort:** L (cần migration `diab_his_pat_allergies`, `diab_his_pha_prescription_warnings`, BE logic, FE banner).

---

### US-PHA-02: Tự động sinh Purchase Order từ thuốc dưới mức tồn tối thiểu
**Là** dược sĩ **tôi muốn** một nút "Tạo PO tự động từ thuốc tồn dưới min" **để** không phải copy tay danh sách 30-50 thuốc mỗi tuần.

**Acceptance Criteria:**
- [ ] **Given** có ≥ 1 thuốc với `SUM(quantity_available) < reorder_level`, **when** dược sĩ vào `/pharmacy` tab "Cảnh báo" và bấm "Sinh PO tự động", **then** hệ thống mở modal hiển thị bảng: thuốc | tồn hiện tại | reorder_level | đề xuất số lượng đặt | NCC ưu tiên (NCC gần nhất đã giao thành công).
- [ ] **Given** dược sĩ chỉnh sửa số lượng / NCC, **when** bấm "Tạo phiếu", **then** hệ thống nhóm thuốc theo NCC → tạo nhiều `purchase_orders` với `status='DRAFT'`, mỗi PO có order_no `PO-YYYYMMDD-NNN`.
- [ ] **Given** PO đã tạo, **when** xem `/pharmacy` tab "Nhập kho", **then** PO hiện ngay đầu danh sách với badge "Tự động".
- [ ] Đề xuất số lượng = `max((avg_consumption_30d * 30) - quantity_available, reorder_level * 2)`.
- [ ] Nếu thuốc chưa có lịch sử NCC, hiển thị "Chưa có NCC — vui lòng chọn" (không block, cho phép tạo PO không gán NCC = DRAFT).

**API phụ thuộc:**
- `GET /api/v1/pharmacy/reorder-suggestions`
- `POST /api/v1/pharmacy/purchase-orders/auto-generate` body `{lines: [{drug_id, quantity, supplier_id}]}`

**UI:** `/pharmacy` → AlertsTab → button "Sinh PO tự động" → modal `AutoPoModal`.
**Ưu tiên:** P0
**Effort:** M (BE logic + 1 modal FE; tận dụng bảng PO đã có).

---

### US-PHA-03: Đối chiếu kê đơn ↔ phát thuốc ↔ thu tiền (3-way reconciliation)
**Là** kế toán trưởng / chủ phòng khám **tôi muốn** xem một báo cáo đối chiếu 3 chiều giữa đơn đã kê, phiếu đã phát, và hóa đơn đã thu **để** phát hiện thất thoát doanh thu thuốc.

**Acceptance Criteria:**
- [ ] **Given** trong khoảng ngày X→Y, **when** vào `/reports/pharmacy-reconciliation?from=X&to=Y`, **then** hệ thống hiển thị bảng: mã đơn | BN | BS | giá trị đơn | đã phát (y/n + thời gian) | đã thu (y/n + số HĐ) | chênh lệch.
- [ ] **Given** đơn `SIGNED` quá 24h chưa phát, **then** badge VÀNG "Chậm phát".
- [ ] **Given** đơn `DISPENSED` nhưng chưa link `cashier_invoice_id`, **then** badge ĐỎ "Phát chưa thu" + cho phép kế toán bấm "Tạo HĐ".
- [ ] **Given** số lượng phát ≠ số lượng kê (PARTIAL), **then** hiển thị chi tiết từng thuốc thiếu.
- [ ] Export Excel với màu nền theo trạng thái.
- [ ] Quyền truy cập: chỉ `KeToan` + `Admin`. `BacSi`/`DuocSi` 403.

**API phụ thuộc:**
- `GET /api/v1/reports/pharmacy-reconciliation?from=&to=&status=`
- `POST /api/v1/cashier/invoices/from-dispense/{dispense_record_id}`

**UI:** trang mới `/reports/pharmacy-reconciliation/page.tsx`.
**Ưu tiên:** P0
**Effort:** M (cần thêm cột `cashier_invoice_id` vào `dispense_records`, view SQL join 3 bảng, FE table + export).

---

## 3. Out of scope sprint này

- Tích hợp Cục QLD GPP báo cáo định kỳ (G5).
- Báo cáo BI nâng cao (G4) — đã có ticket riêng.
- Drug-allergy cross-reference với database thương mại (Drugbank, Lexicomp).

## 4. Dependencies

- Module Patient cần expose `allergies`, `clinical-flags`.
- Module Cashier cần API `invoices/from-dispense`.
- Migration mới: `0064_create_patient_allergies.sql`, `0065_add_cashier_link_to_dispense.sql`, `0066_create_prescription_warnings.sql`.

## 5. Risks

- DDI/contraindication false-positive cao → BS bỏ qua → giảm hiệu quả. **Mitigation:** chỉ block với severity `CONTRAINDICATED`, các mức khác chỉ cảnh báo.
- Auto-PO sinh sai lượng → ứ đọng vốn. **Mitigation:** luôn `DRAFT`, cần dược sĩ duyệt trước khi `SENT`.
