-- ============================================================
-- Migration: 9138_cli_lab_orders_add_deleted_by
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-29
-- Idempotent: YES (dung stored procedure check information_schema)
-- ============================================================
-- Bo sung cot deleted_by cho bang diab_his_cli_lab_orders.
-- Ly do: entity EF LabOrder (BaseEntity co DeletedBy) truoc day map nham sang bang
-- chet diab_his_lab_orders. Da remap ve bang live diab_his_cli_lab_orders (LabRadConfiguration)
-- de nhap ket qua XN lookup dung chi dinh. Bang live thieu cot deleted_by -> them de dong bo
-- audit columns, tranh loi "Unknown column 'deleted_by'" khi EF SELECT.
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _9138_add_col;
DELIMITER $$
CREATE PROCEDURE _9138_add_col(
    IN tbl VARCHAR(64),
    IN col VARCHAR(64),
    IN coldef TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', coldef);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$
DELIMITER ;

CALL _9138_add_col('diab_his_cli_lab_orders', 'deleted_by', 'CHAR(36) NULL AFTER deleted_at');

DROP PROCEDURE IF EXISTS _9138_add_col;

-- ------------------------------------------------------------
-- Go bo FK fk_lab_results_order tren diab_his_lab_results.
-- FK nay tro order_id -> diab_his_lab_orders (BANG CHET, khong ai insert chi dinh XN
-- that vao day). Chi dinh XN that nam o diab_his_cli_lab_orders. FK cu khien moi lan
-- nhap ket qua XN (order_id = id chi dinh o bang live) deu vi pham rang buoc -> khong the
-- luu ket qua. Rang buoc tham chieu that su duoc bao dam o application layer
-- (CreateLabResultCommandHandler kiem tra chi dinh ton tai truoc khi tao ket qua).
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS _9138_drop_fk;
DELIMITER $$
CREATE PROCEDURE _9138_drop_fk()
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'diab_his_lab_results'
          AND CONSTRAINT_NAME = 'fk_lab_results_order'
          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
    ) THEN
        ALTER TABLE diab_his_lab_results DROP FOREIGN KEY fk_lab_results_order;
    END IF;
END$$
DELIMITER ;

CALL _9138_drop_fk();

DROP PROCEDURE IF EXISTS _9138_drop_fk;
