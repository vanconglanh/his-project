-- ============================================================
-- Migration: 9087_bil_counters_rep_cache_branch_unique
-- Muc dich: mo rong UNIQUE KEY cua cac bang co the bi tranh chap giua
--   nhieu chi nhanh trong cung 1 tenant, sau khi da co cot branch_id
--   (them o 9084, backfill o 9085):
--     1) diab_his_bil_counters       : UNIQUE(tenant_id, code)
--                                   -> UNIQUE(tenant_id, branch_id, code)
--        Ly do: diab_his_bil_counters la danh muc "quay thu" (DICH_VU/NHA_THUOC/CLS).
--        Truoc khi tach chi nhanh, quay thu dung chung toan tenant. Voi mo hinh
--        multi-branch, moi chi nhanh co the co bo quay thu rieng ma khong dung
--        chung dai so voi chi nhanh khac -> tranh xung dot ma code trung giua
--        cac chi nhanh dang hoat dong doc lap.
--     2) 5 bang diab_his_rep_*_cache : UNIQUE(tenant_id, period_key)
--                                   -> UNIQUE(tenant_id, branch_id, period_key)
--        Ly do: cache bao cao (doanh thu/KPI bac si/top thuoc/ton kho/cohort dai
--        thao duong) truoc day dung chung 1 dong per (tenant_id, period_key) ->
--        neu khong tach theo chi nhanh, chi nhanh B se doc nham cache duoc tinh
--        cho chi nhanh A (hoac ghi de len nhau qua ON DUPLICATE KEY UPDATE).
--   MySQL coi nhieu dong co cung (tenant_id, code/period_key) nhung branch_id
--   khac nhau la KHONG trung (kem theo dung 1 dong branch_id=NULL cho "cache/quay
--   thu dung chung toan tenant" vi MySQL bo qua NULL trong unique index).
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- add_index_if_missing (0000_helpers.sql) chi tao INDEX thuong, khong phai UNIQUE.
-- Dinh nghia rieng 1 helper UNIQUE cho migration nay.
DROP PROCEDURE IF EXISTS add_unique_index_if_missing;
DELIMITER $$
CREATE PROCEDURE add_unique_index_if_missing(
    IN p_tbl      VARCHAR(64),
    IN p_idx_name VARCHAR(64),
    IN p_col_list TEXT
)
BEGIN
    DECLARE v_count INT DEFAULT 0;
    SELECT COUNT(*) INTO v_count
      FROM information_schema.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl AND INDEX_NAME = p_idx_name;
    IF v_count = 0 THEN
        SET @__ddl = CONCAT('ALTER TABLE `', p_tbl, '` ADD UNIQUE KEY `', p_idx_name, '` ', p_col_list);
        PREPARE __stmt FROM @__ddl; EXECUTE __stmt; DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;

-- 1) diab_his_bil_counters --------------------------------------------------
CALL drop_index_if_exists('diab_his_bil_counters', 'uq_counter_code');
CALL add_unique_index_if_missing('diab_his_bil_counters', 'uq_counter_code_branch',
     '(`tenant_id`, `branch_id`, `code`)');

-- 2) 5 bang report cache -----------------------------------------------------
CALL drop_index_if_exists('diab_his_rep_daily_revenue_cache', 'uq_rev_cache');
CALL add_unique_index_if_missing('diab_his_rep_daily_revenue_cache', 'uq_rev_cache_branch',
     '(`tenant_id`, `branch_id`, `period_key`)');

CALL drop_index_if_exists('diab_his_rep_doctor_kpi_cache', 'uq_kpi_cache');
CALL add_unique_index_if_missing('diab_his_rep_doctor_kpi_cache', 'uq_kpi_cache_branch',
     '(`tenant_id`, `branch_id`, `period_key`)');

CALL drop_index_if_exists('diab_his_rep_top_drugs_cache', 'uq_drugs_cache');
CALL add_unique_index_if_missing('diab_his_rep_top_drugs_cache', 'uq_drugs_cache_branch',
     '(`tenant_id`, `branch_id`, `period_key`)');

CALL drop_index_if_exists('diab_his_rep_inventory_value_cache', 'uq_inv_cache');
CALL add_unique_index_if_missing('diab_his_rep_inventory_value_cache', 'uq_inv_cache_branch',
     '(`tenant_id`, `branch_id`, `period_key`)');

CALL drop_index_if_exists('diab_his_rep_diabetes_cohort_cache', 'uq_cohort_cache');
CALL add_unique_index_if_missing('diab_his_rep_diabetes_cohort_cache', 'uq_cohort_cache_branch',
     '(`tenant_id`, `branch_id`, `period_key`)');

DROP PROCEDURE IF EXISTS add_unique_index_if_missing;
