-- ============================================================
-- Migration: 9016_perf_indexes
-- Engine: MySQL 8.0+, InnoDB
-- Generated: 2026-05-30
-- Mo ta: Them composite indexes cho cac query path cham (Fix #5, #6, #7)
--        Ap dung cho: diab_his_lab_results, diab_his_sys_tenants,
--        diab_his_sec_audit_logs
-- Idempotent: YES (dung stored proc add_index_if_missing tu 0000_helpers.sql)
-- Luu y: Chay 0000_helpers.sql truoc de co stored proc
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- diab_his_lab_results
-- Query hot: list by tenant + thoi gian, patient lookup, encounter lookup
-- Cot da xac nhan ton tai: tenant_id, created_at, patient_id, encounter_id
-- (them boi 9013_extend_lab_results)
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_lab_results',
    'idx_lab_results_tenant_created',
    '(tenant_id, created_at DESC)'
);

CALL add_index_if_missing(
    'diab_his_lab_results',
    'idx_lab_results_patient',
    '(tenant_id, patient_id)'
);

CALL add_index_if_missing(
    'diab_his_lab_results',
    'idx_lab_results_encounter',
    '(tenant_id, encounter_id)'
);

-- ------------------------------------------------------------
-- diab_his_sys_tenants
-- Query hot: filter by status (admin panel, onboarding)
-- Cot da xac nhan ton tai: status, created_at
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_sys_tenants',
    'idx_tenants_status_created',
    '(status, created_at DESC)'
);

-- ------------------------------------------------------------
-- diab_his_sec_audit_logs
-- Query hot: admin audit trail by tenant+time, by user+action, by resource
-- Cot da xac nhan ton tai: tenant_id, created_at, user_id, action
-- resource_type, resource_id: them boi 0056_audit_log_extensions
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_sec_audit_logs',
    'idx_audit_tenant_time_v2',
    '(tenant_id, created_at DESC)'
);

CALL add_index_if_missing(
    'diab_his_sec_audit_logs',
    'idx_audit_user_action',
    '(tenant_id, user_id, action)'
);

-- resource_type/resource_id: chi tao neu 2 cot nay ton tai
-- (them boi 0056_audit_log_extensions, co the chua apply o moi moi truong)
DROP PROCEDURE IF EXISTS _try_add_audit_resource_idx;
DELIMITER $$
CREATE PROCEDURE _try_add_audit_resource_idx()
BEGIN
    DECLARE v_col_count INT DEFAULT 0;
    SELECT COUNT(*) INTO v_col_count
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'diab_his_sec_audit_logs'
      AND COLUMN_NAME IN ('resource_type', 'resource_id')
    GROUP BY TABLE_NAME
    HAVING COUNT(*) = 2;

    IF v_col_count = 2 THEN
        CALL add_index_if_missing(
            'diab_his_sec_audit_logs',
            'idx_audit_resource',
            '(tenant_id, resource_type, resource_id)'
        );
    END IF;
END$$
DELIMITER ;

CALL _try_add_audit_resource_idx();
DROP PROCEDURE IF EXISTS _try_add_audit_resource_idx;

SET FOREIGN_KEY_CHECKS = 1;
