# PRD — Module Kê đơn thuốc (Prescription)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-05-31 · Version: 1.0
> Liên quan: CLAUDE.md §4 Prescription, TT 27/2021/TT-BYT, ADR-pending (DTQG integration)
> Cross-link: `docs/review/po-review-2026-05-31.md` §2, FHIR R4 `MedicationRequest`

## 1. Mục tiêu
- Cho phép bác sĩ kê đơn điện tử trong lượt khám (Encounter), ký số và đẩy lên cổng **Đơn thuốc Quốc gia** (donthuocquocgia.vn) theo TT 27/2021/TT-BYT.
- Dược sĩ phát thuốc dựa trên đơn đã ký, trừ tồn kho theo lô.
- In đơn A5 có mã QR DTQG hợp lệ.
- Đáp ứng truy vết: mỗi đơn gắn `encounter_id`, `patient_id`, `doctor_id`, `tenant_id`.

## 2. Personas
| Persona | Quyền | Hành động chính |
|---|---|---|
| BacSi | RX_CREATE/EDIT/SUBMIT | Tạo DRAFT, sửa, ký gửi ĐTQG, huỷ |
| DuocSi | RX_DISPENSE | Phát thuốc, ghi nhận đã nhận |
| LeTan | RX_VIEW | Xem trạng thái, in lại |
| Admin | RX_* | Toàn quyền + audit |

## 3. Use cases
- UC-01: Tạo đơn từ Encounter (entry chính)
- UC-02: Tạo đơn tái khám nhanh từ đơn cũ (clone)
- UC-03: Ký số & đẩy ĐTQG → nhận `dtqg_code` + QR
- UC-04: In đơn A5 với QR
- UC-05: Phát thuốc (Dược sĩ) — trừ tồn kho theo FEFO
- UC-06: Huỷ đơn (chỉ DRAFT/SUBMITTED, lý do bắt buộc)

## 4. User stories & Acceptance Criteria

### US-RX-01 — BS tạo đơn DRAFT
- AC-1: Given BS mở Encounter `E` của BN `P`, When click "Tạo đơn thuốc", Then tạo prescription `status=DRAFT`, `encounter_id=E`, `patient_id=P`, `doctor_id=current_user`, `tenant_id` từ JWT.
- AC-2: Header đơn hiển thị: họ tên BN, năm sinh, giới tính, cân nặng (bắt buộc khi tuổi ≤ 72 tháng), địa chỉ, chẩn đoán ICD-10 từ Encounter (snapshot), BS kê đơn, ngày kê.
- AC-3: Mỗi item bắt buộc: `drug_id`, `dose_per_time`, `times_per_day`, `route` (PO/IV/IM/SC/TOP), `duration_days`, `total_quantity` (tự tính), `instruction` (free text).
- AC-4: Validation: `total_quantity > 0`, `duration_days ≥ 1`, `route ∈ enum`. Lỗi hiển thị tiếng Việt có dấu.

### US-RX-02 — Submit đơn (DRAFT → SUBMITTED)
- AC-1: Given DRAFT có ≥1 item hợp lệ, When click "Gửi", Then chuyển `SUBMITTED`, lock chỉnh sửa.
- AC-2: Check DDI (drug-drug interaction): nếu có cặp CHỐNG CHỈ ĐỊNH → block, hiển thị danh sách cảnh báo.
- AC-3: Ghi `submitted_at`, `submitted_by`.

### US-RX-03 — Đẩy ĐTQG
- AC-1: Given SUBMITTED, When click "Ký số & đẩy ĐTQG", Then BE gọi API donthuocquocgia.vn với token tenant từ `his_tenant_integration`.
- AC-2: Thành công → lưu `dtqg_code`, `dtqg_qr_url`, `dtqg_sent_at`, status `DTQG_SENT`.
- AC-3: Thất bại (4xx/5xx/timeout) → giữ SUBMITTED, log `dtqg_last_error`, hiển thị toast tiếng Việt. Cho retry tối đa 3 lần/đơn.
- AC-4: Idempotency: re-call với cùng `prescription_uuid` → BE trả `dtqg_code` cũ, không tạo trùng.

### US-RX-04 — In đơn A5 với QR
- AC-1: Given `DTQG_SENT` hoặc `DISPENSED`, When click "In", Then render PDF A5 có: header BN, BS, chẩn đoán, bảng thuốc, mã QR từ `dtqg_qr_url`, chữ ký số.
- AC-2: Ghi `prescription_print_history` (user, timestamp, copy_no).
- AC-3: Reprint cho phép không giới hạn, mỗi lần tăng `copy_no`.

### US-RX-05 — Dược sĩ phát thuốc
- AC-1: Given `DTQG_SENT`, When DuocSi click "Phát thuốc", Then UI hiện danh sách item kèm gợi ý lô FEFO (Hết hạn gần nhất trước) từ `pha_stock`.
- AC-2: Cho phép DS chọn lô khác, ghi `batch_no`, `expiry_date`, `quantity_dispensed`.
- AC-3: Confirm → trừ tồn `pha_stock`, ghi `pha_stock_movement` (type=DISPENSE), chuyển status `DISPENSED`, ghi `dispensed_at`, `dispensed_by`.
- AC-4: Nếu SL phát < SL kê → ghi `partial_reason`, status vẫn `DISPENSED` (1 lần phát chốt).

## 5. State machine
```
DRAFT ──submit──▶ SUBMITTED ──dtqg-submit──▶ DTQG_SENT ──dispense──▶ DISPENSED
  │                  │                          │
  └─cancel──┐        └─cancel──┐                └─(không cancel sau DISPENSED)
            ▼                   ▼
        CANCELLED          CANCELLED
```
Huỷ sau DTQG_SENT: phải gọi API DTQG huỷ đơn → nhận confirm → chuyển CANCELLED.

## 6. Data model

### `diab_his_pha_prescriptions`
| Cột | Kiểu | Ghi chú |
|---|---|---|
| id | INT PK | |
| uuid | CHAR(36) UNIQUE | idempotency key DTQG |
| tenant_id | INT NOT NULL | index `(tenant_id, status, created_at)` |
| encounter_id | INT NOT NULL FK | |
| patient_id | INT NOT NULL FK | |
| doctor_id | INT NOT NULL FK | |
| diagnosis_icd10 | VARCHAR(20) | snapshot từ encounter |
| diagnosis_text | TEXT | |
| patient_weight_kg | DECIMAL(5,2) NULL | bắt buộc khi tuổi ≤ 72 tháng |
| status | ENUM('DRAFT','SUBMITTED','DTQG_SENT','DISPENSED','CANCELLED') | |
| dtqg_code | VARCHAR(64) NULL | |
| dtqg_qr_url | VARCHAR(512) NULL | |
| dtqg_sent_at | DATETIME NULL | |
| dtqg_last_error | TEXT NULL | |
| dispensed_at, dispensed_by | | |
| cancelled_at, cancelled_by, cancel_reason | | |
| note | TEXT | onChange + PATCH (fix gap §2) |
| created_at/by, updated_at/by, deleted_at | audit | |

### `diab_his_pha_prescription_items`
`id, prescription_id FK, drug_id FK, dose_per_time, times_per_day, route ENUM, duration_days, total_quantity, instruction TEXT, batch_no, expiry_date, quantity_dispensed, line_no`

### `diab_his_pha_prescription_print_history`
`id, prescription_id FK, printed_by, printed_at, copy_no, file_url`

## 7. API contract
| Method | Path | Mô tả |
|---|---|---|
| GET | /api/v1/prescriptions?status=&patient_id=&from=&to= | List, paged |
| POST | /api/v1/prescriptions | Tạo DRAFT (body: encounter_id) |
| GET | /api/v1/prescriptions/{id} | Detail + items |
| PUT | /api/v1/prescriptions/{id} | Update DRAFT only |
| POST | /api/v1/prescriptions/{id}/items | Add item |
| DELETE | /api/v1/prescriptions/{id}/items/{itemId} | Remove (DRAFT) |
| POST | /api/v1/prescriptions/{id}/submit | DRAFT → SUBMITTED |
| POST | /api/v1/prescriptions/{id}/dtqg-submit | SUBMITTED → DTQG_SENT |
| POST | /api/v1/prescriptions/{id}/dispense | DTQG_SENT → DISPENSED |
| POST | /api/v1/prescriptions/{id}/cancel | + reason |
| GET | /api/v1/prescriptions/{id}/print | Trả PDF A5 |

Error envelope chuẩn CLAUDE §6: `{ "error": { "code": "RX_DTQG_FAILED", "message": "Không thể đẩy đơn lên ĐTQG: ..." } }`

## 8. UX wireframe (ASCII)
```
┌─ Đơn thuốc #PRX-2026-00123 [DTQG_SENT]  [In] [Huỷ] ─┐
│ BN: Nguyễn Văn A · 1985 · Nam · 65kg                 │
│ Chẩn đoán: E11.9 — Đái tháo đường typ 2              │
│ BS: BS. Trần B · Ngày: 31/05/2026                    │
├──────────────────────────────────────────────────────┤
│ # │ Thuốc        │ Liều │ x/ngày │ Đường │ Ngày │ SL │
│ 1 │ Metformin    │ 500mg│   2    │  PO   │  30  │ 60 │
│ 2 │ Glimepiride  │ 2mg  │   1    │  PO   │  30  │ 30 │
├──────────────────────────────────────────────────────┤
│ Ghi chú: Uống sau ăn ...                             │
│ [QR ĐTQG: DTQG-2026-...] [Ký số đã hoàn tất]          │
└──────────────────────────────────────────────────────┘
```

## 9. Edge cases
- BN nhi ≤ 72 tháng: bắt buộc `patient_weight_kg`.
- BN có dị ứng thuốc (`pat_patient_allergies`): cảnh báo vàng khi thêm item, không block.
- DTQG token hết hạn: trả `RX_DTQG_TOKEN_EXPIRED` → admin renew ở Tenant Settings.
- Mất mạng giữa submit DTQG: dùng `uuid` idempotency, retry an toàn.
- Phát thuốc khi tồn không đủ: block, gợi ý lô thay thế.
- Đơn ngoại trú không gắn Encounter (tái khám đơn lặp): cho phép `encounter_id NULL` nhưng phải có `reissue_of_prescription_id` (US-RX-02 mở rộng).

## 10. Non-functional
- Performance: list `/prescriptions` < 500ms p95 (100 records/page).
- DTQG call timeout 10s, retry 3x exponential backoff.
- Audit: mọi transition status ghi `diab_his_sec_audit_logs` (actor, before/after).
- BHYT mapping: items có `bhyt_eligible=true` đi vào XML 4750 (XML2 — Thuốc).
- FHIR R4: 1 prescription = 1 `MedicationRequest` resource, items = `MedicationRequest.dosageInstruction[]`.
- I18n: tiếng Việt có dấu, mã lỗi `RX_*` SCREAMING_SNAKE.

## 11. Out of scope (v1)
- Kê thuốc hướng tâm thần / gây nghiện (TT 20/2017) — sprint sau.
- E-signature USB token cho BS — sprint sau (v1 dùng JWT + audit).
- Đơn nhập viện nội trú.

## 12. Dependencies
- Module Encounter (chẩn đoán ICD-10).
- Module Drug + Pharmacy Stock (FEFO).
- Tenant Integration table (DTQG token).
- PDF service (QuestPDF/Puppeteer).
