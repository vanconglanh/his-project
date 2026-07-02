# Sprint 5 — EPIC 4 Lab/Rad Results + External Lab Integration

**Architect:** Lành | **Date:** 2026-05-22 | **DB engine:** MySQL 8

## Scope

Sprint 3-4 đã có `cls-orders.yaml` (chỉ định XN/CĐHA). Sprint này focus:
- Nhập / xác thực kết quả XN (manual + import CSV/HL7)
- Kết quả CĐHA + upload DICOM
- Tích hợp đối tác lab thứ 3 (Medlatec, Diag) — 2 chiều outbound/inbound
- US-SUNS-13/14/15

## Files

| File | Endpoints | Mô tả |
|------|-----------|-------|
| `lab-results.yaml` | 10 | Kết quả XN: CRUD, verify/unverify, import CSV-HL7, abnormal list, history-trend, PDF, batch-verify |
| `rad-results.yaml` | 6  | Kết quả CĐHA: CRUD, verify (ký PDF), DICOM upload, PDF |
| `lab-partners.yaml` | 7 | Đối tác lab: CRUD, test-connection, credentials, rotate |
| `lab-integration.yaml` | 8 | Outbound send/list/retry + Webhook inbound + list/reprocess/raw + stats 7d |
| **Total** | **31** | |

## Schemas chính

- `LabResultResponse` — flag NORMAL/H/L/HH/LL/CRITICAL, status DRAFT/VERIFIED/AMENDED, source MANUAL/IMPORT/PARTNER
- `RadResultResponse` — findings/impression/conclusion/recommendations, dicom_count, signed_pdf_url
- `LabPartner` — auth_type NONE/API_KEY/BEARER, transport REST/HL7_MLLP, api_key mã hóa AES-256-GCM
- `LabOutbound` — PENDING -> SENT -> ACKED|FAILED, retry exponential backoff (max 5)
- `LabInbound` — RECEIVED -> PROCESSED|FAILED, idempotent theo `external_result_id`

## Error codes (message tiếng Việt)

```
LAB_RESULT_NOT_FOUND
LAB_RESULT_ALREADY_VERIFIED
LAB_RESULT_EDIT_TIMEOUT          (sau 15 phút verify không sửa được trừ khi AMEND có reason)
LAB_IMPORT_INVALID_FORMAT
LAB_IMPORT_PARSE_ERROR
LAB_PARTNER_NOT_FOUND
LAB_PARTNER_CONNECTION_FAILED
LAB_PARTNER_AUTH_INVALID
LAB_INTEGRATION_RETRY_EXCEEDED   (>5 retry, cần admin force)
LAB_WEBHOOK_INVALID_SIGNATURE
RAD_RESULT_NOT_FOUND
RAD_DICOM_UPLOAD_FAILED
```

## Permission map (gửi qua Thảo cho seed)

| Permission | KTV | KTV Trưởng | BS CĐHA | Admin |
|---|---|---|---|---|
| `lab_result.read` | x | x | x | x |
| `lab_result.write` | x | x |   | x |
| `lab_result.verify` |   | x |   | x |
| `lab_result.import` | x | x |   | x |
| `rad_result.read` |   | x | x | x |
| `rad_result.write` |   |   | x | x |
| `rad_result.verify` |   |   | x | x |
| `lab_partner.read` |   | x |   | x |
| `lab_partner.write` |   |   |   | x |
| `lab_partner.admin` |   |   |   | x |
| `lab_integration.send` | x | x |   | x |
| `lab_integration.retry` |   | x |   | x |
| `lab_integration.webhook` | — | — | — | — (public, API key) |

Tổng 15 permission mới (đếm cả `lab_integration.send/retry/webhook`).

## Migrations (chuyển Thảo — MySQL 8)

### `0032_lab_rad_results.sql`
- ALTER `cli_lab_results` qua `add_col_if_missing`: `value_numeric DECIMAL(18,4) NULL`, `unit VARCHAR(32) NULL`, `reference_range_low DECIMAL(18,4) NULL`, `reference_range_high DECIMAL(18,4) NULL`, `flag ENUM('NORMAL','H','L','HH','LL','CRITICAL') DEFAULT 'NORMAL'`, `method VARCHAR(64) NULL`, `status ENUM('DRAFT','VERIFIED','AMENDED') DEFAULT 'DRAFT'`, `verified_at DATETIME NULL`, `verified_by CHAR(36) NULL`, `source ENUM('MANUAL','IMPORT','PARTNER') DEFAULT 'MANUAL'`.
- Index: `(tenant_id, status)`, `(tenant_id, flag)`, `(tenant_id, patient_id, test_code, performed_at)` cho trend.
- ALTER `cli_rad_results` add: `findings TEXT`, `impression TEXT NULL`, `conclusion TEXT`, `recommendations TEXT NULL`, `dicom_count INT DEFAULT 0`, `signed_pdf_path VARCHAR(512) NULL`, `status ENUM('DRAFT','VERIFIED','AMENDED') DEFAULT 'DRAFT'`, `verified_at/by`.

### `0033_lab_partners_seed_dict.sql`
- CREATE TABLE `cli_lab_partners` (id CHAR(36) PK, tenant_id, code, name, endpoint_url, auth_type, api_key_encrypted VARBINARY(512), bearer_token_encrypted VARBINARY(1024), api_key_masked VARCHAR(32), transport, supported_tests JSON, status, contact_email/phone, audit cols). UNIQUE `(tenant_id, code)`.
- CREATE TABLE `cli_lab_outbound` (id, tenant_id, lab_order_id FK, lab_partner_id FK, external_order_id, payload_json JSON, status, retry_count, error_message TEXT, sent_at, acked_at, audit). Index `(tenant_id, status, created_at)`.
- CREATE TABLE `cli_lab_inbound` (id, tenant_id, lab_partner_id FK, external_result_id, outbound_id FK NULL, payload_json JSON NULL, raw_hl7_message MEDIUMTEXT NULL, headers JSON NULL, status, processed_at, received_at, processed_result_count, error_message TEXT, audit). UNIQUE `(lab_partner_id, external_result_id)` idempotent.
- ADD cols `diab_his_dict_lab_tests`: `reference_range_low/high DECIMAL`, `unit VARCHAR(32)` nếu thiếu (add_col_if_missing).
- Seed 2 partner: `MEDLATEC` (REST, supported_tests `["GLU","HBA1C","CHOL","LDL","HDL","TG"]`), `DIAG` (REST, `["CBC","HBA1C"]`) — status INACTIVE, để admin bật & nhập key.

### `0034_seed_permissions_sprint5.sql`
- INSERT IGNORE 15 permission codes vào `iam_permissions`.
- Map role:
  - `KTV` -> lab_result.read/write/import, lab_integration.send
  - `KTV_TRUONG` -> + lab_result.verify, lab_partner.read, lab_integration.retry, rad_result.read
  - `BS_CDHA` -> rad_result.read/write/verify, lab_result.read
  - `ADMIN` -> all (bao gồm lab_partner.admin)
- Webhook không cần permission JWT (public + HMAC).

## Notes cho team

- **Hùng (BE)**: dùng MediatR command/handler. Webhook controller ngoài `[Authorize]` — middleware verify HMAC `X-Partner-Signature = HMAC-SHA256(secret, raw_body)`. Background job dùng Hangfire/Quartz cho retry outbound (exponential 1m, 5m, 15m, 1h, 6h).
- **Trang (FE)**: màn `/cls/lab-results` (filter + verify batch), `/cls/rad-results/{id}` (DICOM viewer dùng `cornerstone.js`), `/admin/lab-partners` (CRUD + test connection), `/admin/lab-integration/dashboard` (stats 7d).
- **FHIR mapping**: LabResult -> `Observation` (category=laboratory, code=LOINC), RadResult -> `DiagnosticReport` + `ImagingStudy`.
- **Sensitive cols cần mã hóa AES-256-GCM**: `cli_lab_partners.api_key_encrypted`, `bearer_token_encrypted`.
- **DICOM storage**: MinIO bucket `rad-dicom`, path `{tenant_id}/{rad_result_id}/{filename}.dcm`. Không lưu trong DB.

## Open questions cho Đăng

1. Tốc độ verify SLA — có cần auto-verify với test nằm trong reference range (FE config flag `auto_verify` trong import đã có)?
2. CRITICAL flag — có cần gửi SMS/notification realtime tới BS điều trị không? (Sprint 5 chưa có module notification)
3. AMEND có cần workflow approval 2 cấp (KTV trưởng + Trưởng khoa) hay 1 cấp đủ?
