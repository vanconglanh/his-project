# PRD — Nhà cung cấp & Purchase Order & GRN

> Tác giả: Đăng (PO/BA) · Ngày: 2026-05-31 · Version: 1.0
> Liên quan: CLAUDE.md §4 Pharmacy, TT 11/2018/TT-BYT (truy xuất nguồn gốc thuốc), TT 02/2018 (thực hành tốt phân phối)
> Cross-link: `docs/review/po-review-2026-05-31.md` §3 US-SUP-01

## 1. Mục tiêu
- Quản lý **danh mục nhà cung cấp** thuốc/vật tư.
- Tạo **Purchase Order (PO)** đặt hàng NCC.
- Nhận hàng qua **GRN (Goods Receipt Note)** → cập nhật tồn kho theo lô + HSD → đảm bảo truy xuất nguồn gốc.

## 2. Personas
| Persona | Quyền |
|---|---|
| Admin | SUP_*, PO_*, GRN_* |
| DuocSi | SUP_VIEW, PO_CREATE/EDIT, GRN_RECEIVE |
| KeToan | SUP_VIEW, PO_VIEW (đối soát công nợ NCC) |

## 3. Use cases
- UC-01: CRUD NCC.
- UC-02: Tạo PO (chọn NCC + items + giá + SL dự kiến).
- UC-03: Confirm PO → gửi NCC.
- UC-04: Tạo GRN từ PO (nhận 1 phần hoặc toàn bộ) → ghi lô + HSD → cập nhật `pha_stock`.
- UC-05: Đóng PO khi nhận đủ.
- UC-06: Truy vết: từ 1 lô → tìm GRN → PO → NCC.

## 4. User stories & AC

### US-SUP-01 — Admin CRUD NCC
- AC-1: Form: `code` (unique/tenant), `name`, `tax_code`, `phone`, `email`, `address`, `contact_person`, `bank_account`, `bank_name`, `payment_term_days`, `is_active`.
- AC-2: Validate `tax_code` Việt Nam (10 hoặc 13 ký tự số).
- AC-3: Soft delete; chặn xoá nếu có PO hoặc lô tồn > 0 (return 409 `SUP_IN_USE`).
- AC-4: Search/filter: active, tên, mã số thuế.

### US-SUP-02 — Tạo Purchase Order
- AC-1: Chọn NCC → thêm items: `drug_id`, `quantity_ordered`, `unit_price`, `expected_delivery_date`.
- AC-2: Tự tính `subtotal`, `vat_amount` (cấu hình theo NCC, mặc định 5% hoặc 10%), `total_amount`.
- AC-3: Status `DRAFT` cho phép sửa. Click "Xác nhận" → `CONFIRMED` (lock, audit).
- AC-4: In PO PDF gửi NCC.
- AC-5: Huỷ PO chỉ khi DRAFT hoặc CONFIRMED chưa có GRN.

### US-SUP-03 — GRN nhập kho
- AC-1: Từ PO `CONFIRMED`, click "Tạo phiếu nhập" → form GRN: với mỗi PO item, nhập `quantity_received`, `batch_no`, `manufacture_date`, `expiry_date`, `unit_cost` (có thể khác PO).
- AC-2: Validate: `expiry_date > today + 6 tháng` (cảnh báo nếu < 6 tháng, block nếu < 3 tháng), `quantity_received ≤ quantity_ordered − đã_nhận`.
- AC-3: Confirm GRN → insert/update `pha_stock` (cộng dồn theo `(drug_id, batch_no)`), insert `pha_stock_movement` type=GRN_IN.
- AC-4: Mỗi `pha_stock` row giữ `supplier_id`, `po_id`, `grn_id` để truy vết (TT 11/2018).
- AC-5: Khi tổng `quantity_received` của PO = `quantity_ordered` → PO chuyển `RECEIVED`. Nếu < → `PARTIAL_RECEIVED`.
- AC-6: Tự sinh công nợ phải trả NCC (`supplier_payable`) = sum(GRN.total).

## 5. State machine
```
PO:  DRAFT ──confirm──▶ CONFIRMED ──grn──▶ PARTIAL_RECEIVED ──grn──▶ RECEIVED ──close──▶ CLOSED
       │                  │
       └──cancel──────────┘
                          ▼
                       CANCELLED
GRN: DRAFT ──confirm──▶ POSTED (terminal, không sửa được)
```

## 6. Data model

### `diab_his_pha_suppliers`
| Cột | Kiểu |
|---|---|
| id, uuid, tenant_id | unique `(tenant_id, code)` |
| code, name, tax_code, phone, email | |
| address, contact_person, bank_account, bank_name | |
| payment_term_days INT DEFAULT 30 | |
| is_active, created_at/by, updated_at/by, deleted_at | |

### `diab_his_pha_purchase_orders`
`id, uuid, tenant_id, code (PO-YYYY-NNNN), supplier_id FK, order_date DATE, expected_delivery_date DATE, status ENUM('DRAFT','CONFIRMED','PARTIAL_RECEIVED','RECEIVED','CLOSED','CANCELLED'), subtotal, vat_rate DECIMAL(5,2), vat_amount, total_amount DECIMAL(18,2), note TEXT, confirmed_at/by, cancelled_at/by, created_at/by`

### `diab_his_pha_purchase_order_items`
`id, po_id FK, drug_id FK, quantity_ordered INT, quantity_received INT DEFAULT 0, unit_price DECIMAL(18,2), line_total, expected_delivery_date`

### `diab_his_pha_grn` (Goods Receipt Note)
`id, uuid, tenant_id, code (GRN-YYYY-NNNN), po_id FK, supplier_id FK, received_date DATE, received_by FK users, status ENUM('DRAFT','POSTED'), total_amount, note, posted_at/by`

### `diab_his_pha_grn_items`
`id, grn_id FK, po_item_id FK, drug_id FK, batch_no VARCHAR(50), manufacture_date, expiry_date, quantity_received INT, unit_cost DECIMAL(18,2), line_total`

### `diab_his_pha_stock` (đã có, bổ sung)
Bổ sung: `supplier_id`, `po_id`, `grn_id` (last receipt), `expiry_date`, `batch_no` — đảm bảo truy vết.

## 7. API contract
| Method | Path |
|---|---|
| GET/POST | /api/v1/suppliers |
| GET/PUT/DELETE | /api/v1/suppliers/{id} |
| POST | /api/v1/suppliers/{id}/activate \| /deactivate |
| GET/POST | /api/v1/purchase-orders |
| GET/PUT/DELETE | /api/v1/purchase-orders/{id} (DRAFT only) |
| POST | /api/v1/purchase-orders/{id}/confirm |
| POST | /api/v1/purchase-orders/{id}/cancel |
| POST | /api/v1/purchase-orders/{id}/close |
| GET | /api/v1/purchase-orders/{id}/print |
| GET/POST | /api/v1/grn (POST body: `{po_id, items: [...]}`) |
| GET | /api/v1/grn/{id} |
| POST | /api/v1/grn/{id}/post (DRAFT → POSTED) |
| GET | /api/v1/stock/{stockId}/traceability (lô → GRN → PO → NCC) |

Error: `SUP_TAX_INVALID`, `SUP_IN_USE`, `PO_NOT_EDITABLE`, `GRN_EXPIRY_TOO_SOON`, `GRN_OVER_RECEIVE`.

## 8. UX wireframe
```
┌─ Purchase Order #PO-2026-0042 · CONFIRMED ────────┐
│ NCC: Cty Dược ABC · TT: 30 ngày                    │
│ Đặt: 28/05/2026 · Dự kiến: 05/06/2026              │
├────────────────────────────────────────────────────┤
│ Thuốc        │ SL đặt │ Đã nhận │ Đơn giá │ Total  │
│ Metformin    │  1000  │   600   │  1.200  │1.2tr   │
│ Glimepiride  │   500  │     0   │  3.500  │1.75tr  │
├────────────────────────────────────────────────────┤
│ [Tạo phiếu nhập GRN]  [In PO]  [Đóng PO]           │
└────────────────────────────────────────────────────┘

[GRN mới]
 Metformin: nhận [400] lô [LOT-A23] HSD [12/2027] giá [1.200]
 [+ Thêm dòng]
 [Lưu DRAFT]  [Ghi sổ POSTED]
```

## 9. Edge cases
- NCC đổi MST: tạo NCC mới + deactivate cũ (giữ lịch sử PO/GRN).
- Nhận hàng nhiều lô khác nhau cho cùng 1 PO item: nhiều GRN row cho cùng `po_item_id`.
- Trả hàng (Goods Return): sprint sau (RGN — Return Good Note), out of scope v1.
- Lô đã nhập sai HSD: cho sửa qua "Stock Adjustment" (module Pharmacy), audit log bắt buộc.
- PO + GRN cross-tenant: chặn (validate tenant_id trùng giữa supplier, PO, GRN).
- Đơn giá GRN khác PO: cho phép (giá thực nhận), cảnh báo vàng, ghi `unit_cost_variance`.

## 10. Non-functional
- Performance: list suppliers < 300ms, list PO < 500ms p95.
- Truy vết (TT 11/2018): từ lô bất kỳ truy ngược về NCC trong ≤ 3 click.
- Audit: confirm PO + post GRN ghi `sec_audit_logs` (immutable transition).
- Inventory consistency: GRN post + stock update trong cùng DB transaction.
- FHIR mapping: supplier → `Organization` (role=supplier), PO → `SupplyRequest`, GRN → `SupplyDelivery`.

## 11. Out of scope (v1)
- Trả hàng NCC (RGN).
- Đấu thầu / so sánh báo giá nhiều NCC.
- Tích hợp API NCC (EDI).
- Quản lý công nợ NCC chi tiết (chỉ có payable balance đơn giản).

## 12. Dependencies
- Module Drug (drug_id).
- Module Pharmacy Stock (`pha_stock`, `pha_stock_movement`).
- Module Cashier/Accounting (công nợ NCC — payable).
