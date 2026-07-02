-- ============================================================
-- Migration: 0003_add_audit_columns
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-AUDIT-01
-- Idempotent: YES
-- Ghi chú: Bảng cũ thường đã có CREATED_AT/CREATED_BY/LAST_UPDATED_AT
--   (với LAST_UPDATED_BY và không có deleted_at). Migration này add thêm
--   updated_by (alias chuẩn mới) và deleted_at cho soft-delete.
--   Không đổi tên LAST_UPDATED_BY để tránh breaking existing queries.
-- ============================================================
SET NAMES utf8mb4;

-- ── Nhóm bệnh nhân (pat_*) ──────────────────────────────────
CALL add_col_if_missing('pat_patients',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật (cột chuẩn mới)\'');
CALL add_col_if_missing('pat_patients',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_pii_data',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_pii_data',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_phi_data',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_phi_data',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_insurance',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_insurance',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_consents',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_consents',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_emergency_contacts', 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_emergency_contacts', 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pat_privacy_settings',   'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pat_privacy_settings',   'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');

-- ── Nhóm lâm sàng (cli_*) ───────────────────────────────────
CALL add_col_if_missing('cli_visits',               'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_visits',               'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_emr_headers',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_emr_headers',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_emr_contents',         'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_emr_contents',         'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_lab_orders',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_lab_orders',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_lab_results',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_lab_results',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_rad_orders',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_rad_orders',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_rad_results',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_rad_results',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_medications',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_medications',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_vital_signs',          'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_vital_signs',          'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_allergies',            'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_allergies',            'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('cli_treatment_monitoring', 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cli_treatment_monitoring', 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');

-- ── Nhóm dược (pha_*) ───────────────────────────────────────
CALL add_col_if_missing('pha_drug_master',   'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pha_drug_master',   'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pha_prescriptions', 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pha_prescriptions', 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pha_stocks',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pha_stocks',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pha_transactions',  'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pha_transactions',  'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('pha_warehouses',    'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('pha_warehouses',    'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');

-- ── Nhóm nhân sự (sta_*) ────────────────────────────────────
CALL add_col_if_missing('sta_doctors',               'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_doctors',               'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_staff',                 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_staff',                 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_schedules',             'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_schedules',             'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_certifications',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_certifications',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_qualifications',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_qualifications',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_salary_info',           'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_salary_info',           'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_performance_reviews',   'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_performance_reviews',   'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_work_experience',       'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_work_experience',       'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sta_department_assignments','updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sta_department_assignments','deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');

-- ── Nhóm hệ thống và hóa đơn ────────────────────────────────
CALL add_col_if_missing('sys_branches',    'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sys_branches',    'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sys_departments', 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sys_departments', 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sys_hospitals',   'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sys_hospitals',   'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sys_rooms',       'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sys_rooms',       'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sys_beds',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sys_beds',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('bil_billing',     'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('bil_billing',     'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');

-- ── Các nhóm khác ───────────────────────────────────────────
CALL add_col_if_missing('cdss_rules',       'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('cdss_rules',       'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('equ_equipment',    'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('equ_equipment',    'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('equ_calibration',  'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('equ_calibration',  'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('equ_maintenance',  'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('equ_maintenance',  'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('fil_files',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('fil_files',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('fil_file_versions','updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('fil_file_versions','deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('inv_consumables',  'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('inv_consumables',  'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('or_rooms',         'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('or_rooms',         'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('or_surgeries',     'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('or_surgeries',     'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('rep_reports',      'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('rep_reports',      'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('sch_doctor_schedules', 'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('sch_doctor_schedules', 'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('int_canonical_data',   'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('int_canonical_data',   'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('int_data_mappings',    'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('int_data_mappings',    'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('int_raw_data',         'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('int_raw_data',         'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('int_schema_registry',  'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('int_schema_registry',  'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
CALL add_col_if_missing('int_sync_logs',        'updated_by', 'INT NULL COMMENT \'ID người cập nhật\'');
CALL add_col_if_missing('int_sync_logs',        'deleted_at', 'DATETIME NULL COMMENT \'Thời điểm xóa mềm\'');
