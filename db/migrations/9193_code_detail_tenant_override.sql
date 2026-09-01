-- ============================================================
-- Migration: 9193_code_detail_tenant_override
-- Muc dich: Viec 1 (audit-hardcode-vs-master-data) - cho phep tung tenant
--   TAO/GHI DE/AN ma trong diab_his_sys_code_detail thay vi dung chung
--   danh muc global cung 27 nhom seed san (9034/9035).
--   - tenant_id NULL  = ma CHUAN he thong (dung chung moi tenant)
--   - tenant_id = X   = ma RIENG cua tenant X (override hoac tu tao moi)
--   - is_hidden = 1   = tenant X an mã chuan (khong doi global, chi an rieng)
--   - is_system = 1   = mã he thong, khong cho phep XOA (chi an/override)
-- Idempotent: YES (add_col_if_missing / drop_index_if_exists nhu 9095)
-- ============================================================
SET NAMES utf8mb4;

-- ---------------------------------------------------------------
-- 1) diab_his_sys_code_detail: them tenant_id + tenant_scope + is_hidden + is_system
-- ---------------------------------------------------------------
CALL add_col_if_missing('diab_his_sys_code_detail', 'tenant_id',
    "INT NULL COMMENT 'NULL = ma chuan he thong; co gia tri = ma rieng/override cua tenant'");

CALL add_col_if_missing('diab_his_sys_code_detail', 'tenant_scope',
    "INT AS (COALESCE(`tenant_id`, 0)) STORED COMMENT 'Cot sinh: 0 the cho tenant_id NULL (global), chi dung lam khoa UNIQUE'");

CALL add_col_if_missing('diab_his_sys_code_detail', 'is_hidden',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Tenant an ma chuan (chi anh huong rieng tenant nay)'");

CALL add_col_if_missing('diab_his_sys_code_detail', 'is_system',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Ma he thong (seed san) - khong cho phep xoa, chi an/override'");

-- Doi UNIQUE (code_master_id, code) -> (tenant_scope, code_master_id, code) de
-- cho phep tenant override cung 1 code voi ban global (khac tenant_scope).
CALL drop_index_if_exists('diab_his_sys_code_detail', 'uq_code_detail_old');

SET @idx_exists_detail := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_sys_code_detail'
      AND INDEX_NAME = 'uq_code_detail' AND COLUMN_NAME = 'code_master_id'
);
SET @sql_detail := IF(@idx_exists_detail > 0,
    'ALTER TABLE `diab_his_sys_code_detail` RENAME INDEX `uq_code_detail` TO `uq_code_detail_old`, ADD UNIQUE KEY `uq_code_detail` (`tenant_scope`, `code_master_id`, `code`)',
    'SELECT 1');
PREPARE stmt_detail FROM @sql_detail;
EXECUTE stmt_detail;
DEALLOCATE PREPARE stmt_detail;
CALL drop_index_if_exists('diab_his_sys_code_detail', 'uq_code_detail_old');

CALL add_index_if_missing('diab_his_sys_code_detail', 'idx_code_detail_tenant', '(tenant_id, code_master_id)');

-- Danh dau toan bo ma seed san (27 nhom, migration 9034/9035) la mã he thong.
UPDATE `diab_his_sys_code_detail` SET `is_system` = 1 WHERE `tenant_id` IS NULL AND `is_system` = 0;

-- ---------------------------------------------------------------
-- 2) diab_his_sys_code_master: them tenant_id + is_system (khong doi PK)
-- ---------------------------------------------------------------
CALL add_col_if_missing('diab_his_sys_code_master', 'tenant_id',
    "INT NULL COMMENT 'NULL = nhom ma chuan he thong; co gia tri = nhom tu tao rieng cua tenant'");

CALL add_col_if_missing('diab_his_sys_code_master', 'tenant_scope',
    "INT AS (COALESCE(`tenant_id`, 0)) STORED COMMENT 'Cot sinh - chua dung lam khoa UNIQUE (id van la PK duy nhat)'");

CALL add_col_if_missing('diab_his_sys_code_master', 'is_system',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Nhom ma he thong (seed san 27 nhom, 9034/9035)'");

UPDATE `diab_his_sys_code_master` SET `is_system` = 1 WHERE `tenant_id` IS NULL AND `is_system` = 0;

-- ---------------------------------------------------------------
-- 3) Permission code.read / code.manage + grant admin (pattern 9172)
-- ---------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'code.read', 'code', 'read', 'Xem danh muc ma dung chung (code master/detail)', NOW()
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = 'code.read');

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'code.manage', 'code', 'manage', 'Quan ly (tao/sua/an/xoa) danh muc ma rieng cua tenant', NOW()
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = 'code.manage');

DROP PROCEDURE IF EXISTS _grant_code_perm_9193;
DELIMITER $$
CREATE PROCEDURE _grant_code_perm_9193(IN p_role VARCHAR(50), IN p_perm VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36); DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id=v_role_id AND permission_id=v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_code_perm_9193('admin', 'code.read');
CALL _grant_code_perm_9193('admin', 'code.manage');
DROP PROCEDURE IF EXISTS _grant_code_perm_9193;
