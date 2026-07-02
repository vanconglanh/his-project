# PRD — Module Giám định BHYT (Export & Reconcile)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-05-31 · Version: 1.0
> Liên quan: QĐ 4750/QĐ-BYT, QĐ 130/QĐ-BYT, CLAUDE.md §4 BHYT, §5
> Cross-link: `docs/review/po-review-2026-05-31.md` §3 US-BHYT-01

## 1. Mục tiêu
- Cho phép kế toán **chốt kỳ giám định** (tháng/quý) và sinh đầy đủ **XML theo QĐ 4750/QĐ-BYT**: XML1 (Tổng hợp hồ sơ), XML2 (Chi tiết thuốc), XML3 (Chi tiết DVKT), XML4 (CLS).
- Tải file ZIP để upload lên Cổng giám định BHYT.
- Đối soát kết quả giám định (upload XML phản hồi) → cập nhật trạng thái từng hồ sơ.
- Resubmit hồ sơ sau khi sửa lỗi.

## 2. Personas
| Persona | Quyền |
|---|---|
| KeToan | BHYT_EXPORT, BHYT_RECONCILE |
| Admin | BHYT_* + xoá kỳ lỗi |
| BacSi | BHYT_VIEW (xem lỗi để sửa hồ sơ) |

## 3. Use cases
- UC-01: Tạo kỳ export (chọn from-to date).
- UC-02: Validate trước export (cảnh báo hồ sơ thiếu).
- UC-03: Sinh 4 XML + ZIP.
- UC-04: Đánh dấu SUBMITTED (đã upload cổng).
- UC-05: Upload XML phản hồi → parse + match từng hồ sơ.
- UC-06: Resubmit hồ sơ sau sửa.

## 4. User stories & AC

### US-BHYT-01 — KT chốt kỳ + sinh 4 XML
- AC-1: KT chọn `from_date`, `to_date`, `clinic_id` → POST `/api/v1/bhyt/exports`.
- AC-2: BE gom các Encounter có `is_bhyt=true` đã đóng (status=CLOSED) + prescription `DISPENSED` + dịch vụ + CLS trong khoảng kỳ.
- AC-3: Pre-validate; cảnh báo (không block) nếu encounter thiếu ICD-10, đơn chưa ký, dịch vụ thiếu `bhyt_code/bhyt_price`, BN thiếu số thẻ.
- AC-4: Sinh 4 file XML đúng schema QĐ 4750. Lưu vào MinIO; ghi `diab_his_int_bhyt_exports` với `status=DRAFT`.
- AC-5: KT review counts (số hồ sơ, tổng tiền BHYT, tổng tiền cùng chi trả) → click "Chốt" → status `SUBMITTED`, `submitted_at`, `submitted_by`.
- AC-6: Tải ZIP qua `GET /exports/{id}/zip`.

### US-BHYT-02 — Đối soát kết quả
- AC-1: KT upload file XML phản hồi (cổng giám định) qua `POST /reconcile/upload`.
- AC-2: BE parse, match từng `ma_lk` về `bhyt_export_item` → ghi `status_reconcile = APPROVED | REJECTED | PARTIAL`, `reject_reason`.
- AC-3: Tổng hợp: số hồ sơ accept/reject + tổng tiền được duyệt → cập nhật export `status=APPROVED|REJECTED|PARTIAL`.
- AC-4: Danh sách hồ sơ lỗi hiển thị với link sang Encounter để BS sửa.

### US-BHYT-03 — Resubmit
- AC-1: Sau khi sửa Encounter/đơn lỗi, KT click "Resubmit lô lỗi" → tạo export mới `parent_export_id=cũ`, chỉ chứa hồ sơ REJECTED.
- AC-2: Workflow lặp như US-BHYT-01.
- AC-3: Khi tất cả hồ sơ APPROVED + nhận thông báo thanh toán → KT click "Đánh dấu đã thanh toán" → status `PAID`, `paid_at`, `paid_amount`.

## 5. State machine
```
DRAFT ──submit──▶ SUBMITTED ──upload-reconcile──▶ APPROVED ──mark-paid──▶ PAID
                                  │                  │
                                  ├─▶ PARTIAL ──resubmit-rejected──▶ (new DRAFT)
                                  └─▶ REJECTED ─────┘
```

## 6. Data model

### `diab_his_int_bhyt_exports`
| Cột | Kiểu |
|---|---|
| id INT PK, uuid CHAR(36) UNIQUE | |
| tenant_id INT NOT NULL | index `(tenant_id, status, from_date)` |
| clinic_id INT NOT NULL | |
| from_date, to_date DATE | |
| parent_export_id INT NULL FK | resubmit chain |
| status ENUM('DRAFT','SUBMITTED','APPROVED','PARTIAL','REJECTED','PAID') | |
| total_records INT, total_amount DECIMAL(18,2), approved_amount DECIMAL(18,2), paid_amount DECIMAL(18,2) | |
| xml1_url, xml2_url, xml3_url, xml4_url, zip_url VARCHAR(512) | MinIO key |
| submitted_at/by, paid_at/by | audit |
| created_at/by, updated_at/by, deleted_at | |

### `diab_his_int_bhyt_export_items`
`id, export_id FK, encounter_id FK, ma_lk VARCHAR(50) UNIQUE per export, patient_bhyt_no, total_amount, bhyt_amount, copay_amount, status_reconcile ENUM('PENDING','APPROVED','REJECTED','PARTIAL'), reject_reason TEXT, reject_code VARCHAR(20)`

### `diab_his_int_bhyt_reconcile_uploads`
`id, export_id FK, file_url, uploaded_by, uploaded_at, parsed_records INT, parse_errors TEXT`

### `diab_his_int_bhyt_reconcile_items`
`id, upload_id FK, ma_lk, status, amount_approved, reason`

## 7. API contract
| Method | Path |
|---|---|
| GET | /api/v1/bhyt/exports?status=&from=&to= |
| POST | /api/v1/bhyt/exports `{from_date,to_date,clinic_id}` |
| GET | /api/v1/bhyt/exports/{id} |
| GET | /api/v1/bhyt/exports/{id}/items?status= |
| GET | /api/v1/bhyt/exports/{id}/xml/{type} (type=1\|2\|3\|4) |
| GET | /api/v1/bhyt/exports/{id}/zip |
| POST | /api/v1/bhyt/exports/{id}/submit |
| POST | /api/v1/bhyt/exports/{id}/reconcile/upload (multipart XML) |
| POST | /api/v1/bhyt/exports/{id}/resubmit-rejected |
| POST | /api/v1/bhyt/exports/{id}/mark-paid `{paid_amount}` |

## 8. UX wireframe
```
┌─ Kỳ giám định BHYT ────────────────[+ Tạo kỳ mới]─┐
│ Mã    │ Kỳ        │ HS │ T.tiền   │ TT       │     │
│ EX-05 │ 05/2026   │ 234│ 45.2tr   │ APPROVED │ [▶] │
│ EX-04 │ 04/2026   │ 198│ 38.1tr   │ PAID     │ [▶] │
├────────────────────────────────────────────────────┤
│ Detail EX-05:                                       │
│  ├ XML1 [↓] XML2 [↓] XML3 [↓] XML4 [↓] ZIP [↓]    │
│  ├ 234 hồ sơ: 220 APPROVED · 14 REJECTED            │
│  └ [Đối soát] [Resubmit 14 lỗi] [Đánh dấu đã chi]  │
└────────────────────────────────────────────────────┘
```

## 9. Edge cases
- BN có 2 thẻ BHYT trong kỳ (đổi thẻ): theo thẻ tại ngày khám.
- Encounter cross-month: tính theo `discharge_date`.
- Hồ sơ đa tuyến (chuyển viện): cờ `multi_tier=true` mapping field `MA_LYDO_VVIEN`.
- Re-export cùng kỳ: phải huỷ export cũ trước (chỉ 1 export ACTIVE per kỳ).
- Schema XML đổi version: lưu `xml_schema_version` trong export.

## 10. Non-functional
- Performance: sinh XML cho 1000 hồ sơ < 30s (async job, progress bar).
- Storage: XML/ZIP lưu MinIO bucket `bhyt-exports/{tenant}/{year}/{month}/`.
- Audit: mọi action ghi `sec_audit_logs`.
- FHIR mapping: items map sang `Claim` resource (FHIR R4).
- Bảo mật: file XML chứa thông tin BN → ACL theo tenant, signed URL TTL 1h.

## 11. Out of scope (v1)
- Tự động upload sang cổng giám định (API GDB chưa public ổn định).
- Giám định realtime per-encounter.

## 12. Dependencies
- Encounter (status CLOSED + ICD-10).
- Prescription (status DISPENSED).
- Service Catalog (bhyt_code, bhyt_price) — xem `service-catalog-prd.md`.
- Patient (số thẻ BHYT, nơi đăng ký KCB ban đầu).
