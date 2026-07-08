-- ============================================================
-- Migration: 9033_fix_presc_print_history_types
-- Mo ta: diab_his_pha_prescription_print_history co prescription_id + printed_by
--   kieu INT (legacy) trong khi code ghi GUID chuoi (CHAR(36)) ->
--   INSERT khi in don thuoc bao "Data truncated for column 'prescription_id'"
--   -> GET /prescriptions/{id}/pdf tra 500. Bang rong nen doi kieu an toan.
-- Idempotent: YES (chi doi khi dang INT)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_presc_print_hist_9033;
DELIMITER $$
CREATE PROCEDURE _fix_presc_print_hist_9033()
BEGIN
    DECLARE t_pres VARCHAR(32);
    DECLARE t_by VARCHAR(32);
    SELECT DATA_TYPE INTO t_pres FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pha_prescription_print_history' AND COLUMN_NAME = 'prescription_id';
    SELECT DATA_TYPE INTO t_by FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pha_prescription_print_history' AND COLUMN_NAME = 'printed_by';

    IF t_pres = 'int' THEN
        ALTER TABLE diab_his_pha_prescription_print_history MODIFY COLUMN prescription_id CHAR(36) NOT NULL;
    END IF;
    IF t_by = 'int' THEN
        ALTER TABLE diab_his_pha_prescription_print_history MODIFY COLUMN printed_by CHAR(36) NULL;
    END IF;
END$$
DELIMITER ;
CALL _fix_presc_print_hist_9033();
DROP PROCEDURE IF EXISTS _fix_presc_print_hist_9033;
