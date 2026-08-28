-- ============================================================
-- Migration: 9085_backfill_branch_id
-- Muc dich: gan toan bo du lieu lich su ve branch mac dinh cua tenant.
-- Idempotent: YES (chi update dong branch_id IS NULL)
-- CANH BAO: chay ngoai gio cao diem. Bang lon (enc_encounters, bil_billing,
--   pha_stock_movements) nen chay theo lo qua script backfill rieng neu > 1 trieu dong.
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS backfill_branch;
DELIMITER $$
CREATE PROCEDURE backfill_branch(IN p_tbl VARCHAR(64))
BEGIN
    DECLARE v_cnt INT DEFAULT 0;
    SELECT COUNT(*) INTO v_cnt
      FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl AND COLUMN_NAME = 'branch_id';
    IF v_cnt > 0 THEN
        SET @__ddl = CONCAT(
            'UPDATE `', p_tbl, '` x ',
            'JOIN `diab_his_sys_branches` b ON b.`tenant_id` = x.`tenant_id` ',
            '  AND b.`is_default` = 1 AND b.`deleted_at` IS NULL ',
            'SET x.`branch_id` = b.`id` WHERE x.`branch_id` IS NULL');
        PREPARE __stmt FROM @__ddl; EXECUTE __stmt; DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;

CALL backfill_branch('diab_his_sec_users');
CALL backfill_branch('diab_his_sec_audit_logs');
CALL backfill_branch('diab_his_sys_rooms');          -- da co san cot tu 9006
CALL backfill_branch('diab_his_sch_appointments');
CALL backfill_branch('diab_his_sch_doctor_schedules');
CALL backfill_branch('diab_his_sch_schedule_blocks');
CALL backfill_branch('diab_his_rcp_queue_tickets');
CALL backfill_branch('diab_his_enc_encounters');
CALL backfill_branch('diab_his_lab_orders');
CALL backfill_branch('diab_his_rad_orders');
CALL backfill_branch('diab_his_lab_results');
CALL backfill_branch('diab_his_rad_results');
CALL backfill_branch('diab_his_cls_uploads');
CALL backfill_branch('diab_his_fil_cls_uploads');
CALL backfill_branch('diab_his_pha_prescriptions');
CALL backfill_branch('diab_his_pha_dispenses');
CALL backfill_branch('diab_his_pha_dispense_records');
CALL backfill_branch('diab_his_pha_stock');
CALL backfill_branch('diab_his_pha_stock_movements');
CALL backfill_branch('diab_his_pha_purchase_orders');
CALL backfill_branch('diab_his_pha_grn');
CALL backfill_branch('diab_his_pha_stocktakes');
CALL backfill_branch('pha_warehouses');
CALL backfill_branch('diab_his_bil_billing');
CALL backfill_branch('diab_his_bil_payments');
CALL backfill_branch('diab_his_bil_einvoices');
CALL backfill_branch('diab_his_bil_cashier_shifts');
CALL backfill_branch('diab_his_bil_counters');
CALL backfill_branch('diab_his_bil_cash_out');
CALL backfill_branch('diab_his_int_bhyt_exports');
CALL backfill_branch('diab_his_int_bhyt_reconcile_uploads');
CALL backfill_branch('diab_his_int_dtqg_credentials');
CALL backfill_branch('diab_his_int_dtqg_submissions');
CALL backfill_branch('diab_his_cli_followup_recall');
CALL backfill_branch('diab_his_rep_daily_revenue_cache');
CALL backfill_branch('diab_his_rep_doctor_kpi_cache');
CALL backfill_branch('diab_his_rep_top_drugs_cache');
CALL backfill_branch('diab_his_rep_inventory_value_cache');
CALL backfill_branch('diab_his_rep_diabetes_cohort_cache');

DROP PROCEDURE IF EXISTS backfill_branch;

-- Dong bo sec_users.branch_id voi user_branches.is_primary
UPDATE `diab_his_sec_users` u
  JOIN `diab_his_sec_user_branches` ub
    ON ub.`user_id` = u.`id` AND ub.`is_primary` = 1 AND ub.`deleted_at` IS NULL
   SET u.`branch_id` = ub.`branch_id`
 WHERE u.`branch_id` IS NULL;

-- Query kiem chung sau backfill (chay tay, phai tra ve 0 dong):
-- SELECT 'enc_encounters' t, COUNT(*) c FROM diab_his_enc_encounters WHERE branch_id IS NULL
-- UNION ALL SELECT 'bil_billing', COUNT(*) FROM diab_his_bil_billing WHERE branch_id IS NULL
-- UNION ALL SELECT 'pha_prescriptions', COUNT(*) FROM diab_his_pha_prescriptions WHERE branch_id IS NULL
-- UNION ALL SELECT 'sec_users', COUNT(*) FROM diab_his_sec_users WHERE branch_id IS NULL AND deleted_at IS NULL;
