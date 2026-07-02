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
