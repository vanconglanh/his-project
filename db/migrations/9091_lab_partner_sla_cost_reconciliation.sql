-- ============================================================
-- Migration: 9091_lab_partner_sla_cost_reconciliation
--   (doi so tu 9090 -> 9091 do trung so voi 9090_create_fil_file_annotations.sql
--    khi 2 agent chay song song; noi dung khong phu thuoc thu tu giua 2 file)
-- Muc dich (FR-511/FR-512 P1 - SRS):
--   FR-511: Canh bao ket qua XN qua han SLA cam ket voi doi tac lab.
--     Them cot sla_days tren diab_his_int_lab_partners. Trang thai
--     overdue duoc TINH TOAN trong query (order_date + sla_days < now
--     va status chua co ket qua) - khong luu cot rieng de tranh drift.
--   FR-512: Doi soat cong no/hoa hong voi doi tac XN. Them bang
--     diab_his_int_lab_partner_costs (chi phi tung LabOrder) va
--     diab_his_int_lab_partner_reconciliations (ky doi soat theo thang).
--
--   Ghi chu quan trong: bang LabOrder dang duoc dung thuc te trong
--   ClsHandlers.cs la `diab_his_cli_lab_orders` (KHONG PHAI
--   `diab_his_lab_orders` ma EF LabOrderConfiguration tro toi - bang do
--   hien khong co code nao ghi du lieu). Migration 9084 chi them
--   branch_id cho `diab_his_lab_orders` (bang khong dung) nen
--   `diab_his_cli_lab_orders` con thieu branch_id. Migration nay bo
--   sung branch_id cho dung bang dang chay that, phuc vu filter chi
--   nhanh cho tinh nang canh bao overdue / doi soat.
--
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- 1) LabPartner: SLA cam ket (so ngay) + gia von mac dinh (fallback khi
--    khong nhap chi phi cu the tung LabOrder)
CALL add_col_if_missing('diab_his_int_lab_partners', 'sla_days',
     'INT NOT NULL DEFAULT 3 COMMENT ''So ngay cam ket tra ket qua XN (SLA)''');
CALL add_col_if_missing('diab_his_int_lab_partners', 'default_cost_amount',
     'DECIMAL(12,2) NULL COMMENT ''Gia von mac dinh (VND) phong kham tra doi tac / 1 chi dinh, dung khi khong nhap chi phi rieng''');

-- 2) Bo sung branch_id cho bang LabOrder dang duoc dung thuc te
--    (fix gap tu 9084 - xem ghi chu tren dau file)
CALL add_branch_col('diab_his_cli_lab_orders');

-- Cot danh dau da gui canh bao overdue (chong spam thong bao trung lap
-- moi lan job chay - pattern giong cli_visits.alert_sent_at cua
-- EncounterOver12hAlertJob)
CALL add_col_if_missing('diab_his_cli_lab_orders', 'overdue_alert_sent_at',
     'DATETIME NULL COMMENT ''Thoi diem da gui canh bao qua han SLA (FR-511) - reset ve NULL khi doi trang thai/doi tac''');

-- 3) Chi phi / hoa hong tung LabOrder tra doi tac (FR-512)
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_partner_costs` (
    `id`                 CHAR(36)      NOT NULL,
    `tenant_id`          INT           NOT NULL,
    `branch_id`          INT           NULL COMMENT 'FK -> diab_his_sys_branches.id (ke thua tu LabOrder)',
    `lab_partner_id`     CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_int_lab_partners.id',
    `lab_order_id`       CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_cli_lab_orders.id',
    `test_code`          VARCHAR(50)   NOT NULL,
    `cost_amount`        DECIMAL(12,2) NOT NULL COMMENT 'Gia von / hoa hong phong kham tra doi tac (khac gia thu benh nhan)',
    `currency`           VARCHAR(10)   NOT NULL DEFAULT 'VND',
    `incurred_at`        DATETIME      NOT NULL COMMENT 'Thoi diem phat sinh chi phi (= ordered_at cua LabOrder)',
    `period_month`       CHAR(7)       NOT NULL COMMENT 'Ky doi soat, dang YYYY-MM, suy tu incurred_at',
    `reconciliation_id`  CHAR(36)      NULL COMMENT 'FK -> diab_his_int_lab_partner_reconciliations.id, NULL = chua gan vao ky doi soat nao',
    `note`                VARCHAR(500)  NULL,
    `created_at`         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`         CHAR(36)      NULL,
    `updated_at`         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`         CHAR(36)      NULL,
    `deleted_at`         DATETIME      NULL,
    `deleted_by`         CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_lab_partner_cost_order` (`lab_order_id`),
    INDEX `idx_lpc_tenant_partner_period` (`tenant_id`, `lab_partner_id`, `period_month`),
    INDEX `idx_lpc_reconciliation` (`reconciliation_id`),
    INDEX `idx_lpc_tenant_branch` (`tenant_id`, `branch_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi phi/hoa hong phong kham tra doi tac XN cho tung LabOrder (FR-512)';

-- 4) Ky doi soat cong no theo thang / doi tac (FR-512)
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_partner_reconciliations` (
    `id`               CHAR(36)      NOT NULL,
    `tenant_id`        INT           NOT NULL,
    `lab_partner_id`   CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_int_lab_partners.id',
    `period_month`     CHAR(7)       NOT NULL COMMENT 'Ky doi soat, dang YYYY-MM',
    `total_orders`     INT           NOT NULL DEFAULT 0,
    `total_cost`       DECIMAL(14,2) NOT NULL DEFAULT 0,
    `currency`         VARCHAR(10)   NOT NULL DEFAULT 'VND',
    `status`           VARCHAR(20)   NOT NULL DEFAULT 'draft' COMMENT 'draft | confirmed | paid',
    `confirmed_at`     DATETIME      NULL,
    `confirmed_by`     CHAR(36)      NULL,
    `paid_at`          DATETIME      NULL,
    `paid_by`          CHAR(36)      NULL,
    `note`             VARCHAR(500)  NULL,
    `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)      NULL,
    `updated_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)      NULL,
    `deleted_at`       DATETIME      NULL,
    `deleted_by`       CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_lab_partner_reconciliation_period` (`tenant_id`, `lab_partner_id`, `period_month`),
    INDEX `idx_lpr_tenant_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ky doi soat cong no/hoa hong voi doi tac XN theo thang (FR-512)';

-- 5) Permissions moi (theo pattern diab_his_sec_* - xem 9086)
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'lab_partner.finance_read'  AS code, 'Xem chi phi/doi soat cong no doi tac lab' AS descr UNION ALL
    SELECT 'lab_partner.finance_write','Tao/sua chi phi, tao va xac nhan/thanh toan ky doi soat doi tac lab'
) AS t;

DROP PROCEDURE IF EXISTS _grant_lab_partner_finance_perm;
DELIMITER $$
CREATE PROCEDURE _grant_lab_partner_finance_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

CALL _grant_lab_partner_finance_perm('admin',          'lab_partner.finance_read');
CALL _grant_lab_partner_finance_perm('admin',          'lab_partner.finance_write');
-- Ke toan chu tri doi soat cong no voi doi tac
CALL _grant_lab_partner_finance_perm('ke_toan',        'lab_partner.finance_read');
CALL _grant_lab_partner_finance_perm('ke_toan',        'lab_partner.finance_write');
-- KTV truong / duoc si xem duoc chi phi de doi chieu, khong duoc sua/xac nhan
CALL _grant_lab_partner_finance_perm('ky_thuat_vien',  'lab_partner.finance_read');

DROP PROCEDURE IF EXISTS _grant_lab_partner_finance_perm;
