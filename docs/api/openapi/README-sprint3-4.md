# Pro-Diab HIS — OpenAPI Specs Sprint 3-4 (EPIC 3)

Tác giả: Lành (Architect)

> Đây là spec **bổ sung** cho Sprint 3-4 EPIC 3 (Encounter + Vital Signs + EMR + ĐTĐ + CLS Orders + ICD-10).
> Sprint 1 (Tenant/Users/RBAC/Audit) tham khảo `README.md` cùng thư mục.

## File mục lục

| File | Endpoints | Story map |
|------|-----------|-----------|
| `encounters.yaml` | 11 paths / ~15 op | US-E01..E07, E10, E12, E15 |
| `vital-signs.yaml` | 6 paths / 8 op | US-N01..N03, US-SUNS-09 |
| `emr.yaml` | 10 paths / 12 op | US-E08, E09, E11, E13, E14, US-SUNS-06..08 |
| `diabetes.yaml` | 4 paths / 6 op | US-E04 (chuyên khoa ĐTĐ) |
| `cls-orders.yaml` | 6 paths / 10 op | US-E08 (chỉ định CLS, result Sprint 5) |
| `icd10.yaml` | 3 paths / 3 op | US-E07 (lookup chẩn đoán) |

**Tổng: ~60 endpoints** trên 40 paths.

## Story coverage matrix

| Story | File | Endpoint chính |
|-------|------|----------------|
| US-E01 Tạo encounter | encounters | POST /encounters |
| US-E02 List/filter | encounters | GET /encounters |
| US-E03 Detail | encounters | GET /encounters/{id} |
| US-E04 ĐTĐ assessment | diabetes | POST /encounters/{id}/diabetes-assessment |
| US-E05 Start (WAITING->IN_PROGRESS) | encounters | POST /encounters/{id}/start |
| US-E06 Chief complaint | encounters | PUT /encounters/{id}/chief-complaint |
| US-E07 Chẩn đoán ICD-10 | encounters + icd10 | POST /encounters/{id}/diagnoses, GET /icd10/search |
| US-E08 Save EMR draft + chỉ định CLS | emr + cls-orders | PUT /encounters/{id}/emr, POST .../lab-orders, .../rad-orders |
| US-E09 Ký số EMR | emr | POST /encounters/{id}/emr/sign |
| US-E10 Đóng encounter | encounters | POST /encounters/{id}/close |
| US-E11 Unsign (admin) | emr | POST /encounters/{id}/emr/unsign |
| US-E12 Timeline | encounters | GET /encounters/{id}/timeline |
| US-E13 Export PDF | emr | GET /encounters/{id}/emr/pdf |
| US-E14 Version history | emr | GET /encounters/{id}/emr/versions |
| US-E15 Alert >12h | encounters | GET /encounters/alerts/over-12h |
| US-N01 Nhập sinh hiệu | vital-signs | POST /encounters/{eid}/vital-signs |
| US-N02 Latest vital cho EMR | vital-signs | GET .../vital-signs/latest |
| US-N03 Trend đa encounter | vital-signs | GET /patients/{pid}/vital-signs/history |
| US-SUNS-06 List template | emr | GET /emr-templates |
| US-SUNS-07 Tạo template custom | emr | POST /emr-templates |
| US-SUNS-08 Ký số + cert | emr | POST .../emr/sign |
| US-SUNS-09 Điều dưỡng nhập sinh hiệu | vital-signs | POST .../vital-signs |

## Schema chính
- `EncounterResponse` / `EncounterDetailResponse` (lifecycle + computed `alert_over_12h`)
- `VitalSignsRequest/Response` (validation range, BMI computed)
- `EmrContentResponse` (Tiptap JSON + chữ ký số + version)
- `DiabetesAssessmentRequest/Response` (HbA1c, complications, treatment_target)
- `LabOrderResponse` / `RadOrderResponse` (status workflow)
- `Icd10Response` (full-text VN)

## Error codes mới
```
ENCOUNTER_NOT_FOUND, ENCOUNTER_ALREADY_CLOSED, ENCOUNTER_INVALID_TRANSITION,
ENCOUNTER_MISSING_DIAGNOSIS, ENCOUNTER_OVER_12H_ALERT
VITAL_INVALID_RANGE, VITAL_EDIT_TIMEOUT
EMR_ALREADY_SIGNED, EMR_NOT_SIGNED, EMR_SIGNATURE_INVALID, EMR_USB_TOKEN_ERROR
DIABETES_ASSESSMENT_EXISTS, DIABETES_INVALID_HBA1C
LAB_ORDER_NOT_FOUND, LAB_ORDER_CANNOT_DELETE
ICD10_NOT_FOUND
```

## Permission mới (seed migration 0030)
```
encounter.read, encounter.create, encounter.update, encounter.start, encounter.close
vital_sign.read, vital_sign.write, vital_sign.delete
emr.read, emr.write, emr.sign, emr.unsign, emr.export
emr_template.read, emr_template.write
diabetes.assess
lab_order.read, lab_order.create, lab_order.update, lab_order.delete
rad_order.read, rad_order.create, rad_order.update, rad_order.delete
icd10.read
```

Default role mapping:
- **BacSi**: encounter.* (trừ delete), vital_sign.read, emr.*, diabetes.assess, lab_order.*, rad_order.*, icd10.read
- **DieuDuong (Nurse)**: encounter.read/update, vital_sign.*, emr.read, lab_order.read/update (sample_taken), icd10.read
- **Admin**: tất cả + emr.unsign
- **LeTan**: encounter.read/create, vital_sign.read
- **KyThuatVien**: lab_order.read/update, rad_order.read/update

---

## Migration cần (note cho Thảo — backend)

> Lưu ý naming: dự án legacy dump dùng prefix `diab_his_cli_*` (MySQL).
> Sprint 3-4 này theo CLAUDE.md → PostgreSQL prefix `his_*`.
> Khi viết migration, **Thảo confirm engine target** (PG mới vs migrate từ MySQL).
> Skeleton dưới đây viết Postgres-flavor; nếu giữ MySQL thì convert `gen_random_uuid()` → `UUID()`, `JSONB` → `JSON`, `TIMESTAMPTZ` → `DATETIME`.

### 0025_create_encounter_extensions.sql
Kiểm tra `his_encounter` (hoặc legacy `cli_visits`) đã có:
- `encounter_type ENUM/VARCHAR`
- `status ENUM(WAITING, IN_PROGRESS, DONE, CANCELLED)`
- `started_at TIMESTAMPTZ NULL`
- `finished_at TIMESTAMPTZ NULL`
- `chief_complaint TEXT NULL`
- `reason_for_visit VARCHAR(1000)`
- `alert_sent_at TIMESTAMPTZ NULL` (cho job >12h)

Dùng `add_col_if_missing(table, col, type)` helper function. Thêm index:
- `idx_encounter_tenant_status_started ON (tenant_id, status, started_at)`
- `idx_encounter_patient_created ON (tenant_id, patient_id, created_at DESC)`

Thêm bảng `his_encounter_diagnoses` (id, tenant_id, encounter_id, icd10_code, type ENUM, note, audit) — nếu chưa có.

### 0026_create_emr_templates.sql
```
his_emr_templates (
  id UUID PK,
  tenant_id UUID NULL,   -- NULL = system template
  name VARCHAR(200),
  content_json JSONB NOT NULL,
  speciality VARCHAR(50),
  is_system BOOLEAN DEFAULT false,
  + audit cols
)
```
Seed: 1 General + 1 Diabetes (Tiptap JSON với section: Lý do khám / Tiền sử / Khám lâm sàng / CLS / Chẩn đoán / Hướng xử trí).

### 0027_create_emr_signatures.sql
```
his_emr_signatures (
  id UUID PK,
  tenant_id UUID,
  emr_id UUID FK -> his_emr_content,
  encounter_id UUID,
  signed_at TIMESTAMPTZ,
  signed_by UUID FK -> his_users,
  certificate_serial VARCHAR(128),
  certificate_subject TEXT,
  signature_algorithm VARCHAR(50) DEFAULT 'SHA256withRSA',
  signature_data BYTEA NOT NULL,   -- PKCS#7 detached
  + audit
)
```
**Cột nhạy cảm:** `signature_data` lưu nguyên bản (đã là chữ ký số), nhưng audit access log bắt buộc.

Bảng `his_emr_content` (nếu chưa có): id, tenant_id, encounter_id UNIQUE, content_json JSONB, content_html TEXT, template_id, version INT DEFAULT 1, signed_at, signed_by, audit.

Bảng `his_emr_versions`: snapshot mỗi lần save (id, emr_id, version, content_json, saved_at, saved_by, bytes_size).

### 0028_seed_icd10.sql
- Bảng `his_dict_icd10` đã có ở migration 0018 — sprint này seed **đầy đủ ICD-10 VN ~10000 mã** từ CSV.
- File CSV: Thảo download từ Bộ Y tế — migration chỉ scaffold INSERT mẫu E10-E14 + comment `-- TODO DBA: load full CSV via \copy`.
- Index full-text (Postgres):
  ```sql
  ALTER TABLE his_dict_icd10 ADD COLUMN search_tsv tsvector
    GENERATED ALWAYS AS (
      to_tsvector('simple', coalesce(code,'') || ' ' || coalesce(name_vi,'') || ' ' || coalesce(name_en,''))
    ) STORED;
  CREATE INDEX idx_icd10_tsv ON his_dict_icd10 USING GIN(search_tsv);
  ```
- Nếu engine MySQL: `FULLTEXT(name_vi, name_en) WITH PARSER ngram`.

### 0029_create_diabetes_history.sql
- Bảng `his_diabetes_assessments` đã có ở 0015 — sprint này ADD:
  ```sql
  CREATE INDEX idx_dm_patient_date ON his_diabetes_assessments(tenant_id, patient_id, assessed_at DESC);
  ```
- ADD cột nếu thiếu: `treatment_target JSONB`, `waist_circumference NUMERIC(5,2)`, `urine_acr NUMERIC(8,2)`, `complications JSONB`.

### 0030_seed_permissions_sprint3.sql
Seed các permission + role mapping ở mục trên.

### 0031_create_lab_rad_orders.sql (nếu chưa có)
```
his_lab_orders (id, tenant_id, encounter_id, test_code, test_name, sample_type,
                priority, status, ordered_at, ordered_by, scheduled_for, lab_partner_id, note, audit)
his_rad_orders (id, tenant_id, encounter_id, modality, body_part, contrast,
                procedure_code, procedure_name, priority, status, ordered_at, ordered_by, note, audit)
his_dict_lab_tests (code PK, name, sample_type, default_price, bhyt_price, is_active)
his_dict_rad_procedures (code PK, name, modality, default_price, bhyt_price, is_active)
```
Index: `(tenant_id, encounter_id)`, `(tenant_id, status, ordered_at)`.

### RLS policies (mọi bảng mới)
```sql
ALTER TABLE his_xxx ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON his_xxx
  USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

---

## Background Job (note cho Thảo)

**Job name:** `EncounterOver12hAlertJob`
**Schedule:** mỗi 10 phút (Hangfire recurring).
**Logic:**
```sql
SELECT id, tenant_id, patient_id, doctor_id, started_at
FROM his_encounter
WHERE status = 'IN_PROGRESS'
  AND started_at < (now() - interval '12 hours')
  AND alert_sent_at IS NULL
  AND deleted_at IS NULL;
```
Với mỗi row: insert vào `his_notifications` (target doctor + admin), audit log, UPDATE `alert_sent_at = now()`.

**Hangfire storage:** dùng PostgreSQL (Hangfire.PostgreSql) thay vì MySQL (CLAUDE.md mục 2 đã chốt PG17). Cấu hình ở `Program.cs`:
```csharp
services.AddHangfire(cfg => cfg.UsePostgreSqlStorage(connStr));
services.AddHangfireServer();
RecurringJob.AddOrUpdate<EncounterOver12hAlertJob>(
  "encounter-over-12h",
  j => j.Execute(),
  "*/10 * * * *");
```

---

## FHIR R4 mapping

| Entity | FHIR Resource |
|--------|---------------|
| Encounter | `Encounter` (class, status, period.start/end, participant=doctor, reasonCode) |
| Diagnosis | `Condition` (code=ICD-10, encounter ref, category=encounter-diagnosis) |
| VitalSigns | `Observation` (category=vital-signs, code=LOINC: 8867-4 HR, 8480-6 SBP, 8462-4 DBP, 8310-5 Temp, 9279-1 RR, 59408-5 SpO2, 29463-7 Weight, 8302-2 Height, 39156-5 BMI, 38208-5 PainScale) |
| Diabetes Assessment | `Observation` panel (HbA1c=4548-4, fasting=1558-6, eGFR=98979-8, ACR=14959-1) + `Condition` complications |
| LabOrder | `ServiceRequest` (category=laboratory) |
| RadOrder | `ServiceRequest` (category=imaging) |
| EMR | `Composition` (type=11488-4 Consultation note, section[], attester=signed_by) — export PDF khi cần |

---

## Trade-off ghi nhận (sẽ viết ADR riêng)

1. **Tiptap JSON vs HTML thuần cho EMR**: chọn JSON để diff/version chuẩn xác, render HTML cache khi save. Trade-off: tăng dung lượng ~15%, nhưng version diff/migrate format dễ.
2. **Vital N record/encounter vs 1 record**: chọn N để theo dõi diễn biến (nhập viện ngắn ngày, theo dõi sốt). EMR luôn dùng `latest` để tránh ambiguity.
3. **EMR sign = USB token (PKCS#7) vs server-side HSM**: Sprint 3-4 làm USB token (client-side ký, server verify + lưu). HSM hoãn sang Sprint 8+.
