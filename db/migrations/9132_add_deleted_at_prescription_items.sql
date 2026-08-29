-- ============================================================
-- Migration: 9132_add_deleted_at_prescription_items
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug: BUG-NEW-001 (Blocker) - schema drift bang diab_his_pha_prescription_items.
-- Nguyen nhan goc:
--   Bang cha diab_his_pha_prescriptions co cot soft-delete `deleted_at` (dung
--   convention CLAUDE.md), nhung bang con diab_his_pha_prescription_items chi co
--   `deleted_by`, THIEU `deleted_at`. Code Dapper (PrescriptionHandlers.cs,
--   PortalMeHandlers.cs) loc `WHERE i.deleted_at IS NULL` va soft-delete
--   `UPDATE ... SET deleted_at = NOW()` -> loi "Unknown column 'deleted_at'"
--   tren toan bo luong doc/xoa item don thuoc.
--
-- Xu ly: bo sung cot `deleted_at DATETIME NULL` cho dong bo convention soft-delete
--   voi bang cha. Cot nullable, cac dong hien co = NULL -> deu thoa `deleted_at IS NULL`.
--
-- (Rieng loi cot ghi chu `instructions` -> da sua o code, cot thuc te la `note`,
--  khong can migration DB.)
--
-- Idempotent: YES - dung stored procedure check information_schema
--   (MySQL 8.0.23 khong ho tro ADD COLUMN IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9132;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9132()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'diab_his_pha_prescription_items'
          AND column_name = 'deleted_at'
    ) THEN
        ALTER TABLE `diab_his_pha_prescription_items`
            ADD COLUMN `deleted_at` DATETIME NULL AFTER `created_at`;
    END IF;
END$$
DELIMITER ;

CALL add_col_if_missing_9132();
DROP PROCEDURE IF EXISTS add_col_if_missing_9132;

-- Xac minh (tham khao khi chay tay):
--   SELECT COLUMN_NAME FROM information_schema.columns
--   WHERE table_schema = DATABASE()
--     AND table_name = 'diab_his_pha_prescription_items'
--     AND column_name = 'deleted_at';  -- phai tra ve 1 dong
