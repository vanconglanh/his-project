-- ============================================================
-- Migration: 9210_create_cls_order_templates
-- Nguon: Lark Bug-Feedback (BM-12) - "Role BS - Man hinh Kham benh - tab CLS -
-- Thieu tinh nang luu bo chi dinh mau".
-- PO xac nhan (2026-09-10): lam ngay, uu tien cao. Bo chi dinh mau luu RIENG
-- TUNG BAC SI (khong dung chung giua cac bac si trong tenant).
-- Bang moi diab_his_cls_order_templates: moi dong la 1 bo mau, cot doctor_id
-- = ICurrentUser.UserId cua nguoi tao (lay tu JWT, khong trust input client),
-- items_json luu nguyen mang cart FE (ClsCatalogItem[] + is_free) dang JSON.
-- Idempotent: YES (add_col_if_missing / CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cls_order_templates (
    id CHAR(36) NOT NULL PRIMARY KEY,
    tenant_id INT NOT NULL,
    doctor_id CHAR(36) NOT NULL,
    name VARCHAR(200) NOT NULL,
    items_json LONGTEXT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by CHAR(36) NULL,
    updated_at DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    updated_by CHAR(36) NULL,
    deleted_at DATETIME NULL,
    INDEX idx_cls_order_templates_doctor (tenant_id, doctor_id, deleted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Permission catalog + grant cho role bac_si (nguoi tao/dung bo mau) va admin.
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code)+1), t.code, NOW()
FROM (
    SELECT 'cls_order_template.create' AS code
    UNION ALL SELECT 'cls_order_template.read'
    UNION ALL SELECT 'cls_order_template.delete'
) AS t;

DROP PROCEDURE IF EXISTS _grant_perm_9210;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9210(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

CALL _grant_perm_9210('bac_si', 'cls_order_template.create');
CALL _grant_perm_9210('bac_si', 'cls_order_template.read');
CALL _grant_perm_9210('bac_si', 'cls_order_template.delete');
CALL _grant_perm_9210('admin', 'cls_order_template.create');
CALL _grant_perm_9210('admin', 'cls_order_template.read');
CALL _grant_perm_9210('admin', 'cls_order_template.delete');

DROP PROCEDURE IF EXISTS _grant_perm_9210;

-- Rollback:
--   DROP TABLE IF EXISTS diab_his_cls_order_templates;
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE p.code LIKE 'cls_order_template.%';
--   DELETE FROM diab_his_sec_permissions WHERE code LIKE 'cls_order_template.%';
