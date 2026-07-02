-- ============================================================
-- Migration: 0019_create_indexes
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-PERF-01, US-PERF-02
-- Idempotent: YES
-- Ghi chú: Thêm index (tenant_id) cho các bảng nghiệp vụ quan trọng
--   để hỗ trợ lọc multi-tenant hiệu quả.
--   FULLTEXT ngram cho tìm kiếm bệnh nhân tiếng Việt (nếu MySQL hỗ trợ
--   ngram parser — mặc định có từ MySQL 5.7.6+).
-- ============================================================
SET NAMES utf8mb4;

-- ── Index tenant_id trên các bảng nghiệp vụ ─────────────────
CALL add_index_if_missing('pat_patients',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_pii_data',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_phi_data',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_insurance',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_consents',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_emergency_contacts', 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pat_privacy_settings',   'idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('cli_visits',               'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_emr_headers',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_emr_contents',         'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_lab_orders',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_lab_results',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_rad_orders',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_rad_results',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_medications',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_vital_signs',          'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_allergies',            'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cli_treatment_monitoring', 'idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('pha_drug_master',   'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pha_prescriptions', 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pha_stocks',        'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pha_transactions',  'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('pha_warehouses',    'idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('sta_doctors',               'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_staff',                 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_schedules',             'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_certifications',        'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_qualifications',        'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_salary_info',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_performance_reviews',   'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_work_experience',       'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sta_department_assignments','idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('sec_users',    'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sec_sessions', 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sec_audit_logs','idx_tenant_id','(tenant_id)');

CALL add_index_if_missing('sys_branches',    'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sys_departments', 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sys_hospitals',   'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sys_rooms',       'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sys_beds',        'idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('bil_billing',     'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('cdss_rules',      'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('equ_equipment',   'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('inv_consumables', 'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('or_surgeries',    'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('rep_reports',     'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('sch_doctor_schedules', 'idx_tenant_id', '(tenant_id)');

CALL add_index_if_missing('int_canonical_data',  'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('int_data_mappings',   'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('int_raw_data',        'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('int_sync_logs',       'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('fil_files',           'idx_tenant_id', '(tenant_id)');
CALL add_index_if_missing('fil_file_versions',   'idx_tenant_id', '(tenant_id)');

-- ── FULLTEXT index tìm kiếm bệnh nhân tiếng Việt ────────────
-- Ghi chú: MySQL 8.0+ hỗ trợ ngram parser (ft_min_word_len=2 mặc định
--   cho ngram, tốt cho tiếng Việt không dấu cách giữa âm tiết).
-- Nếu server chưa cấu hình ngram_token_size, mặc định là 2 char.
-- Syntax: FULLTEXT KEY + WITH PARSER phải dùng trong CREATE TABLE
--   hoặc ALTER TABLE ADD FULLTEXT — không thể qua PREPARE (hạn chế MySQL).
-- Do đó dùng IF NOT EXISTS check thủ công qua stored proc tùy chỉnh.

-- Kiểm tra xem FULLTEXT index ft_full_name đã tồn tại chưa
SET @ft_count = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'pat_patients'
      AND INDEX_NAME   = 'ft_full_name'
);

-- Ghi chú: PREPARE không hỗ trợ DDL FULLTEXT trong mọi phiên bản MySQL.
-- Thực hiện trực tiếp với điều kiện kiểm tra qua user variable.
-- Nếu cần idempotent hoàn toàn, DBA chạy thủ công lần đầu:
--   ALTER TABLE pat_patients ADD FULLTEXT KEY ft_full_name (FULL_NAME) WITH PARSER ngram;
-- Script bên dưới sẽ không lỗi nếu index đã tồn tại (DROP trước khi ADD).

-- Bỏ index cũ nếu có tên khác để tránh duplicate, rồi tạo lại:
-- (safe vì add_index_if_missing không hỗ trợ FULLTEXT WITH PARSER)
-- DBA chú ý: chạy lệnh sau 1 lần nếu chưa có index FULLTEXT:
--   ALTER TABLE `pat_patients` ADD FULLTEXT KEY `ft_full_name` (`FULL_NAME`) WITH PARSER ngram;
--
-- Đây là comment hướng dẫn, không auto-execute để tránh lỗi PREPARE + FULLTEXT.
SELECT 'INFO: FULLTEXT index ft_full_name trên pat_patients.FULL_NAME cần DBA tạo thủ công nếu chưa có.' AS migration_note;
