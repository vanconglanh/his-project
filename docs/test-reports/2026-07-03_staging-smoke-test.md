# Checklist Smoke-Test Staging — Đợt chuẩn hóa UI/UX + Report (2026-07-03)

> Mục đích: kiểm chứng RUNTIME các thay đổi phiên 2026-07-02 → 07-03 mà máy dev không chạy được (thiếu Docker/DB sạch).
> Người chạy: DevOps (Chương) / Tester (Phượng) trên **staging**. Đánh dấu ✅ Pass / ❌ Fail (kèm ảnh + log).
> Phạm vi commit liên quan: `13af01e` → `b94673e` (baseline khôi phục → F1–F9, report endpoints, F-01/F-03, token).

---

## 0. Chuẩn bị môi trường

- [ ] `docker compose up -d` (MySQL + Redis + MinIO + API + FE) — hoặc deploy staging theo `deploy.sh`.
- [ ] Áp đủ migration `db/migrations/` tới bản mới nhất (đặc biệt `0065_add_letterhead_fields.sql`).
- [ ] Seed dữ liệu demo có: ≥1 tenant (có `logo_url`, `cskcb_code`, `address`, `phone`), bệnh nhân, lượt khám, đơn thuốc, hóa đơn FINALIZED, tồn kho, export BHYT.
- [ ] Đăng nhập được bằng tài khoản có quyền xem báo cáo + thu ngân + kho dược.
- [ ] Xác nhận API **KHỞI ĐỘNG SẠCH** (log không có lỗi kết nối DB / DI). Đây đã là 1 test — code report mới thay đổi DI.

---

## Nhóm A — RỦI RO CAO NHẤT: 8 report endpoint mới (F7)

> Đã verify tĩnh: SQL đúng `ONLY_FULL_GROUP_BY` + tên cột khớp schema. **Chưa verify: số liệu đúng + Dapper map kiểu (decimal/DateOnly).** Đây là nhóm phải test kỹ nhất.
> Cách test nhanh: mở Swagger (`/swagger`) hoặc các tab màn Báo cáo, gọi từng endpoint với khoảng ngày có dữ liệu.

- [ ] `GET /reports/revenue/by-service` — trả danh sách dịch vụ + doanh thu + %, KHÔNG gồm thuốc (item_type=DRUG). Tổng % ≈ 100.
- [ ] `GET /reports/revenue/by-payment-method` — field JSON là `value` (KHÔNG phải `amount`); chart doanh thu theo phương thức hiển thị đúng, không vỡ.
- [ ] `GET /reports/cashier/daily-summary?date=&cashierId=` — opening/closing balance khớp ca, `total_invoices` = số hóa đơn distinct.
- [ ] `GET /reports/debts/aging` — bucket tuổi nợ (0-30/31-60/…) khớp `payment_due_date`; chỉ gồm hóa đơn còn `balance>0`.
- [ ] `GET /reports/bhyt/summary` — số liệu từ `diab_his_int_bhyt_exports`, rejection rate tính đúng.
- [ ] `GET /reports/clinical/visits` — phân trang lượt khám + tên BN/BS + ICD10 chính; **kiểm tra không rò dữ liệu tenant khác** (multi-tenant).
- [ ] `GET /reports/clinical/icd10` — top chẩn đoán, phân trang.
- [ ] `GET /reports/pharmacy/inventory-value` — giá trị tồn theo `drug_category`, chỉ thuốc còn hạn (`exp_date>=CURDATE()`), field shape `{total_value,total_skus,by_category}`.
- [ ] **Không endpoint nào trả 500** (lỗi SQL runtime). Nếu 500 → xem log, phần lớn là type-mapping Dapper.

---

## Nhóm B — In ấn & Letterhead (F3, F4)

- [ ] **Xuất PDF báo cáo A4** (Financial/Clinical/Pharmacy) — letterhead có đủ: **logo + tên phòng khám + Mã CSKCB + địa chỉ + SĐT**. Mã CSKCB là trường MỚI thêm (F3), phải hiển thị.
- [ ] **In biên lai thu tiền** (F4 — đã sửa sai tên bảng `sys_tenants`/`sec_users`) — không lỗi runtime, hiện đúng tên phòng khám + người thu.
- [ ] **In hóa đơn** — tương tự, letterhead đầy đủ.
- [ ] Trang in trên màn (`/reports/print/[type]`, `/encounters/[id]/print`, `/encounters/[id]/cls-print`) — header teal, đủ thông tin, dễ đọc, số trang góc dưới.

---

## Nhóm C — Màu sắc & Token (F1, F2, F5, F8)

- [ ] **Bật LIGHT MODE** (toggle theme) — toàn bộ màn hiển thị đúng màu, KHÔNG bị tối/mất màu (F1 đã sửa token light-mode chết). Kiểm badge trạng thái, nền panel, chữ.
- [ ] **Dark mode** — vẫn đúng như trước (không regress).
- [ ] Chart (doanh thu, HbA1c, biến chứng, xu hướng khám…) — màu theo palette thống nhất, đổi đúng theo light/dark (F2/F5).
- [ ] Avatar bệnh nhân/người dùng — màu nền ổn định theo tên (F8 token, không đổi hành vi).

---

## Nhóm D — Luồng nhập liệu đã đổi (F-01, F-03)

**F-01 — Tạo bệnh nhân từ Tiếp đón:**
- [ ] Ở Tiếp đón, gõ tìm bệnh nhân → bấm "Tạo bệnh nhân mới" → chuyển sang trang `/patients/new` (đủ field, không phải dialog cũ).
- [ ] Trước khi chuyển: nhập dở phòng/lý do khám ở form check-in → sau khi tạo BN xong quay lại, **các field đó được khôi phục** (draft sessionStorage).
- [ ] Sau khi tạo BN xong → quay về Tiếp đón, bệnh nhân vừa tạo **được tự chọn sẵn** (auto-select).
- [ ] Bấm Hủy ở `/patients/new` → quay về Tiếp đón bình thường.

**F-03 — 3 form kho dược Fullpage:**
- [ ] Kho: "Tạo đơn đặt hàng" → mở trang `/pharmacy/purchase-orders/new`, thêm được **nhiều dòng thuốc**, lưu OK, quay về tab Kho.
- [ ] Trên 1 dòng PO: "Nhập kho" → mở `/pharmacy/grn/new?poId=...`, nhập nhiều lô, lưu OK. Vào thẳng `/pharmacy/grn/new` KHÔNG có poId → redirect + toast "Chọn đơn đặt hàng trước".
- [ ] "Tạo điều chỉnh tồn" (cả nút trong tab lẫn nút cấp trang) → mở `/pharmacy/adjustments/new`, thêm nhiều dòng, lưu OK.
- [ ] Ctrl+S lưu, Esc/nút quay lại hoạt động ở cả 3 trang.
- [ ] **Lưu ý hạn chế đã biết**: 3 form này còn nhập `drug_id` dạng UUID (chưa có Drug Picker — PRD `docs/prd/drug-picker-prd.md` đợt sau). Test bằng cách dán UUID thuốc hợp lệ.

---

## Nhóm E — Nhất quán layout (regression nhanh, đợt 2)

- [ ] Topbar cao vừa (56px), sidebar thu gọn 64px — không vỡ.
- [ ] Mọi Sheet/Drawer (admin users/tenants/roles/audit, labrad, nurse…) — nội dung không sát mép (có `px-6 pb-6`).
- [ ] Dialog kho dược/phân quyền chứa bảng — rộng thoải mái (`max-w-4xl`), không chật.
- [ ] Tiêu đề trang đồng đều một cỡ; card/panel bo góc đồng nhất.
- [ ] Bảng danh sách dài (bệnh nhân, khám, ICD-10) — mật độ gọn (dense).

---

## Nhóm F — Regression tổng (đảm bảo không vỡ cái đang chạy)

- [ ] Đăng nhập/đăng xuất, đổi mật khẩu.
- [ ] Tiếp đón → Khám → Kê đơn → Thu ngân → Cấp phát — luồng chính chạy suốt.
- [ ] Dashboard KPI + chart load đúng.
- [ ] Xuất BHYT XML, ký đơn thuốc (QR), phát hành hóa đơn điện tử.

---

## Ghi chú kết quả

| Nhóm | Pass/Fail | Ghi chú lỗi (kèm log/ảnh) |
|------|-----------|---------------------------|
| A — Report endpoints | | |
| B — In ấn & Letterhead | | |
| C — Màu & Token | | |
| D — F-01/F-03 | | |
| E — Layout | | |
| F — Regression | | |

**Ưu tiên fix nếu Fail:** Nhóm A (số liệu/500) → B (in ấn nghiệp vụ) → D (luồng mới) → C/E (thẩm mỹ) → F.
