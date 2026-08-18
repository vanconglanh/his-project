-- ============================================================
-- Migration: 9081_create_cls_order_rounds
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-18
-- Story refs: G01 + G02 - Chuoi CLS theo dot chi dinh + gate thanh toan
-- Mo ta: bang dot chi dinh CLS (don vi thu tien + gate thuc hien);
--   lab/rad order gan round_id (NULL = don le legacy, bo qua gate);
--   tenant flag cho_phep_no_vien_phi; cot ho tro TicketStatus WAITING_CLS.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + add_col_if_missing / add_index_if_missing)
-- Prereq: 0000_helpers.sql
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- 0) Helper cuc bo: chi ALTER khi bang ton tai (repo co ca cap bang
--    diab_his_cli_lab_orders (0031) lan diab_his_lab_orders (9004))
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS _9081_add_col_if_table;
DELIMITER $$
CREATE PROCEDURE _9081_add_col_if_table(IN p_tbl VARCHAR(64), IN p_col VARCHAR(64), IN p_def TEXT)
BEGIN
    DECLARE v_t INT DEFAULT 0;
    SELECT COUNT(*) INTO v_t FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl AND TABLE_TYPE = 'BASE TABLE';
    IF v_t > 0 THEN CALL add_col_if_missing(p_tbl, p_col, p_def); END IF;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS _9081_add_idx_if_table;
DELIMITER $$
CREATE PROCEDURE _9081_add_idx_if_table(IN p_tbl VARCHAR(64), IN p_idx VARCHAR(64), IN p_cols TEXT)
BEGIN
    DECLARE v_t INT DEFAULT 0;
    SELECT COUNT(*) INTO v_t FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tbl AND TABLE_TYPE = 'BASE TABLE';
    IF v_t > 0 THEN CALL add_index_if_missing(p_tbl, p_idx, p_cols); END IF;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- 1) Bang dot chi dinh CLS
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_cls_order_rounds` (
    `id`             CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT           NOT NULL,
    `encounter_id`   CHAR(36)      NOT NULL      COMMENT 'FK -> diab_his_enc_encounters.id',
    `round_no`       INT           NOT NULL      COMMENT 'So thu tu dot trong luot kham, bat dau 1',
    `status`         VARCHAR(20)   NOT NULL DEFAULT 'OPEN'
                     COMMENT 'OPEN|SUBMITTED|IN_PROGRESS|COMPLETED|CANCELLED',
    `payment_status` VARCHAR(20)   NOT NULL DEFAULT 'UNPAID'
                     COMMENT 'UNPAID|PAID|WAIVED',
    `total_amount`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `billing_id`     CHAR(36)      NULL          COMMENT 'FK -> diab_his_bil_billing.id',
    `paid_at`        DATETIME      NULL,
    `paid_by`        CHAR(36)      NULL,
    `waived_reason`  VARCHAR(500)  NULL          COMMENT 'Ly do mien/no vien phi (payment_status=WAIVED)',
    `cancel_reason`  VARCHAR(500)  NULL,
    `note`           TEXT          NULL,
    `created_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)      NULL,
    `updated_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)      NULL,
    `deleted_at`     DATETIME      NULL,
    `deleted_by`     CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_clsround_enc_no` (`tenant_id`, `encounter_id`, `round_no`),
    INDEX `idx_clsround_enc`  (`tenant_id`, `encounter_id`),
    INDEX `idx_clsround_pay`  (`tenant_id`, `payment_status`, `status`),
    INDEX `idx_clsround_bill` (`tenant_id`, `billing_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Dot chi dinh CLS - don vi thanh toan va gate thuc hien';

-- ------------------------------------------------------------
-- 2) round_id tren lab/rad orders (NULL = legacy, bo qua gate)
--    Ap dung cho ca 2 cap bang dang ton tai trong repo.
-- ------------------------------------------------------------
CALL _9081_add_col_if_table('diab_his_lab_orders',     'round_id', 'CHAR(36) NULL COMMENT ''FK -> diab_his_cls_order_rounds.id; NULL = don le legacy''');
CALL _9081_add_col_if_table('diab_his_rad_orders',     'round_id', 'CHAR(36) NULL COMMENT ''FK -> diab_his_cls_order_rounds.id; NULL = don le legacy''');
CALL _9081_add_col_if_table('diab_his_cli_lab_orders', 'round_id', 'CHAR(36) NULL COMMENT ''FK -> diab_his_cls_order_rounds.id; NULL = don le legacy''');
CALL _9081_add_col_if_table('diab_his_cli_rad_orders', 'round_id', 'CHAR(36) NULL COMMENT ''FK -> diab_his_cls_order_rounds.id; NULL = don le legacy''');

CALL _9081_add_idx_if_table('diab_his_lab_orders',     'idx_laborder_round', '(`tenant_id`, `round_id`)');
CALL _9081_add_idx_if_table('diab_his_rad_orders',     'idx_radorder_round', '(`tenant_id`, `round_id`)');
CALL _9081_add_idx_if_table('diab_his_cli_lab_orders', 'idx_laborder_round', '(`tenant_id`, `round_id`)');
CALL _9081_add_idx_if_table('diab_his_cli_rad_orders', 'idx_radorder_round', '(`tenant_id`, `round_id`)');

-- ------------------------------------------------------------
-- 3) Tenant flag: cho phep no vien phi
-- ------------------------------------------------------------
CALL _9081_add_col_if_table('diab_his_sys_tenants', 'cho_phep_no_vien_phi',
    'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''1 = cho phep thuc hien CLS khi dot chua thanh toan (ghi audit log)''');

-- ------------------------------------------------------------
-- 4) Ho tro TicketStatus WAITING_CLS (cot status dang VARCHAR nen khong can ALTER type)
-- ------------------------------------------------------------
CALL _9081_add_col_if_table('diab_his_rcp_queue_tickets', 'released_room_id',
    'CHAR(36) NULL COMMENT ''Phong da nha khi chuyen WAITING_CLS; dung de quay lai IN_PROGRESS''');
CALL _9081_add_col_if_table('diab_his_rcp_queue_tickets', 'waiting_cls_at',
    'DATETIME NULL COMMENT ''Thoi diem chuyen sang cho ket qua CLS''');
CALL _9081_add_idx_if_table('diab_his_rcp_queue_tickets', 'idx_ticket_status_date',
    '(`tenant_id`, `ticket_date`, `status`)');

DROP PROCEDURE IF EXISTS _9081_add_col_if_table;
DROP PROCEDURE IF EXISTS _9081_add_idx_if_table;

-- ============================================================
-- Rollback:
--   DROP TABLE IF EXISTS `diab_his_cls_order_rounds`;
--   ALTER TABLE `diab_his_lab_orders`  DROP COLUMN `round_id`;
--   ALTER TABLE `diab_his_rad_orders`  DROP COLUMN `round_id`;
--   ALTER TABLE `diab_his_cli_lab_orders` DROP COLUMN `round_id`;
--   ALTER TABLE `diab_his_cli_rad_orders` DROP COLUMN `round_id`;
--   ALTER TABLE `diab_his_sys_tenants` DROP COLUMN `cho_phep_no_vien_phi`;
--   ALTER TABLE `diab_his_rcp_queue_tickets` DROP COLUMN `released_room_id`, DROP COLUMN `waiting_cls_at`;
-- ============================================================
