# PRD — Danh mục Dịch vụ (Service Catalog)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-05-31 · Version: 1.0
> Liên quan: CLAUDE.md §4 (bổ sung module mới), QĐ 5454/QĐ-BYT (giá DVKT), TT 13/2019 (giá BHYT)
> Cross-link: `docs/review/po-review-2026-05-31.md` §3 US-SVC-01

## 1. Mục tiêu
- Cho phép Admin quản lý **danh mục dịch vụ** dùng cho billing + BHYT export: Khám / Xét nghiệm / CĐHA / Thủ thuật.
- Lưu **lịch sử giá** (không destructive) — bill cũ giữ giá tại thời điểm.
- Mapping `bhyt_code` (chuẩn QĐ 5454) + `bhyt_price` + `bhyt_coverage_rate` để export XML BHYT chính xác.
- Fix timeout trang `/services`.

## 2. Personas
| Persona | Quyền |
|---|---|
| Admin | SVC_CREATE/UPDATE/DELETE/ACTIVATE |
| BacSi, LeTan, KeToan | SVC_VIEW (search khi chỉ định) |
| KyThuatVien | SVC_VIEW (CLS) |

## 3. Use cases
- UC-01: Admin CRUD dịch vụ.
- UC-02: Sửa giá → sinh bản ghi `service_price_history` mới, mở rộng `effective_to` bản cũ.
- UC-03: Tạo package (gói combo nhiều dịch vụ, có giá tổng riêng).
- UC-04: Map BHYT code + rate.
- UC-05: Search/filter theo group, active, BHYT-eligible.

## 4. User stories & AC

### US-SVC-01 — Admin CRUD dịch vụ
- AC-1: Form tạo: `code` (unique per tenant), `name`, `group` ENUM(KHAM, XN, CDHA, TT, KHAC), `unit_price`, `unit` (lần/test/phim), `is_bhyt_eligible`, `bhyt_code`, `bhyt_price`, `bhyt_coverage_rate` (0-100%), `is_active`.
- AC-2: Validate: `unit_price ≥ 0`, `code` regex `^[A-Z0-9_]+$`, `bhyt_code` required khi `is_bhyt_eligible=true`.
- AC-3: Delete = soft delete (`deleted_at`). Không cho xoá nếu đã dùng trong bill (return 409 `SVC_IN_USE`).
- AC-4: Filter: `?group=&active=&bhyt=&q=` (search code+name).
- AC-5: Fix timeout `/services`: index `(tenant_id, group, is_active)`, paging mặc định 50, không SELECT toàn bộ price_history.

### US-SVC-02 — Sửa giá có history
- AC-1: PUT `/services/{id}/price` với `{unit_price, bhyt_price, effective_from}` → tạo `service_price_history` mới, cập nhật `effective_to=effective_from` của bản cũ.
- AC-2: Không cho sửa trực tiếp cột `unit_price` qua PUT thường — phải qua endpoint riêng.
- AC-3: Khi service được add vào bill → snapshot `price_at_time` + `bhyt_price_at_time` vào `billing_items`.
- AC-4: GET `/services/{id}/price-history` trả timeline + actor.

### US-SVC-03 — BHYT mapping
- AC-1: Field `bhyt_code` ràng buộc theo danh mục QĐ 5454 (validation soft — cảnh báo nếu không match).
- AC-2: `bhyt_coverage_rate`: % BHYT chi trả (vd 80%, 95%, 100%).
- AC-3: Khi bill cho BN có BHYT: tự tính `bhyt_amount = unit_price × bhyt_coverage_rate × quantity`, `copay_amount = total − bhyt_amount`.
- AC-4: Service `is_bhyt_eligible=false` → toàn bộ vào copay.

## 5. Data model

### `diab_his_bil_services`
| Cột | Kiểu |
|---|---|
| id INT PK, uuid | |
| tenant_id INT NOT NULL | unique `(tenant_id, code)` |
| code VARCHAR(50), name VARCHAR(255) | |
| group ENUM('KHAM','XN','CDHA','TT','KHAC') | index `(tenant_id, group, is_active)` |
| unit VARCHAR(20), unit_price DECIMAL(18,2) | giá hiện hành (cache từ price_history mới nhất) |
| is_bhyt_eligible TINYINT(1) | |
| bhyt_code VARCHAR(50) NULL, bhyt_price DECIMAL(18,2) NULL, bhyt_coverage_rate DECIMAL(5,2) NULL | |
| is_active TINYINT(1) DEFAULT 1 | |
| description TEXT | |
| created_at/by, updated_at/by, deleted_at | |

### `diab_his_bil_service_price_history`
`id, service_id FK, unit_price, bhyt_price, bhyt_coverage_rate, effective_from DATE, effective_to DATE NULL, changed_by, changed_at, change_reason TEXT`

Constraint: tại 1 thời điểm chỉ 1 row có `effective_to IS NULL`.

### `diab_his_bil_service_packages`
`id, tenant_id, code, name, package_price DECIMAL(18,2), is_active`

### `diab_his_bil_service_package_items`
`id, package_id FK, service_id FK, quantity INT DEFAULT 1`

## 6. API contract
| Method | Path |
|---|---|
| GET | /api/v1/services?group=&active=&bhyt=&q=&page=&size= |
| POST | /api/v1/services |
| GET | /api/v1/services/{id} |
| PUT | /api/v1/services/{id} (sửa metadata, KHÔNG sửa giá) |
| PUT | /api/v1/services/{id}/price `{unit_price, bhyt_price, bhyt_coverage_rate, effective_from, change_reason}` |
| DELETE | /api/v1/services/{id} (soft) |
| POST | /api/v1/services/{id}/activate, /deactivate |
| GET | /api/v1/services/{id}/price-history |
| GET/POST/PUT/DELETE | /api/v1/service-packages |

Error code: `SVC_CODE_DUPLICATE`, `SVC_IN_USE`, `SVC_INVALID_BHYT_CODE`, `SVC_PRICE_OVERLAP`.

## 7. UX wireframe
```
┌─ Danh mục Dịch vụ ─────────[+ Thêm]─[Import Excel]─┐
│ [KHAM ▼] [Active ✓] [BHYT ✓] [Search: ____] [Lọc]  │
├────────────────────────────────────────────────────┤
│ Mã    │ Tên          │ Nhóm│ Giá    │ BHYT │ Action│
│ KB001 │ Khám TQ      │ KHAM│ 80.000 │ ✓    │ [✎][↻]│
│ XN012 │ Glucose máu  │ XN  │ 25.000 │ ✓    │ [✎][↻]│
├────────────────────────────────────────────────────┤
│ [✎ KB001]  Giá hiện tại: 80.000 (từ 01/05/2026)    │
│ Lịch sử:                                            │
│  · 80.000 — 01/05/2026 → nay (Admin A)             │
│  · 70.000 — 01/01/2026 → 30/04/2026 (Admin A)      │
└────────────────────────────────────────────────────┘
```

## 8. Edge cases
- Trùng `bhyt_code` giữa 2 dịch vụ: cho phép (1 mã BHYT có thể map nhiều dịch vụ nội bộ — vd khám TQ và khám chuyên khoa cùng code).
- Sửa giá `effective_from` trong quá khứ: chặn (block, return 400 `SVC_PRICE_BACKDATE_FORBIDDEN`).
- Service đang trong package: deactivate phải cảnh báo package bị ảnh hưởng.
- Import Excel hàng loạt: validate từng dòng, return summary thành công/lỗi.
- Thay đổi `bhyt_coverage_rate` giữa kỳ: bill cũ giữ rate cũ (snapshot).

## 9. Non-functional
- Performance: list `/services` < 500ms p95 (sau fix index + paging).
- Cache: list active services per tenant cache Redis TTL 5 phút, invalidate khi CRUD.
- Audit: mọi thay đổi giá ghi `sec_audit_logs` + `service_price_history.change_reason`.
- BHYT mapping: `bhyt_code` + `bhyt_price` ghi vào XML3 (DVKT) khi export.
- FHIR mapping: service → `ChargeItemDefinition`.

## 10. Out of scope (v1)
- Giá theo đối tượng BN (VIP, nội bộ, hợp đồng).
- Giá theo thời điểm trong ngày (ngày/đêm).
- Service dependency tree (vd XN A bắt buộc kèm XN B).

## 11. Dependencies
- Module Billing (snapshot price).
- Module BHYT export (xem `bhyt-prd.md` — cần `bhyt_code`, `bhyt_price`).
- Danh mục QĐ 5454 (seed initial vào `db/seeds/`).
