-- ============================================================
-- Migration: 9080_helpers_branch
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Muc dich: bo sung helper drop index / drop FK idempotent
--   (0000_helpers.sql chi co add_col_if_missing + add_index_if_missing)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS drop_index_if_exists;
DELIMITER $$
CREATE PROCEDURE drop_index_if_exists(IN p_tbl VARCHAR(64), IN p_idx VARCHAR(64))
BEGIN
    DECLARE v_count INT DEFAULT 0;
    SELECT COUNT(*) INTO v_count
      FROM information_schema.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl AND INDEX_NAME = p_idx;
    IF v_count > 0 THEN
        SET @__ddl = CONCAT('ALTER TABLE `', p_tbl, '` DROP INDEX `', p_idx, '`');
        PREPARE __stmt FROM @__ddl; EXECUTE __stmt; DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS drop_fk_if_exists;
DELIMITER $$
CREATE PROCEDURE drop_fk_if_exists(IN p_tbl VARCHAR(64), IN p_fk VARCHAR(64))
BEGIN
    DECLARE v_count INT DEFAULT 0;
    SELECT COUNT(*) INTO v_count
      FROM information_schema.TABLE_CONSTRAINTS
     WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl
       AND CONSTRAINT_NAME = p_fk AND CONSTRAINT_TYPE = 'FOREIGN KEY';
    IF v_count > 0 THEN
        SET @__ddl = CONCAT('ALTER TABLE `', p_tbl, '` DROP FOREIGN KEY `', p_fk, '`');
        PREPARE __stmt FROM @__ddl; EXECUTE __stmt; DEALLOCATE PREPARE __stmt;
    END IF;
END$$
DELIMITER ;

-- add_col_if_missing chi chay khi BANG ton tai (tranh loi voi bang legacy khong co)
DROP PROCEDURE IF EXISTS add_branch_col;
DELIMITER $$
CREATE PROCEDURE add_branch_col(IN p_tbl VARCHAR(64))
BEGIN
    DECLARE v_tbl INT DEFAULT 0;
    SELECT COUNT(*) INTO v_tbl
      FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl;
    IF v_tbl > 0 THEN
        CALL add_col_if_missing(p_tbl, 'branch_id',
             'INT NULL COMMENT ''FK -> diab_his_sys_branches.id (NULL = du lieu truoc khi tach chi nhanh)''');
        CALL add_index_if_missing(p_tbl, CONCAT('idx_', p_tbl, '_tenant_branch'), '(`tenant_id`, `branch_id`)');
    END IF;
END$$
DELIMITER ;
