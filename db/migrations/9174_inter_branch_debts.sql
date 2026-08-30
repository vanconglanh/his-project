-- ============================================================================
-- 9174_inter_branch_debts.sql
-- Dot 4 da chi nhanh: Cong no noi bo giua cac chi nhanh (BR-84, BR-85, BR-87, US-5.2).
--   - BR-85: BN tra no chi nhanh A tai chi nhanh B -> quy B +tien, cong no BN cua A giam,
--            sinh but toan cong no noi bo: debtor=B (dang giu ho tien cua A -> B no A), creditor=A.
--            source_type = CROSS_BRANCH_PAYMENT.
--   - BR-87: dieu chuyen kho RECEIVED/PARTIALLY_RECEIVED -> debtor=to_branch (nhan hang),
--            creditor=from_branch (gui hang). source_type = STOCK_TRANSFER.
--   - QUYET DINH (BO review, Q3=Khong + Q6=Co): day la but toan doi soat NOI BO 1 phap nhan,
--     KHONG xuat hoa don/chung tu ban hang. BR-86: doanh thu ghi nhan noi cung cap dich vu,
--     KHONG bi anh huong boi bang nay.
--   - Entity 2-chi-nhanh (debtor/creditor) -> KHONG dung IBranchScoped/EF global query filter
--     (giong pattern diab_his_pha_stock_transfers, BR-60): filter scope tuong minh
--     "(debtor_branch_id IN scope OR creditor_branch_id IN scope)" o tang Application.
-- Can chuc: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md muc 5.3 (BR-84..87), US-5.2.
-- Idempotent: CREATE TABLE IF NOT EXISTS + INSERT IGNORE + NOT EXISTS check. Chay lai an toan.
-- ============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_bil_inter_branch_debts` (
    `id`                  CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT           NOT NULL,
    `debtor_branch_id`    INT           NOT NULL COMMENT 'Chi nhanh no (dang giu ho tien/hang cua creditor)',
    `creditor_branch_id`  INT           NOT NULL COMMENT 'Chi nhanh duoc no',
    `amount`              DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `source_type`         VARCHAR(30)   NOT NULL COMMENT 'CROSS_BRANCH_PAYMENT|STOCK_TRANSFER',
    `source_ref_id`       CHAR(36)      NULL COMMENT 'FK toi payment.id hoac stock_transfer.id (khong rang buoc cung cot)',
    `source_ref_code`     VARCHAR(50)   NULL,
    `status`              VARCHAR(20)   NOT NULL DEFAULT 'OPEN' COMMENT 'OPEN|SETTLED',
    `note`                VARCHAR(500)  NULL,
    `settled_at`          DATETIME      NULL,
    `settled_by`          CHAR(36)      NULL,
    `created_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`          CHAR(36)      NULL,
    `updated_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`          CHAR(36)      NULL,
    `deleted_at`          DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_ibd_debtor`   (`tenant_id`, `debtor_branch_id`),
    INDEX `idx_ibd_creditor` (`tenant_id`, `creditor_branch_id`),
    INDEX `idx_ibd_status`   (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Cong no noi bo giua cac chi nhanh (BR-84/85/87) - doi soat cuoi ky, khong xuat hoa don';

-- --- Quyen inter_branch_debt.read --------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'inter_branch_debt', SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'inter_branch_debt.read' AS code,
           'Xem cong no noi bo giua cac chi nhanh (BR-85/BR-87)' AS descr
    UNION ALL
    SELECT 'inter_branch_debt.settle',
           'Danh dau cong no noi bo la da doi soat/tat toan (SETTLED)'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

DROP PROCEDURE IF EXISTS _grant_ibd_perm;
DELIMITER $$
CREATE PROCEDURE _grant_ibd_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_ibd_perm('admin', 'inter_branch_debt.read');
CALL _grant_ibd_perm('admin', 'inter_branch_debt.settle');
CALL _grant_ibd_perm('ke_toan', 'inter_branch_debt.read');
CALL _grant_ibd_perm('quan_ly_vung', 'inter_branch_debt.read');
CALL _grant_ibd_perm('quan_ly_vung', 'inter_branch_debt.settle');
DROP PROCEDURE IF EXISTS _grant_ibd_perm;
