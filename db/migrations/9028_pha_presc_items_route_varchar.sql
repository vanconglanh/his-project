-- ============================================================
-- Migration: 9028_pha_presc_items_route_varchar
-- Mo ta: Cot route cua diab_his_pha_prescription_items la ENUM han che
--   -> them dong thuoc voi route ngoai danh sach ENUM gay 500 (STRICT).
--   Doi sang VARCHAR(50) de nhan moi gia tri route (PO, ORAL, IV...).
-- Idempotent: YES (chi doi khi dang la enum)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_route_9028;
DELIMITER $$
CREATE PROCEDURE _fix_route_9028()
BEGIN
    DECLARE t VARCHAR(64);
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pha_prescription_items' AND COLUMN_NAME = 'route';
    IF t = 'enum' THEN
        ALTER TABLE `diab_his_pha_prescription_items`
            MODIFY COLUMN `route` VARCHAR(50) NOT NULL DEFAULT 'ORAL';
    END IF;
END$$
DELIMITER ;
CALL _fix_route_9028();
DROP PROCEDURE IF EXISTS _fix_route_9028;
