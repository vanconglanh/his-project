# DB Migrations — Pro-Diab HIS

Engine: MySQL 8.0+, InnoDB, utf8mb4_0900_ai_ci  
Database: `diab_his` (production read-only dump: `db/diab_his_*.sql`)  
Migrations folder: `db/migrations/` (ADD-only, không sửa dump gốc)

---

## 1. Inventory bảng cũ (63 bảng từ dump production)

### Nhóm bệnh nhân — pat\_\* (7 bảng)
| Bảng | Mô tả |
|---|---|
| pat_patients | Hồ sơ bệnh nhân (thông tin nhân khẩu, trạng thái) |
| pat_pii_data | Thông tin định danh cá nhân (CMND, địa chỉ — encrypted) |
| pat_phi_data | Thông tin sức khỏe bảo mật (PHI — encrypted) |
| pat_insurance | Thẻ BHYT, thông tin bảo hiểm |
| pat_consents | Chấp thuận điều trị của bệnh nhân |
| pat_emergency_contacts | Người liên hệ khẩn cấp |
| pat_privacy_settings | Cài đặt quyền riêng tư của bệnh nhân |

### Nhóm lâm sàng — cli\_\* (9 bảng)
| Bảng | Mô tả |
|---|---|
| cli_visits | Lượt khám (encounter header) |
| cli_emr_headers | Tiêu đề hồ sơ bệnh án điện tử |
| cli_emr_contents | Nội dung chi tiết EMR (MEDIUMTEXT — bảng lớn) |
| cli_lab_orders | Phiếu chỉ định xét nghiệm |
| cli_lab_results | Kết quả xét nghiệm |
| cli_rad_orders | Phiếu chỉ định CĐHA |
| cli_rad_results | Kết quả CĐHA |
| cli_medications | Thuốc trong đơn/phác đồ |
| cli_vital_signs | Dấu hiệu sinh tồn |
| cli_allergies | Dị ứng thuốc/thức ăn của bệnh nhân |
| cli_treatment_monitoring | Theo dõi điều trị |

### Nhóm dược — pha\_\* (5 bảng)
| Bảng | Mô tả |
|---|---|
| pha_drug_master | Danh mục thuốc |
| pha_prescriptions | Đơn thuốc |
| pha_stocks | Tồn kho thuốc (lô, hạn dùng) |
| pha_transactions | Giao dịch xuất/nhập dược |
| pha_warehouses | Kho dược |

### Nhóm nhân sự — sta\_\* (9 bảng)
| Bảng | Mô tả |
|---|---|
| sta_doctors | Thông tin bác sĩ |
| sta_staff | Thông tin nhân viên |
| sta_schedules | Lịch làm việc |
| sta_certifications | Bằng cấp, chứng chỉ |
| sta_qualifications | Năng lực chuyên môn |
| sta_salary_info | Thông tin lương (encrypted) |
| sta_performance_reviews | Đánh giá hiệu suất |
| sta_work_experience | Kinh nghiệm làm việc |
| sta_department_assignments | Phân công khoa/phòng |

### Nhóm bảo mật — sec\_\* (9 bảng)
| Bảng | Mô tả |
|---|---|
| sec_users | Tài khoản người dùng |
| sec_roles | Vai trò (RBAC) |
| sec_permissions | Quyền chi tiết |
| sec_user_roles | Gán role cho user |
| sec_role_permissions | Gán permission cho role |
| sec_sessions | Phiên đăng nhập |
| sec_audit_logs | Nhật ký kiểm toán |
| sec_data_masks | Cấu hình masking dữ liệu |
| sec_encryption_keys | Khóa mã hóa (AES-256-GCM) |

### Nhóm hệ thống — sys\_\* (5 bảng)
| Bảng | Mô tả |
|---|---|
| sys_branches | Chi nhánh phòng khám |
| sys_departments | Khoa/phòng |
| sys_hospitals | Cơ sở y tế |
| sys_rooms | Phòng khám |
| sys_beds | Giường bệnh |

### Nhóm khác (19 bảng)
| Bảng | Mô tả |
|---|---|
| bil_billing | Hóa đơn, thanh toán |
| cdss_rules | Quy tắc hỗ trợ ra quyết định lâm sàng |
| equ_equipment | Thiết bị y tế |
| equ_calibration | Lịch hiệu chỉnh thiết bị |
| equ_maintenance | Bảo trì thiết bị |
| fil_files | File đính kèm (MinIO metadata) |
| fil_file_versions | Phiên bản file |
| inv_consumables | Vật tư tiêu hao |
| or_rooms | Phòng mổ |
| or_surgeries | Ca phẫu thuật |
| rep_reports | Báo cáo |
| sch_doctor_schedules | Lịch trực bác sĩ |
| int_canonical_data | Dữ liệu chuẩn hóa (tích hợp) |
| int_data_mappings | Ánh xạ dữ liệu giữa hệ thống |
| int_raw_data | Dữ liệu thô từ hệ thống ngoài (bảng lớn) |
| int_schema_registry | Registry schema tích hợp |
| int_sync_logs | Log đồng bộ dữ liệu |
| sys_systems | Thông tin cấu hình hệ thống |

---

## 2. Mapping User Story → Bảng/Cột

| Story ID | Bảng chính | Cột liên quan | Migration |
|---|---|---|---|
| US-SUNS-01 | pat_patients | avatar_url | 0004 |
| US-SUNS-02 | pat_patients | reception_note | 0004 |
| US-SUNS-03, 04, 05 | diab_his_fil_cls_uploads | doc_type, file_path | 0006 |
| US-SUNS-08, 10, 11, 12 | diab_his_api_partners, _scopes, _request_logs | api_key_hash, scope, status_code | 0008 |
| US-SUNS-09 | diab_his_sch_appointments | appointment_at, source, status | 0016 |
| US-SUNS-13, 14, 15 | diab_his_int_lab_partners, _orders_outbound, _results_inbound | status, payload_json | 0007 |
| US-SUNS-16, 17, 18 | diab_his_nti_notifications, _user_preferences, _web_push_subscriptions | type, read_at, endpoint | 0009 |
| US-SUNS-19, US-N01 | sec_roles | CODE=DIEUDUONG | 0010 |
| US-SUNS-20, 21 | bil_billing, diab_his_bil_qr_codes | payment_method_v2, qr_payload | 0014 |
| US-SUNS-22, US-EMR-PORTAL-01..03 | diab_his_pat_portal_accounts, _tokens | phone_e164, otp_code_hash | 0017 |
| US-N02 | cli_vital_signs | recorded_at, record_sequence | 0005 |
| US-PR-04, 05 | diab_his_int_dtqg_submissions, _credentials | ma_don_thuoc, status | 0011 |
| US-BH-01..05 | diab_his_int_bhyt_exports, _export_items | period_month, table_no | 0012 |
| US-PH-01..05 | pha_stocks (gtin), diab_his_pha_stock_movements | movement_type, quantity | 0013 |
| US-EMR-DM-01..03 | diab_his_cli_diabetes_assessments, _templates | hba1c, complications_json | 0015 |
| US-RC-04 | diab_his_sch_appointments | status, source | 0016 |
| US-TENANT-01..04 | diab_his_sys_tenants | code, cskcb_code, subdomain | 0001 |
| US-AUDIT-01 | (all tables) | updated_by, deleted_at | 0003 |
| US-PERF-01, 02 | (all tables) | INDEX (tenant_id) | 0019 |
| US-MASTER-01, 02 | diab_his_dict_drug_units, _icd10, _doc_types | code, name | 0018 |

---

## 3. ADR Pending

### ADR-001: Multi-tenant Strategy

**Tình trạng:** Pending quyết định  
**Vấn đề:** Hiện tại dùng application-layer filter (WHERE tenant_id = ?). Cần quyết định có nên dùng MySQL Views per tenant hoặc schema-per-tenant không.

**Các lựa chọn:**
- **A. App-layer filter** (hiện tại): đơn giản, nhưng dễ bỏ sót WHERE clause → data leak.
- **B. MySQL Views per tenant**: tạo view `v_{tenant_id}_pat_patients` → ổn định hơn nhưng quản lý phức tạp.
- **C. Schema per tenant**: isolated hoàn toàn nhưng migration cost cao khi có 100+ tenant.

**Gợi ý:** Giữ A (phase 1) + bổ sung middleware kiểm tra mandatory WHERE tenant_id ở tầng repository.

---

### ADR-002: Encryption Strategy

**Tình trạng:** Pending xác nhận danh sách cột  
**Vấn đề:** Cột nào cần AES-256-GCM (qua `IEncryptionService`)? Hiện có `sec_encryption_keys` table nhưng chưa rõ rotation strategy.

**Cột candidate encrypt:**
- `pat_pii_data`: CMND/CCCD, địa chỉ chi tiết
- `pat_phi_data`: kết quả xét nghiệm nhạy cảm, ghi chú bệnh án
- `pat_insurance`: số thẻ BHYT
- `sta_salary_info`: mức lương
- `diab_his_int_dtqg_credentials`: token_encrypted
- `diab_his_int_lab_partners`: credentials_encrypted

**Cần quyết định:** Key rotation frequency, master key storage (HSM vs KMS vs DB table), IV/nonce per-row hay per-column.

---

### ADR-003: BHYT XML Format Detail

**Tình trạng:** Pending review pháp lý  
**Vấn đề:** QĐ 4750/QĐ-BYT định nghĩa 5 bảng XML nhưng format thực tế khác nhau giữa cổng giám định các tỉnh.

**5 bảng QĐ 4750:**
- Bảng 1: Hồ sơ KCB (thông tin bệnh nhân, lượt khám, chẩn đoán)
- Bảng 2: Thuốc sử dụng (tên, hàm lượng, số lượng, đơn giá)
- Bảng 3: Dịch vụ kỹ thuật (tên DVKT, mã BHYT)
- Bảng 4: CĐHA và XN (kết quả, chi phí)
- Bảng 5: Tổng hợp chi phí KCB

**Cần xác nhận:** Namespace XML, version schema mới nhất (một số tỉnh dùng 4210 thay 4750), encoding (UTF-8 vs Windows-1252 legacy).

---

## 4. Cảnh báo quan trọng

1. **MySQL 8.0.23 + ALGORITHM=INSTANT**: Bảng `int_raw_data` và `cli_emr_contents` có thể rất lớn. ADD COLUMN với 8.0.23 không đảm bảo INSTANT cho mọi kiểu cột — cần test với `EXPLAIN ALTER TABLE` trước production.

2. **FULLTEXT ngram**: Index `ft_full_name` trên `pat_patients.FULL_NAME` phải tạo thủ công (stored proc không hỗ trợ `PREPARE` với `FULLTEXT ... WITH PARSER`). Xem hướng dẫn trong `0019_create_indexes.sql`.

3. **Tenant_id phase 2**: Sau khi backfill dữ liệu cũ, cần migration riêng để đổi `tenant_id INT NULL` → `INT NOT NULL` cho các bảng quan trọng.

4. **Column naming**: Schema cũ dùng UPPERCASE (ID, CODE, NAME). Migration mới dùng lowercase. Code mới phải dùng backtick quote khi query để tránh case-sensitive issue trên Linux MySQL.

---

## 5. Trạng thái migration chain (kiểm chứng thật 2026-08-20, DevOps)

Đã dựng MySQL 8.0 sạch bằng Docker (container tạm, đã xóa sau khi test), nạp 64 file
`db/diab_his_*.sql` (0 lỗi, 64 bảng) rồi apply tuần tự 150 file `db/migrations/*.sql`.
**Kết quả thật: 30/150 file lỗi.** Xem phân tích nguyên nhân gốc chi tiết ở `APPLY_ORDER.md`
mục "TRẠNG THÁI THẬT". Tóm tắt bảng dưới đây (nguyên văn lỗi MySQL thật, không suy đoán):

| File | Lỗi thật | Nguyên nhân | Xử lý |
|---|---|---|---|
| 0005_vital_signs_multi_record.sql | `Key column 'encounter_id' doesn't exist` | Bảng `cli_vital_signs` base dump chỉ có `VISIT_ID`, không có `encounter_id` | **Chưa sửa** — cần backend xác nhận cột đúng |
| 0018_seed_master_data.sql | `Column count doesn't match value count at row 7` | INSERT sai số cột | **Chưa sửa** — cần rà lại INSERT |
| 0021, 0024, 0039_seed_permissions_*.sql | `Unknown column 'resource'` | INSERT vào bảng permission cột không tồn tại ở thời điểm chạy | Liên quan mục 2 dưới — thứ tự phụ thuộc |
| 0029_create_diabetes_history.sql | `Unknown column 'default_values'` | Cột không tồn tại trên bảng đích | **Chưa sửa** |
| 0030, 0066_seed_p0_permissions.sql | `Table 'diab_his_sec_permissions' doesn't exist` | Bảng này chỉ tạo ở `9001_create_sec_all.sql` (chạy SAU theo thứ tự tên file) | **Lỗi thứ tự phụ thuộc — cần APPLY_ORDER reorder, KHÔNG tự sửa vì rủi ro** |
| 0032_lab_rad_results.sql | `BLOB/TEXT column 'conclusion' can't have a default value` | Lỗi cú pháp MySQL thật | **Chưa sửa** |
| 0034, 0047_seed_permissions_*.sql | `Table 'iam_permissions'/'diab_his_iam_permissions' doesn't exist` | Bảng chưa từng được tạo ở bất kỳ file nào tìm thấy | **Chưa sửa — nghi thiếu migration tạo bảng, cần backend xác nhận** |
| 0035_create_prescription_extensions.sql | `CREATE INDEX IF NOT EXISTS` cú pháp sai + cột `doctor_id`/`dtqg_code` không tồn tại trên `pha_prescriptions` base dump (cột thật: `PRESCRIBING_PROVIDER_ID`...) | Cú pháp (b) + cột sai (d) | **Chưa sửa — rủi ro đoán sai cột trên bảng production** |
| 0036_drug_master_extensions.sql | `CREATE UNIQUE INDEX IF NOT EXISTS` cú pháp sai | (b) | **Chưa sửa** (cùng dạng 0058, chờ quyết định chung) |
| 0044, 0052, 0057_seed_permissions_*.sql | `Unknown column 'module'/'is_active'` | Cột sai/chưa tồn tại | **Chưa sửa** |
| 0045_bhyt_export_extensions.sql | `ADD COLUMN IF NOT EXISTS` cú pháp sai (nhiều dòng) | (b) | **Chưa sửa** — file dài, cần review kỹ trước khi đổi hàng loạt |
| 0048_create_appointments_extensions.sql | `Duplicate column name 'source_partner_id'` | Cột đã được thêm bởi migration khác trước đó, ADD COLUMN không có guard | **Chưa sửa — cần xác định file đã thêm trước để bỏ trùng, không tự xoá cột vì có thể đã có dữ liệu** |
| 0054_seed_permissions_sprint11.sql | `Unknown column 'updated_at'` | Cột sai | **Chưa sửa** |
| 0056_audit_log_extensions.sql | `ADD COLUMN IF NOT EXISTS` cú pháp sai | (b) | **Chưa sửa** |
| 0058_perf_indexes.sql | (sau khi sửa cú pháp) `Unknown column 'doctor_id'/'prescribed_at'` trên `pha_prescriptions` | (b) đã sửa cú pháp sang `add_index_if_missing`; còn lỗi (d) cột không tồn tại | **Đã sửa 1 phần (cú pháp), còn lỗi cột — cần backend xác nhận tên cột đúng** |
| 0059_fhir_extensions.sql | `ADD COLUMN IF NOT EXISTS fhir_id` cú pháp sai | (b) | **Chưa sửa** |
| 0060_seed_permissions_sprint13.sql | `Table 'diab_his_permissions' doesn't exist` | Tên bảng sai (thiếu `sec_`?) hoặc thứ tự phụ thuộc | **Chưa sửa** |
| 0062_fix_queue_tickets_patient_id_type.sql | Cú pháp lỗi dòng 13 | Cần xem chi tiết statement | **Chưa sửa** |
| 0063_seed_pharmacy_stock.sql | `Unknown column 'is_active'` | Cột sai | **Chưa sửa** |
| 9011_create_missing_tables.sql | `'diab_his_int_bhyt_exports' is not VIEW` | Bảng cùng tên đã tồn tại trước khi tạo VIEW | **Chưa sửa — cần bọc điều kiện kiểm tra TABLE_TYPE trước CREATE VIEW, việc này an toàn (no-op) nhưng chưa kịp làm** |
| 9014_fix_dtqg_apipartners_schema.sql | Cú pháp lỗi dòng 8 | Có thể do DELIMITER trong file | **Chưa sửa** |
| 9020_seed_rich_demo.sql | `Unknown column 'user_id'` | Dữ liệu demo DEV-only, có ghi chú "KHÔNG chạy production" | **Chưa sửa — có thể cân nhắc loại khỏi chain apply chuẩn vì bản chất là demo data** |
| 9065_fix_prod_dict_mojibake.sql | `Table 'diab_his_dict_drug_units' doesn't exist` | Cascade: bảng này được tạo trong `0018_seed_master_data.sql`, nhưng 0018 tự lỗi giữa chừng (INSERT sai cột) nên dừng trước khi tạo xong bảng | **Phụ thuộc vào việc sửa 0018 trước** |
| 9067_seed_full_icd10.sql | `Unknown column 'is_active'` | Cột sai trên bảng đích | **Chưa sửa** |

**Đã sửa (an toàn, xác nhận đúng cột/không đổi dữ liệu):**
- `0058_perf_indexes.sql`: chuyển `CREATE INDEX IF NOT EXISTS` (cú pháp MySQL 8 không hỗ trợ)
  sang `CALL add_index_if_missing(...)`. Vẫn còn lỗi (d) cột `doctor_id`/`prescribed_at` không
  tồn tại — đã ghi rõ trong bảng trên, không tự đoán thêm cột.

**KẾT LUẬN TRUNG THỰC:** Việc dựng DB mới từ số 0 **CHƯA đạt 0 lỗi**. 30 file trên cần một trong
hai hướng xử lý mà DevOps không tự quyết định một mình vì rủi ro với dữ liệu production thật:
(1) backend/architect xác nhận tên cột/bảng đúng cho từng migration cột-sai, hoặc (2) tách hẳn
2 nhánh migration (0xxx "legacy patch" và 9xxx "clean-slate rebuild") thành 2 chain độc lập rõ
ràng thay vì gộp chung 1 thứ tự alphabet, vì bản chất 9000_drop_legacy.sql + 9001+ đã redesign
lại toàn bộ schema dưới tên bảng cũ.
