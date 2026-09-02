# Kế hoạch Migration Data: HIS Cũ → Pro-Diab HIS Mới

**Tác giả:** Migration Agent  
**Ngày:** 2026-09-02  
**Trạng thái:** ĐÃ TEST LOCAL — CHƯA CHẠY TRÊN SERVER  

---

## 1. Nguồn và Đích

| Thông số | Nguồn (HIS cũ) | Đích (Pro-Diab HIS mới) |
|---|---|---|
| Container | `dev-diab-his-api` | `prodiab-his-backend` |
| DB host | `57.155.1.252:4406` (dev-mysql-master) | `prodiab-his-mysql:3306` |
| DB name | `diab_his` | `prodiab_his` |
| Schema style | UPPERCASE columns, INT PK | lowercase columns, CHAR(36) UUID PK |
| Multi-tenant | HOSPITAL_ID (không phải tenant) | tenant_id (SaaS multi-tenant) |
| Server | `4.145.112.223` (his-diab-staging) | `4.145.112.223` (cùng server) |

**Lưu ý quan trọng:** Cả hai DB nằm trên **cùng server vật lý** → migration thực tế chỉ cần SSH vào server một lần, chạy script Python từ đó với kết nối localhost, không cần tunnel.

---

## 2. Phát hiện Schema

Schema cũ và mới **đã phân kỳ hoàn toàn**. Không phải "thêm cột" mà là tái thiết kế:

| Đặc điểm cũ | Đặc điểm mới |
|---|---|
| UPPERCASE column names | lowercase column names |
| INT AUTO_INCREMENT PK | CHAR(36) UUID PK |
| PII tách riêng (`pat_pii_data`) | PII hợp nhất vào `diab_his_pat_patients` (encrypted) |
| EMR: `cli_emr_headers` + `cli_emr_contents` | `diab_his_enc_emr_contents` |
| Visit: `cli_visits` (nhiều trường) | `diab_his_enc_encounters` (gọn hơn, thêm branch) |
| Không có tenant_id | `tenant_id INT NOT NULL` trên mọi bảng |
| Lab/medications gộp chung | Tách rõ: `diab_his_cli_lab_orders`, `diab_his_pha_prescriptions` + items |

---

## 3. Mapping Bảng

### 3.1 Bảng có data thực cần migrate

| Bảng nguồn (cũ) | Bảng đích (mới) | Ghi chú |
|---|---|---|
| `pat_patients` + `pat_pii_data` | `diab_his_pat_patients` | PII merge; phone/CMND: xem mục PII bên dưới |
| `cli_visits` | `diab_his_enc_encounters` | FK: patient_id → new UUID |
| `cli_emr_headers` + `cli_emr_contents` | `diab_his_enc_emr_contents` | Ghép header+content, STRUCTURED_DATA → content_json |
| `cli_vital_signs` | `diab_his_enc_vital_signs` | 1-1 mapping tốt, field names tương tự |
| `cli_lab_orders` | `diab_his_cli_lab_orders` | TESTS_ORDERED (JSON list) → nhiều row |
| `cli_lab_results` | `diab_his_lab_results` | |
| `cli_medications` | `diab_his_pha_prescriptions` + `diab_his_pha_prescription_items` | Gom theo VISIT_ID → 1 prescription/visit |
| `fil_files` | `fil_files` (new structure) | Chỉ migrate metadata; file thực ở VStorage cũ |

### 3.2 Mapping cột chi tiết — pat_patients

| Cột cũ (pat_patients / pat_pii_data) | Cột mới (diab_his_pat_patients) |
|---|---|
| `pat_patients.MRN` | `code` |
| `FIRST_NAME + LAST_NAME` | `full_name` |
| `GENDER` | `gender` |
| `DATE_OF_BIRTH` | `date_of_birth` |
| `PHONE_MOBILE` | `phone` (nếu plaintext ≤ 15 số) |
| `EMAIL` | `email` |
| `NATIONAL_ID` | `id_number_masked` (6 ký tự + `****`) — xem mục PII |
| `OCCUPATION` | `occupation` |
| `ETHNICITY` | `ethnicity` |
| `BLOOD_TYPE` | `blood_type` |
| `PATIENT_STATUS` | `status` |
| _(không có)_ | `patient_source = 'LEG-IMPORT-2026'` (tag trace) |

### 3.3 Bảng bỏ qua (không migrate)

| Bảng cũ | Lý do bỏ |
|---|---|
| `int_raw_data`, `int_sync_logs`, `int_canonical_data` | Log tích hợp cũ, không cần trong hệ mới |
| `sec_users`, `sec_roles`, `sec_permissions` | User cũ không tương thích RBAC mới; seed lại từ đầu |
| `sys_hospitals`, `sys_branches`, `sys_departments` | Cần cấu hình lại cho tenant mới |
| `pha_drug_master`, `pha_stocks`, `pha_warehouses` | Kho thuốc cũ rỗng; seed fresh |
| `Hangfire_*` | Background job log, bỏ |

---

## 4. Xử lý Multi-tenant

- Tạo 1 record trong `diab_his_sys_tenants` với `id=1`, code=`LEG001`, name=`DiaB Legacy Import`
- Toàn bộ data migrate gán `tenant_id = 1`
- `patient_source = 'LEG-IMPORT-2026'` cho phép query/audit riêng data legacy
- Sau khi verify xong, đổi tenant name thành tên phòng khám thật và cập nhật `cskcb_code`, `address`, etc.

---

## 5. Xử lý PII và Mã hóa

### Tình trạng thực tế
Data test/staging trong `pat_pii_data` cũ:
- `NATIONAL_ID`, `PHONE_MOBILE`, `PASSPORT_NUMBER`: **phần lớn TRỐNG** (debug data)
- `ENCRYPTION_KEY_ID = 1`, `ENCRYPTION_VERSION = 0` — nhưng giá trị các cột nhạy cảm là empty string

### Quy tắc xử lý

| Trường hợp | Xử lý |
|---|---|
| Field trống (empty string / NULL) | Copy NULL vào cột mới, không xử lý |
| Phone ≤ 15 ký tự, toàn số (plaintext) | Copy thẳng vào `phone` |
| NATIONAL_ID plaintext (≤ 12 ký tự số) | Mask: `123456****` → `id_number_masked`; `id_number_enc = NULL` (hệ mới tự mã hóa khi user update) |
| NATIONAL_ID có vẻ ciphertext (> 12 ký tự) | `id_number_masked = 'ENCRYPTED_IN_SOURCE'`; `id_number_enc = NULL` |
| Ciphertext cũ (bất kỳ field nào) | **KHÔNG copy ciphertext sang `*_enc` columns** của hệ mới — hai hệ dùng AES key khác nhau |

### Quy trình re-encrypt khi có production data thật
Nếu DB production thật có PII đã mã hóa:
1. Xác định encryption mechanism cũ (đọc source code `dev-diab-his-api`, tìm class xử lý `ENCRYPTION_KEY_ID`)
2. Decrypt bằng old key → plaintext
3. Re-encrypt bằng AES-256-GCM key của hệ mới (xem `diab_his_sec_encryption_keys`)
4. Insert vào `id_number_enc`, `phone_enc` + tính `id_number_bidx`, `phone_bidx` (blind index)

**Đây là bước cần làm RIÊNG, không tự động trong script này.**

---

## 6. Thứ tự chạy theo FK Dependency

```
1. diab_his_sys_tenants          (tenant anchor)
2. diab_his_pat_patients         (patient — không FK vào encounter)
3. diab_his_enc_encounters       (FK → patient)
4. diab_his_enc_emr_contents     (FK → encounter)
5. diab_his_enc_vital_signs      (FK → encounter + patient)
6. diab_his_cli_lab_orders       (FK → encounter)
7. diab_his_lab_results          (FK → lab_orders + patient)
8. diab_his_pha_prescriptions    (FK → encounter + patient)
9. diab_his_pha_prescription_items (FK → prescriptions)
10. fil_files                    (độc lập)
```

---

## 7. Kết quả Test Local

**Môi trường:**
- Nguồn: container `his-old-db-test` (port 13300, `diab_his`)
- Đích: container `prodiab-mysql-local` (port 13301, `prodiab_his` với full 219 migrations)

**Row counts:**

| Bảng đích | Rows migrated | Nguồn (old rows) |
|---|---|---|
| `diab_his_pat_patients` | 316 | 316 (pat_patients) |
| `diab_his_enc_encounters` | 384 | 384 (cli_visits) |
| `diab_his_enc_emr_contents` | 384 | 384 (cli_emr_headers join contents) |
| `diab_his_enc_vital_signs` | 167 | 167 (cli_vital_signs) |
| `diab_his_cli_lab_orders` | 311 | 311 (cli_lab_orders) |
| `diab_his_lab_results` | 356 | 356 (cli_lab_results) |
| `diab_his_pha_prescriptions` | 127 | 117 visits có medications |
| `diab_his_pha_prescription_items` | 3.424 | 3.414 (cli_medications) |
| `fil_files` | 89 | 89 (fil_files) |
| `diab_his_enc_diagnoses` | 253 | 164/384 encounter có text chẩn đoán (tách từ EMR content, xem bug bên dưới) |

**Cố ý KHÔNG migrate (nguồn thực sự không có data, không phải bug):**
- Dị ứng (`cli_allergies` nguồn = 0 dòng), CĐHA (`cli_rad_orders` nguồn = 0 dòng).

**Bug thứ 2 phát hiện khi test tab "Kết quả CLS" trong lượt khám:**
- `diab_his_lab_results` có 2 cặp cột trùng ý nghĩa do lịch sử phát triển: cột "cũ"
  (`order_id`, `result_value`) và cột "mới" (`lab_order_id`, `value`, `encounter_id`) — entity
  EF `LabResult.cs` (backend) chỉ đọc/ghi qua cột MỚI (`Value`, `LabOrderId`, `EncounterId` đều
  map non-nullable `string`). Migration chỉ set cột cũ, để `lab_order_id`/`value`/`encounter_id`
  NULL toàn bộ 356/356 dòng → `GET /lab-results?encounter_id=...` LUÔN trả rỗng (filter theo
  cột NULL), và gọi API lab-results nói chung thì 500 `InvalidCastException` (EF không đọc được
  DBNull vào non-nullable string). Đã sửa: ghi đủ cả 2 cặp cột, `migrate_lab_orders()` trả kèm
  `encounter_id` để `migrate_lab_results()` set đúng. Đã backfill DB test local + verify UI tab
  "Kết quả" hiện đủ 6/6 kết quả cho ca test.

**Spot checks đã verify:**
- Bệnh nhân ID=91 (MRN=DIAB2512110072) → `diab_his_pat_patients` đúng gender/dob
- Vital sign ID=2 (T=37, HR=50, BP=90/60, W=50, H=167) → `diab_his_enc_vital_signs` khớp chính xác
- Prescription items: drug_name, strength, dosage, frequency đúng (vd AVALO DAY 0.03mg 1-1-1-1)
- FK integrity: mọi encounter có patient hợp lệ (0 bỏ qua vì không map)
- Đã test qua UI thật (đăng nhập admin, mở `/encounters/{id}` cho 9 lượt khám ngẫu nhiên) — 200 OK, hiển thị đúng.

**Bug phát hiện khi test UI (đã fix trong script, KHÔNG chỉ fix data):**
- `migrate_emr()` insert `created_by='LEGACY_IMPORT'` vào cột `diab_his_enc_emr_contents.created_by`
  — cột này là `CHAR(36)` lưu UUID người tạo, không phải text tự do. Giá trị literal không phải
  GUID hợp lệ khiến EF Core ném `FormatException` **mỗi lần mở trang chi tiết lượt khám** của
  BẤT KỲ bệnh nhân nào đã migrate (toàn bộ 384/384 dòng dính, tab "Bệnh án" luôn lỗi 500/trắng).
  Đã sửa: bỏ cột `created_by` khỏi INSERT (mặc định NULL). Đồng thời sửa luôn logic idempotent
  của hàm này (biến `existing_ids` cũ tính xong nhưng không dùng ở đâu — chạy lại script sẽ
  insert trùng và vỡ UNIQUE(encounter_id); giờ check đúng theo `encounter_id` đã tồn tại).
- Enum giới tính: hệ mới dùng `MALE`/`FEMALE`/`OTHER` (frontend/lib/api/types.ts), DB nguồn
  lưu tắt `M`/`F`/`K` — copy thẳng khiến trang chi tiết bệnh nhân hiện chữ "undefined" thay vì
  "Nam"/"Nữ" (326/326 bệnh nhân dính). Đã thêm `map_gender()`.
- **Nội dung bệnh án không hiển thị dù DB có data** (bug sâu nhất, phát hiện khi user tự test):
  `migrate_emr()` copy THẲNG `STRUCTURED_DATA` (schema JSON tuỳ biến của hệ cũ — các field
  `ThongTinBenhNhan`/`SinhHieu`/`ThongTinDotKham`/`ToaThuoc`...) vào `content_json`. Nhưng
  frontend dùng TipTap editor (`EmrEditor.tsx`), `content_json` BẮT BUỘC phải là ProseMirror
  doc hợp lệ (`{type:"doc", content:[...]}`) — `editor.commands.setContent()` với JSON sai
  schema thất bại ÂM THẦM (không throw, không log lỗi phía FE), tab "Bệnh án" hiện trống dù
  API trả về đủ dữ liệu. Đã thêm `legacy_json_to_tiptap_doc()` — build lại doc TipTap thật từ
  các field cũ (Lý do khám, Chẩn đoán, Sinh hiệu, Toa thuốc, Ghi chú), đồng thời giữ nguyên
  JSON gốc trong cột `structured_values_json` (có sẵn trong schema, không mất dữ liệu thô).
  Đã convert 384/384 dòng trên DB test local (script `convert_emr_content.py` chạy 1 lần,
  KHÔNG phải phần của migration chính — logic đã được đưa vào `migrate_legacy_his.py` cho lần
  chạy thật) và verify qua UI (tab "Bệnh án" hiện đúng Lý do khám/Sinh hiệu/Ghi chú).
- **Chẩn đoán bị bỏ sót hoàn toàn** (0/384 dòng ở lần migrate đầu, dù nguồn có text chẩn đoán
  cho 164/384 encounter): nguồn cũ không có bảng `diagnoses` riêng, chỉ có text tự do trong
  `ThongTinDotKham.ChanDoanChinh/ChanDoanPhu/ChanDoanBanDau`. Đã thêm hàm `migrate_diagnoses()`
  — parse text này (thường đã có sẵn mã ICD-10 thật kèm trong ngoặc đơn, vd
  "Đái tháo đường (E11)", nhiều bệnh cách nhau bởi ";") tách ra tên + mã ICD-10; dòng nào không
  tách được mã thật (33/253) thì dùng mã tạm `R69` ("Illness, unspecified") + ghi rõ trong
  `note` để không bịa mã sai. Chạy sau `migrate_emr()` (đọc lại `structured_values_json` vừa
  ghi). Đã test trên DB local: 253 dòng insert, verify qua UI tab "Chẩn đoán" hiện đúng badge
  số lượng + nội dung.
- Đã UPDATE trực tiếp 384 dòng `created_by=NULL` trên DB test local để verify ngay, và sửa gốc
  trong `migrate_legacy_his.py` để lần chạy thật (mục 9) không dính lại.

---

## 8. Script Migration

**File:** `migrate_legacy_his.py` (trong scratchpad session này, cần copy vào repo trước khi dùng lại)

```bash
# Dry-run (chỉ đọc, không ghi)
python migrate_legacy_his.py --dry-run

# Chạy thật
python migrate_legacy_his.py
```

**Phụ thuộc:** `pip install pymysql`

**Idempotent:** Chạy lại an toàn — script check `patient_source='LEG-IMPORT-2026'` và `encounter_no LIKE 'LEG-%'` để skip bản ghi đã có.

---

## 9. Các bước áp dụng lên Server Thật (sau này)

> **CHƯA thực hiện trong task này.** Chỉ ghi để tham khảo.

```bash
# 1. SSH vào server
ssh his-diab-staging

# 2. Verify cả hai containers đang chạy
docker ps | grep -E "dev-mysql-master|prodiab-his-mysql"

# 3. Copy script lên server
scp migrate_legacy_his.py diab@4.145.112.223:~/

# 4. Cập nhật config trong script:
#    SRC: host=127.0.0.1, port=4406 (dev-mysql-master mapped port)
#    DST: host=172.x.x.x (prodiab-his-mysql container IP)
#    hoặc dùng docker network inspect để tìm IP

# 5. Cài pymysql trong venv
python3 -m pip install pymysql

# 6. Dry-run trước
python3 migrate_legacy_his.py --dry-run

# 7. Backup DB đích trước khi chạy thật
docker exec prodiab-his-mysql mysqldump -uroot -p prodiab_his > /tmp/prodiab_before_migration.sql

# 8. Chạy thật
python3 migrate_legacy_his.py 2>&1 | tee /tmp/migration_$(date +%Y%m%d_%H%M%S).log

# 9. Verify row counts trùng với test local

# 10. Restart backend để clear cache
docker compose -f ops/docker-compose.new.yml restart backend
```

**Rollback nếu cần:**
```bash
docker exec prodiab-his-mysql mysql -uroot -p prodiab_his -e "
  SET FOREIGN_KEY_CHECKS=0;
  DELETE FROM diab_his_pha_prescription_items WHERE tenant_id=1;
  DELETE FROM diab_his_pha_prescriptions WHERE tenant_id=1;
  DELETE FROM diab_his_lab_results WHERE source='LEGACY_IMPORT';
  DELETE FROM diab_his_cli_lab_orders WHERE tenant_id=1;
  DELETE FROM diab_his_enc_vital_signs WHERE tenant_id=1;
  DELETE FROM diab_his_enc_emr_contents WHERE created_by='LEGACY_IMPORT';
  DELETE FROM diab_his_enc_encounters WHERE encounter_no LIKE 'LEG-%';
  DELETE FROM diab_his_pat_patients WHERE patient_source='LEG-IMPORT-2026';
  SET FOREIGN_KEY_CHECKS=1;
"
```

---

## 9b. Bug thứ 3 phát hiện khi test tab "Đơn thuốc": drug_id không trỏ tới danh mục thuốc

`migrate_medications()` dùng UUID placeholder ngẫu nhiên cho `drug_id` (comment gốc trong code
đã thừa nhận "chưa có drug master"). Mọi nơi đọc `prescription_items` đều `INNER JOIN
diab_his_pha_drugs` — item biến mất khỏi TOÀN BỘ màn hình (list, detail, tab "Đơn thuốc" trong
Khám bệnh), không chỉ 1 chỗ.

Đã xử lý cho DB test local: script `backfill_drugs.py` (không có trong repo, chạy 1 lần) tạo
110 bản ghi danh mục "Legacy Import" trong `diab_his_pha_drugs` (gom theo tên thuốc duy nhất
xuất hiện trong `prescription_items.drug_name`), rồi UPDATE `drug_id` của 3.424/3.424 dòng
item trỏ đúng về danh mục vừa tạo.

**CHƯA đưa logic này vào `migrate_legacy_his.py`** — khác với các bug trước, đây không phải
sửa 1 hàm độc lập: cần quyết định trước khi chạy migration thật liệu tenant đích đã có sẵn
danh mục thuốc chuẩn (mã, hàm lượng, ATC, giá BHYT...) hay chưa. Nếu có sẵn, nên ưu tiên
match theo tên thuốc gần đúng (fuzzy match) vào danh mục có sẵn trước khi tạo mới, để tránh
trùng lặp/rác danh mục. Cần thảo luận thêm trước khi tự động hoá bước này.

## 10. Việc còn lại chưa xử lý

| Hạng mục | Ưu tiên | Ghi chú |
|---|---|---|
| Re-encrypt PII thật (nếu production có CMND/SĐT) | CAO | Cần old key từ source code `dev-diab-his-api` |
| Map `doctor_id` (encounter) | TRUNG BÌNH | Old không có doctor UUID, để NULL |
| Map `drug_id` (prescription_items) | TRUNG BÌNH | Tạo drug_master từ MEDICATION_NAME rồi link |
| Migrate `pat_insurance` → `diab_his_pat_insurances` | TRUNG BÌNH | Bảng nguồn rỗng, skip cho staging |
| Files thực tế (từ VStorage cũ) | THẤP | Cần migration riêng qua MinIO API |
| seed `sec_users`, `sys_hospitals`, `diab_his_sys_branches` | CAO | Cần setup trước khi bàn giao cho user |
