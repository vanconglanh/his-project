-- ============================================================================
-- 9185_branch_item_visibility_and_drug_prices.sql
-- BO chot 2026-08-30: ao/hien (is_active) + override gia theo chi nhanh cho ca
--   DICH VU (mo rong bang cu) va THUOC (bang moi song song, giu logic dich vu da test).
--
-- 1) Them cot is_active vao diab_his_bil_service_branch_prices:
--      - Khong co dong override cho 1 chi nhanh => mac dinh HIEN theo tenant.
--      - Co dong nhung is_active=0 => AN khoi chi nhanh do (dich vu van ton tai o tenant).
-- 2) Tao bang moi diab_his_pha_drug_branch_prices cung cau truc bang dich vu
--    (BRANCH|GROUP, price, is_active, effective_from/to, note, audit) cho THUOC.
--    drug_id kieu VARCHAR(36): diab_his_pha_drugs.ID co the la INT (legacy) hoac
--    CHAR(36) UUID tuy moi truong -> VARCHAR(36) chua duoc ca hai.
-- 3) Quyen drug.price_override (mirror service.price_override) cho admin/quan_ly_vung.
--
-- Idempotent: add_col_if_missing + CREATE IF NOT EXISTS + INSERT IGNORE/NOT EXISTS.
-- Can 0000_helpers.sql (add_col_if_missing/add_index_if_missing) chay truoc.
--
-- Ghi chu scope: snapshot gia THUOC vao dong don thuoc/hoa don (kieu base_unit_price)
--   HOAN sang dot sau (P2) — bang prescription_items chua co cot snapshot va viec
--   nay khong bat buoc cho tinh nang an/hien + override gia hien tai (xem TASKLIST).
-- ============================================================================
SET NAMES utf8mb4;

-- --- 1) Cot is_active cho bang override gia DICH VU ---------------------------
CALL add_col_if_missing('diab_his_bil_service_branch_prices', 'is_active',
     "TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1=hien, 0=an dich vu khoi chi nhanh/nhom nay'");

-- --- 2) Bang override gia + an/hien THUOC theo chi nhanh/nhom -----------------
CREATE TABLE IF NOT EXISTS `diab_his_pha_drug_branch_prices` (
    `id`             CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT           NOT NULL,
    `drug_id`        VARCHAR(36)   NOT NULL COMMENT 'FK diab_his_pha_drugs.ID (INT legacy hoac CHAR(36) UUID)',
    `scope`          VARCHAR(10)   NOT NULL DEFAULT 'BRANCH' COMMENT 'BRANCH|GROUP',
    `branch_id`      INT           NULL COMMENT 'khi scope=BRANCH',
    `group_id`       INT           NULL COMMENT 'khi scope=GROUP',
    `price`          DECIMAL(15,2) NOT NULL,
    `is_active`      TINYINT(1)    NOT NULL DEFAULT 1 COMMENT '1=hien, 0=an thuoc khoi chi nhanh/nhom nay',
    `effective_from` DATE          NOT NULL,
    `effective_to`   DATE          NULL COMMENT 'NULL = vo thoi han',
    `note`           VARCHAR(300)  NULL,
    `created_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)      NULL,
    `updated_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)      NULL,
    `deleted_at`     DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_drug_price_lookup` (`tenant_id`, `drug_id`, `scope`, `branch_id`, `group_id`, `effective_from`),
    INDEX `idx_drug_price_branch` (`tenant_id`, `branch_id`, `effective_from`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Override gia + an/hien thuoc theo branch/group. Chong chong lap kiem tra o app (PRICE_OVERLAP)';

-- --- 3) Quyen drug.price_override (mirror service.price_override 9165) --------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'drug.price_override', 'drug', 'price_override',
       'Tao/sua/xoa gia override + an/hien thuoc theo chi nhanh/nhom',
       NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'drug.price_override');

DROP PROCEDURE IF EXISTS _grant_drug_price_override;
DELIMITER $$
CREATE PROCEDURE _grant_drug_price_override(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'drug.price_override' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_drug_price_override('admin');
CALL _grant_drug_price_override('quan_ly_vung');
DROP PROCEDURE IF EXISTS _grant_drug_price_override;
