-- ============================================================
-- Migration: 0000_helpers
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: N/A (infrastructure)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- Stored procedure: add_col_if_missing
-- Thêm cột vào bảng nếu cột chưa tồn tại (MySQL 8.0 không
-- hỗ trợ ALTER TABLE ADD COLUMN IF NOT EXISTS)
-- Tham số:
--   p_tbl    : tên bảng (case-sensitive theo OS)
--   p_col    : tên cột cần thêm
--   p_coldef : định nghĩa cột đầy đủ (kiểu + constraints)
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS add_col_if_missing;

DELIMITER $$
CREATE PROCEDURE add_col_if_missing(
    IN p_tbl    VARCHAR(64),
    IN p_col    VARCHAR(64),
    IN p_coldef TEXT
)
BEGIN
    DECLARE v_count INT DEFAULT 0;
    DECLARE v_db   VARCHAR(64);
    DECLARE v_sql  TEXT;

    SET v_db = DATABASE();

    SELECT COUNT(*) INTO v_count
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = v_db
      AND TABLE_NAME   = p_tbl
      AND COLUMN_NAME  = p_col;

    IF v_count = 0 THEN
        SET v_sql = CONCAT('ALTER TABLE `', p_tbl, '` ADD COLUMN `', p_col, '` ', p_coldef);
        SET @__ddl = v_sql;
        PREPARE __stmt FROM @__ddl;
        EXECUTE __stmt;
        DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- Stored procedure: add_index_if_missing
-- Thêm index vào bảng nếu index chưa tồn tại
-- Tham số:
--   p_tbl      : tên bảng
--   p_idx_name : tên index
--   p_col_list : danh sách cột, vd '(tenant_id)' hoặc '(tenant_id, created_at DESC)'
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS add_index_if_missing;

DELIMITER $$
CREATE PROCEDURE add_index_if_missing(
    IN p_tbl      VARCHAR(64),
    IN p_idx_name VARCHAR(64),
    IN p_col_list TEXT
)
BEGIN
    DECLARE v_count INT DEFAULT 0;
    DECLARE v_db   VARCHAR(64);
    DECLARE v_sql  TEXT;

    SET v_db = DATABASE();

    SELECT COUNT(*) INTO v_count
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = v_db
      AND TABLE_NAME   = p_tbl
      AND INDEX_NAME   = p_idx_name;

    IF v_count = 0 THEN
        SET v_sql = CONCAT('ALTER TABLE `', p_tbl, '` ADD INDEX `', p_idx_name, '` ', p_col_list);
        SET @__ddl = v_sql;
        PREPARE __stmt FROM @__ddl;
        EXECUTE __stmt;
        DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;
