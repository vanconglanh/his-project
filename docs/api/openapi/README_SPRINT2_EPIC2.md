# Pro-Diab HIS — OpenAPI Contract (Sprint 2, EPIC 2)

> **Scope:** Patient Master + Reception + CLS External Uploads + Generic Files.
> **Source of truth** cho backend (Thảo) và frontend (Nam) triển khai song song.
> **Phiên bản:** 1.0.0 — 2026-05-23.

## Cấu trúc file

| File | Mô tả | #Endpoints | #Schemas |
|---|---|---|---|
| `patients.yaml` | CRUD bệnh nhân, search, lịch sử khám, avatar, allergies, BHYT, emergency contacts, consents, reception_note | 18 | 17 |
| `reception.yaml` | Check-in, queue dashboard, gọi/skip/cancel, in phiếu PDF, rooms, stats | 8 | 8 |
| `cls-uploads.yaml` | Upload kết quả CLS bên ngoài (SUNS-I.3 / SUNS-VI.3) | 5 | 1 |
| `files.yaml` | Generic file upload qua MinIO + signed URL pattern | 3 | 1 |

**Tổng: 34 endpoint, 27 schema.**

## Mapping story -> endpoint

### Patient Master (US-P01..P12)

| Story | Mô tả | Endpoint chính | Ghi chú BE | Ghi chú FE |
|---|---|---|---|---|
| US-P01 | Tạo BN mới | `POST /patients` | Auto sinh code `BN{tenantcode}{6digit}`. Validate id_number unique scope tenant. | Form modal, validate client trước. |
| US-P02 | Xem chi tiết BN | `GET /patients/{id}` | Join pat_pii_data + pat_phi_data; masked theo permission. | Trang detail tab Tổng quan. |
| US-P03 | Tìm kiếm BN | `GET /patients/search?q=` | Dùng pg_trgm + unaccent VN; index GIN trên (full_name, phone, code). | Autocomplete debounce 300ms. |
| US-P04 | Cập nhật BN | `PUT /patients/{id}` | Update audit + re-encrypt nếu id_number đổi. | Form prefill. |
| US-P05 | Xoá BN | `DELETE /patients/{id}` | Soft delete. Block nếu có encounter trong 30 ngày. | Confirm dialog. |
| US-P06 | Lịch sử khám | `GET /patients/{id}/encounters` | Join his_encounter + diagnosis. | Tab Lịch sử khám. |
| US-P07 | Upload avatar | `POST /patients/{id}/avatar` | Validate size 2MB + mime PNG/JPEG; resize 256x256 trước khi lưu MinIO. | Cropper trên FE. |
| US-P08 | Dị ứng | `/patients/{id}/allergies` (GET/POST/DELETE) | Bảng cli_allergies. Update denorm allergies_summary trên patient. | Tab Dị ứng, hiển thị badge SEVERE đỏ. |
| US-P09 | BHYT | `/patients/{id}/insurance` (CRUD) | card_no mã hoá AES-256-GCM trong pat_phi_data. Validate format BHYT 15 ký tự. | Tab BHYT. |
| US-P10 | Liên hệ khẩn cấp | `/patients/{id}/emergency-contacts` (CRUD) | Bảng pat_emergency_contacts. | Tab Liên hệ. |
| US-P11 | Consent | `/patients/{id}/consents` (GET/POST) | document_file_id ref fil_files. | Modal upload PDF + ký. |
| US-P12 | Reception note | `PUT /patients/{id}/reception-note` | Cột mới `reception_note TEXT` trên pat_patients (migration). Hiển thị xuyên phòng. | Textarea inline trên header BN. |

### Reception (US-RC01..RC06)

| Story | Mô tả | Endpoint chính | Ghi chú BE | Ghi chú FE |
|---|---|---|---|---|
| US-RC01 | Tiếp đón | `POST /reception/check-in` | Tạo row `diab_his_rcp_queue_tickets`. Sinh ticket_no nguyên tử (sequence per room+date). | Wizard 3 bước: chọn BN -> dịch vụ -> phòng. |
| US-RC02 | Queue dashboard | `GET /reception/queue` | Index `(tenant_id, status, checked_in_at)`. SSE/poll 10s. | Bảng realtime, badge priority. |
| US-RC03 | Gọi BN | `PUT /reception/queue/{id}/call` | Transition state machine WAITING -> CALLED. Publish event để loa gọi tên. | Nút Gọi trên hàng. |
| US-RC04 | Skip | `PUT /reception/queue/{id}/skip` | Set status SKIPPED, optionally re-queue cuối. | Menu actions. |
| US-RC05 | Huỷ | `PUT /reception/queue/{id}/cancel` | Body reason. Audit log. | Dialog xác nhận. |
| US-RC06 | In phiếu PDF | `GET /reception/queue/{id}/ticket-pdf` | QuestPDF render A6 với QR code (ticketId). | Mở tab mới in trực tiếp. |

### CLS External Uploads (SUNS-I.3, SUNS-VI.3)

| Story | Mô tả | Endpoint | Ghi chú |
|---|---|---|---|
| SUNS-I.3 | LT upload CLS BN mang đến | `POST /patients/{id}/cls-uploads` | Permission `cls_upload.create`. doc_type tự nhập. |
| SUNS-VI.3 | BS xem CLS bên ngoài trong EMR | `GET /encounters/{id}/cls-uploads` | Permission `cls_upload.read`. |
| - | List/view/delete | `GET/DELETE /patients/{id}/cls-uploads/...` | Soft delete. |

### Generic Files (SUNS-I.2, dùng chung)

| Story | Mô tả | Endpoint |
|---|---|---|
| SUNS-I.4/I.5 | Upload file generic | `POST /files/upload` |
| - | Refresh signed URL | `GET /files/{id}/signed-url` |
| - | Xoá file | `DELETE /files/{id}` |

## Permission mới

| Permission | Mô tả |
|---|---|
| `patient.read` / `patient.write` / `patient.delete` | CRUD bệnh nhân |
| `reception.checkin` | Tiếp đón BN |
| `reception.queue.manage` | Gọi/skip/cancel queue |
| `cls_upload.create` / `cls_upload.read` / `cls_upload.delete` | Tài liệu CLS bên ngoài |
| `file.upload` / `file.delete` | File generic |

## Error codes mới

| Code | HTTP | Module |
|---|---|---|
| `PATIENT_NOT_FOUND` | 404 | Patient |
| `PATIENT_CODE_EXISTS` | 409 | Patient |
| `PATIENT_INVALID_AGE` | 422 | Patient |
| `AVATAR_FILE_TOO_LARGE` | 413 | Patient |
| `AVATAR_INVALID_FORMAT` | 415 | Patient |
| `BHYT_CARD_INVALID` | 422 | Insurance |
| `BHYT_EXPIRED` | 422 | Insurance |
| `RECEPTION_ROOM_FULL` | 409 | Reception |
| `RECEPTION_DUPLICATE_CHECKIN` | 409 | Reception |
| `CLS_UPLOAD_INVALID_FORMAT` | 415 | CLS |
| `CLS_UPLOAD_TOO_LARGE` | 413 | CLS |
| `FILE_UPLOAD_FAILED` | 413/422 | Files |
| `FILE_NOT_FOUND` | 404 | Files |

## Trường nhạy cảm (AES-256-GCM)

| Bảng | Cột | Ghi chú |
|---|---|---|
| `pat_pii_data` | `id_number_enc` | CMND/CCCD |
| `pat_phi_data` | `bhyt_card_no_enc` | Mã thẻ BHYT |
| `pat_insurance` | `card_no_enc` | Mã thẻ cho từng record insurance |

API trả về dạng masked (`030185******`), chỉ user có permission `patient.read.pii` (sẽ định nghĩa Sprint sau) mới thấy plain.

## FHIR R4 Mapping

| Entity nội bộ | FHIR Resource |
|---|---|
| `pat_patients` | `Patient` |
| `cli_allergies` | `AllergyIntolerance` |
| `pat_insurance` | `Coverage` |
| `pat_consents` | `Consent` |
| `pat_emergency_contacts` | `RelatedPerson` |
| `diab_his_rcp_queue_tickets` | `Appointment` + `Encounter.status=arrived` |
| `diab_his_fil_cls_uploads` | `DocumentReference` |

## Migration cần thiết

### Migration 0022 — Reception Queue Tickets (MỚI, cần Thảo + DBA review)

```sql
CREATE TABLE diab_his_rcp_queue_tickets (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,
  patient_id UUID NOT NULL REFERENCES pat_patients(id),
  ticket_no VARCHAR(8) NOT NULL,
  room_id UUID NOT NULL,
  doctor_id UUID NULL,
  service_packages_json JSONB NOT NULL DEFAULT '[]'::jsonb,
  reason_for_visit TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'WAITING',
  priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
  checked_in_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  called_at TIMESTAMPTZ,
  started_at TIMESTAMPTZ,
  finished_at TIMESTAMPTZ,
  note TEXT,
  created_by UUID NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_by UUID,
  updated_at TIMESTAMPTZ,
  deleted_at TIMESTAMPTZ
);
CREATE INDEX ix_rcp_queue_tenant_status_time
  ON diab_his_rcp_queue_tickets (tenant_id, status, checked_in_at);
CREATE UNIQUE INDEX ux_rcp_queue_room_date_ticketno
  ON diab_his_rcp_queue_tickets (tenant_id, room_id, (checked_in_at::date), ticket_no)
  WHERE deleted_at IS NULL;
ALTER TABLE diab_his_rcp_queue_tickets ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON diab_his_rcp_queue_tickets
  USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

### Migration 0023 — ALTER pat_patients ADD reception_note + avatar_url

```sql
ALTER TABLE pat_patients
  ADD COLUMN reception_note TEXT,
  ADD COLUMN avatar_url VARCHAR(500);
```

### Bảng đã có (dump cũ, chỉ verify schema)
- `pat_patients`, `pat_pii_data`, `pat_phi_data`
- `pat_insurance`, `pat_emergency_contacts`, `pat_consents`
- `cli_allergies`
- `diab_his_fil_cls_uploads` (migration 0006)
- `fil_files`

## Validation OpenAPI

```bash
npx @redocly/cli lint docs/api/openapi/patients.yaml
npx @redocly/cli lint docs/api/openapi/reception.yaml
npx @redocly/cli lint docs/api/openapi/cls-uploads.yaml
npx @redocly/cli lint docs/api/openapi/files.yaml
```
