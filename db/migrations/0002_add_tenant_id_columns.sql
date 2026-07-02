-- ============================================================
-- Migration: 0002_add_tenant_id_columns
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-TENANT-03, US-TENANT-04
-- Idempotent: YES
-- Ghi chú: Thêm cột tenant_id INT NULL vào ~60 bảng nghiệp vụ cũ.
--   Phase 1: NULL cho phép để không breaking existing data.
--   Phase 2: sẽ backfill rồi ALTER NOT NULL sau.
-- Cảnh báo: Với bảng lớn (int_raw_data, cli_emr_contents) MySQL
--   8.0.23 có thể không dùng ALGORITHM=INSTANT — cần maintenance window.
-- ============================================================
SET NAMES utf8mb4;

-- ── Nhóm bệnh nhân (pat_*) ──────────────────────────────────
CALL add_col_if_missing('pat_patients',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_pii_data',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_phi_data',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_insurance',         'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_consents',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_emergency_contacts','tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pat_privacy_settings',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm lâm sàng (cli_*) ───────────────────────────────────
CALL add_col_if_missing('cli_visits',               'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_emr_headers',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_emr_contents',         'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_lab_orders',           'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_lab_results',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_rad_orders',           'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_rad_results',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_medications',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_vital_signs',          'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_allergies',            'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('cli_treatment_monitoring', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm dược (pha_*) ───────────────────────────────────────
CALL add_col_if_missing('pha_drug_master',    'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pha_prescriptions',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pha_stocks',         'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pha_transactions',   'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('pha_warehouses',     'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm nhân sự (sta_*) ────────────────────────────────────
CALL add_col_if_missing('sta_doctors',               'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_staff',                 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_schedules',             'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_certifications',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_qualifications',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_salary_info',           'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_performance_reviews',   'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_work_experience',       'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sta_department_assignments','tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm bảo mật (sec_*) ────────────────────────────────────
CALL add_col_if_missing('sec_users',           'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_roles',           'tenant_id', 'INT NULL COMMENT \'ID tenant — NULL nghĩa là role hệ thống\'');
CALL add_col_if_missing('sec_permissions',     'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_user_roles',      'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_role_permissions','tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_sessions',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_audit_logs',      'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_data_masks',      'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sec_encryption_keys', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm hệ thống (sys_*) ───────────────────────────────────
CALL add_col_if_missing('sys_branches',    'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sys_departments', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sys_hospitals',   'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sys_rooms',       'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sys_beds',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm hóa đơn (bil_*) ────────────────────────────────────
CALL add_col_if_missing('bil_billing', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Nhóm tích hợp (int_*) ───────────────────────────────────
CALL add_col_if_missing('int_canonical_data',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('int_data_mappings',   'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('int_raw_data',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('int_schema_registry', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('int_sync_logs',       'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');

-- ── Các nhóm khác ───────────────────────────────────────────
CALL add_col_if_missing('cdss_rules',       'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('equ_equipment',    'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('equ_calibration',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('equ_maintenance',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('fil_files',        'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('fil_file_versions','tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('inv_consumables',  'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('or_rooms',         'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('or_surgeries',     'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('rep_reports',      'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
CALL add_col_if_missing('sch_doctor_schedules', 'tenant_id', 'INT NULL COMMENT \'ID tenant sở hữu bản ghi\'');
