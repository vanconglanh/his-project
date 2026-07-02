# PRD — Module Thu ngân (Cashier)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-05-31 · Version: 1.0
> Liên quan: CLAUDE.md §4 Cashier, BUG-CRUD-01 (timeout `/cashier/shift`)
> Cross-link: `docs/review/po-review-2026-05-31.md` §3 US-CASH-01

## 1. Mục tiêu
- Quản lý **ca thu ngân**: mở ca → thu tiền các bill → đóng ca với đối soát tiền mặt.
- Hỗ trợ 4 phương thức thanh toán: CASH, CARD, QR, BANK_TRANSFER.
- Báo cáo công nợ theo aging bucket (0-30, 31-60, 61-90, >90 ngày).
- Fix BUG-CRUD-01: endpoint `/cashier/shift` phải trả < 2s.

## 2. Personas
| Persona | Quyền |
|---|---|
| KeToan | CASH_OPEN_SHIFT, CASH_COLLECT, CASH_CLOSE_SHIFT, CASH_REPORT |
| Admin | CASH_* + force-close ca (sự cố) |
| LeTan | CASH_VIEW (xem bill chưa thu) |

## 3. Use cases
- UC-01: Mở ca (1 user — 1 ca active tại 1 thời điểm).
- UC-02: List bill chưa/đã thanh toán theo ngày + filter BN.
- UC-03: Thu tiền 1 bill (full hoặc partial).
- UC-04: Đóng ca với đối soát tiền mặt thực đếm.
- UC-05: In báo cáo ca (PDF).
- UC-06: Báo cáo công nợ aging.

## 4. User stories & AC

### US-CASH-01 — Mở ca
- AC-1: KT click "Mở ca" → modal nhập `opening_cash` (tiền mặt đầu ca, VND).
- AC-2: Validate: KT chưa có ca OPEN nào → tạo `cashier_shift` `status=OPEN`, `opened_at=now`, `opened_by=user`.
- AC-3: Nếu đã có ca OPEN → redirect vào ca đó, không cho mở mới.
- AC-4 (BUG-CRUD-01): `GET /api/v1/cashier/shift` (lấy ca active của user) trả < 2s p95. Bổ sung index `(tenant_id, opened_by, status, opened_at DESC)` trên `cashier_shifts`.

### US-CASH-02 — Thu tiền bill
- AC-1: Given có ca OPEN, When KT click "Thu" trên bill → modal: `amount`, `method` (CASH/CARD/QR/BANK_TRANSFER), `reference` (mã giao dịch nếu non-cash), `note`.
- AC-2: Validate: `amount ≤ bill.outstanding`, `amount > 0`. Cho phép partial.
- AC-3: Tạo `cashier_payments` gắn `shift_id`, `billing_id`, cập nhật `billing.paid_amount` + `billing.status` (PAID nếu outstanding=0, PARTIAL nếu còn).
- AC-4: In phiếu thu A6 (PDF).
- AC-5: Cho phép HUỶ payment trong vòng 5 phút nếu chưa đóng ca; quá thời gian phải tạo refund.

### US-CASH-03 — Đóng ca
- AC-1: KT click "Đóng ca" → modal hiện:
  - `opening_cash` (đầu ca)
  - `total_cash_collected` (tổng CASH thu được)
  - `expected_cash = opening_cash + total_cash_collected`
  - Input `actual_cash` (KT thực đếm)
  - `variance = actual_cash − expected_cash` (auto, có thể âm)
- AC-2: Nếu `|variance| > 0` → bắt nhập `variance_reason` (text).
- AC-3: Confirm → `status=CLOSED`, `closed_at`, `closed_by`, lock không cho thu thêm.
- AC-4: Tự sinh report PDF: chi tiết payments + breakdown theo method + variance.

### US-CASH-04 — Báo cáo công nợ aging
- AC-1: KT chọn "Báo cáo công nợ" → bảng group theo BN, cột: `0-30d`, `31-60d`, `61-90d`, `>90d`, `total`.
- AC-2: Filter: theo BS, theo bill type, theo BHYT/non-BHYT.
- AC-3: Export Excel/CSV.
- AC-4: Click 1 row → drill-down danh sách bill outstanding của BN.

## 5. State machine
```
cashier_shifts:  OPEN ──close──▶ CLOSED (terminal)
billing:         UNPAID ──pay──▶ PARTIAL ──pay──▶ PAID
                                              └──refund──▶ REFUNDED
```

## 6. Data model

### `diab_his_bil_cashier_shifts`
| Cột | Kiểu |
|---|---|
| id INT PK, uuid CHAR(36) | |
| tenant_id INT NOT NULL | |
| clinic_id INT NOT NULL | |
| opened_by INT NOT NULL FK users | |
| opened_at DATETIME, opening_cash DECIMAL(18,2) | |
| closed_by INT NULL, closed_at DATETIME NULL | |
| expected_cash, actual_cash, variance DECIMAL(18,2) | |
| variance_reason TEXT | |
| status ENUM('OPEN','CLOSED') | |
| created_at, updated_at | |

**Index quan trọng (fix BUG-CRUD-01):** `idx_shift_active (tenant_id, opened_by, status, opened_at DESC)`

### `diab_his_bil_cashier_payments`
`id, uuid, tenant_id, shift_id FK, billing_id FK, patient_id FK, amount DECIMAL(18,2), method ENUM('CASH','CARD','QR','BANK_TRANSFER'), reference VARCHAR(100), note TEXT, status ENUM('ACTIVE','CANCELLED','REFUNDED'), cancelled_at, refund_of_payment_id INT NULL, created_at/by`

### `diab_his_bil_billing` (đã có, bổ sung)
Thêm cột: `paid_amount DECIMAL(18,2) DEFAULT 0`, `outstanding DECIMAL(18,2) GENERATED ALWAYS AS (total_amount - paid_amount)`, `status ENUM('UNPAID','PARTIAL','PAID','REFUNDED','VOID')`.

## 7. API contract
| Method | Path |
|---|---|
| GET | /api/v1/cashier/shift (ca OPEN của user hiện tại, hoặc 404) |
| POST | /api/v1/cashier/shift/open `{opening_cash}` |
| POST | /api/v1/cashier/shift/{id}/close `{actual_cash, variance_reason?}` |
| GET | /api/v1/cashier/shift/{id} (detail + payments) |
| GET | /api/v1/cashier/shift/{id}/report (PDF) |
| GET | /api/v1/cashier/payments?shift_id=&from=&to= |
| POST | /api/v1/cashier/payments `{billing_id, amount, method, reference, note}` |
| POST | /api/v1/cashier/payments/{id}/cancel |
| POST | /api/v1/cashier/payments/{id}/refund `{amount, reason}` |
| GET | /api/v1/cashier/reports/aging?as_of=&doctor_id=&bhyt= |

## 8. UX wireframe
```
┌─ Ca thu ngân #SH-2026-0531-A · OPEN ──── [Đóng ca] ─┐
│ Mở: 31/05 07:30 · Tiền đầu: 500.000đ                 │
│ Thu được: CASH 12.5tr · CARD 3.2tr · QR 1.8tr        │
├──────────────────────────────────────────────────────┤
│ Bill chưa thu hôm nay (8):                            │
│ B-2026-0234 │ N.V.A │ 450.000 │ [Thu]                │
│ B-2026-0235 │ T.T.B │ 320.000 │ [Thu]                │
└──────────────────────────────────────────────────────┘

[Đóng ca]
 Đầu ca:    500.000
 + CASH:  12.500.000
 = Kỳ vọng:13.000.000
 Thực đếm: [_________]   Chênh: 0
 Lý do (nếu lệch): [___________________]
 [Huỷ] [Xác nhận đóng ca]
```

## 9. Edge cases
- KT quên đóng ca cuối ngày: Admin có quyền force-close (audit log).
- 2 KT cùng ca chung quầy: KHÔNG hỗ trợ v1 (1 user — 1 ca).
- Thu trước, chỉnh bill sau: payment giữ nguyên amount, chênh lệch tạo bill mới hoặc refund.
- Mất kết nối máy POS: KT ghi `method=CARD`, `reference` text manual.
- Hoá đơn BHYT (chỉ thu cùng chi trả): `amount = bill.copay_amount`.

## 10. Non-functional
- Performance: `/cashier/shift` < 2s p95 (sau fix index), `/payments` POST < 500ms.
- Audit: mọi cancel/refund ghi `sec_audit_logs` + lý do.
- Số dư tiền: tính realtime từ payments (không cache).
- BHYT mapping: payment trên bill BHYT → field `SO_TIEN_CUNG_CT` trong XML1.
- FHIR mapping: payment → `PaymentReconciliation`, billing → `Invoice`.

## 11. Out of scope (v1)
- Tích hợp POS card reader API.
- Đa quầy đồng thời (multi-counter).
- Phân ca theo lịch làm việc.

## 12. Dependencies
- Module Billing (đã có entity `billing`, cần bổ sung paid_amount/status).
- Module Patient.
- Service Catalog (giá dịch vụ snapshot vào bill).
