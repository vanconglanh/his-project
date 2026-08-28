-- ============================================================
-- Migration: 9092_create_pkg_tables
-- Muc dich (FR-1201..FR-1206): Goi dich vu & theo doi dinh muc.
--   6 bang moi prefix diab_his_pkg_* (D1/D2 - KHONG dung lai
--   diab_his_bil_service_packages vi ban chat khac nhau, xem
--   docs/erd/goi-dich-vu-dinh-muc.md muc 0/1).
-- Engine: MySQL 8.0+, InnoDB, utf8mb4_0900_ai_ci
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

-- ----------------------------------------------------------------
-- 1) diab_his_pkg_service_packages - Template goi (FR-1201)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_service_packages` (
    `id`                    CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT           NOT NULL,
    `code`                  VARCHAR(50)   NOT NULL,
    `name`                  VARCHAR(255)  NOT NULL,
    `description`           TEXT          NULL,
    `duration_days`         INT           NOT NULL DEFAULT 365,
    `list_price`            DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `vat_rate`              TINYINT       NOT NULL DEFAULT 0,
    `min_deposit_percent`   DECIMAL(5,2)  NULL,
    `is_active`             TINYINT(1)    NOT NULL DEFAULT 1,
    `valid_from`            DATE          NULL,
    `valid_to`              DATE          NULL,
    `created_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)      NULL,
    `updated_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)      NULL,
    `deleted_at`            DATETIME      NULL,
    `deleted_by`            CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pkg_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_pkg_tenant_active` (`tenant_id`, `is_active`, `deleted_at`),
    CONSTRAINT `chk_pkg_duration_positive` CHECK (`duration_days` > 0),
    CONSTRAINT `chk_pkg_deposit_range` CHECK (`min_deposit_percent` IS NULL OR (`min_deposit_percent` BETWEEN 0 AND 100))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Template goi dinh muc tra truoc (FR-1201) - khac ban voi bil_service_packages (combo giam gia)';

-- ----------------------------------------------------------------
-- 2) diab_his_pkg_entitlement_definitions - Dong dinh muc cua template
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_entitlement_definitions` (
    `id`            CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`     INT           NOT NULL,
    `package_id`    CHAR(36)      NOT NULL,
    `item_type`     ENUM('VISIT','SERVICE','DRUG') NOT NULL,
    `item_ref_id`   CHAR(36)      NOT NULL,
    `item_code`     VARCHAR(50)   NOT NULL,
    `item_name`     VARCHAR(255)  NOT NULL,
    `unit`          VARCHAR(30)   NOT NULL DEFAULT 'lần',
    `quantity`      DECIMAL(12,3) NOT NULL DEFAULT 1.000,
    `sort_order`    INT           NOT NULL DEFAULT 0,
    `created_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`    CHAR(36)      NULL,
    `updated_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`    CHAR(36)      NULL,
    `deleted_at`    DATETIME      NULL,
    `deleted_by`    CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_ped_pkg_item` (`package_id`, `item_type`, `item_ref_id`),
    INDEX `idx_ped_package` (`package_id`, `sort_order`),
    INDEX `idx_ped_tenant_ref` (`tenant_id`, `item_type`, `item_ref_id`),
    CONSTRAINT `fk_ped_package` FOREIGN KEY (`package_id`) REFERENCES `diab_his_pkg_service_packages` (`id`) ON DELETE CASCADE,
    CONSTRAINT `chk_ped_qty_positive` CHECK (`quantity` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Dong dinh muc cua template goi (FR-1201) - item_type gioi han VISIT|SERVICE|DRUG (cam kieu gia tri VND)';

-- ----------------------------------------------------------------
-- 3) diab_his_pkg_subscriptions - Benh nhan so huu goi (FR-1202)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_subscriptions` (
    `id`                        CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT           NOT NULL,
    `branch_id`                 INT           NULL,
    `patient_id`                CHAR(36)      NOT NULL,
    `package_id`                CHAR(36)      NOT NULL,
    `subscription_no`           VARCHAR(30)   NOT NULL,
    `package_code_snapshot`     VARCHAR(50)   NOT NULL,
    `package_name_snapshot`     VARCHAR(255)  NOT NULL,
    `purchase_date`             DATE          NOT NULL,
    `effective_date`            DATE          NOT NULL,
    `expiry_date`               DATE          NOT NULL,
    `duration_days_snapshot`    INT           NOT NULL,
    `total_price`               DECIMAL(15,2) NOT NULL,
    `amount_paid`               DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `amount_due`                DECIMAL(15,2) GENERATED ALWAYS AS (`total_price` - `amount_paid`) STORED,
    `payment_status`            ENUM('unpaid','deposit_paid','paid_full','refunded') NOT NULL DEFAULT 'unpaid',
    `status`                    ENUM('pending_payment','active','suspended','expired','exhausted','cancelled') NOT NULL DEFAULT 'pending_payment',
    `activated_at`              DATETIME(3)   NULL,
    `suspended_at`              DATETIME(3)   NULL,
    `suspend_reason`            VARCHAR(255)  NULL,
    `cancelled_at`              DATETIME(3)   NULL,
    `cancel_reason`             VARCHAR(255)  NULL,
    `refunded_amount`           DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `expiry_reminded_at`        DATETIME(3)   NULL,
    `overdue_alerted_at`        DATETIME(3)   NULL,
    `note`                      TEXT          NULL,
    `created_at`                DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)      NULL,
    `updated_at`                DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`                CHAR(36)      NULL,
    `deleted_at`                DATETIME      NULL,
    `deleted_by`                CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_sub_tenant_no` (`tenant_id`, `subscription_no`),
    INDEX `idx_sub_patient_active` (`tenant_id`, `patient_id`, `status`, `expiry_date`),
    INDEX `idx_sub_tenant_branch` (`tenant_id`, `branch_id`),
    INDEX `idx_sub_expiry` (`tenant_id`, `status`, `expiry_date`),
    INDEX `idx_sub_debt` (`tenant_id`, `payment_status`, `amount_due`),
    INDEX `idx_sub_package` (`tenant_id`, `package_id`),
    CONSTRAINT `fk_sub_package` FOREIGN KEY (`package_id`) REFERENCES `diab_his_pkg_service_packages` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Benh nhan mua goi dinh muc (FR-1202) - dung xuyen chi nhanh (branch_id chi de danh dau noi BAN)';

-- ----------------------------------------------------------------
-- 4) diab_his_pkg_entitlement_balances - So du dinh muc (FR-1202/1204/1205)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_entitlement_balances` (
    `id`                    CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT           NOT NULL,
    `subscription_id`       CHAR(36)      NOT NULL,
    `definition_id`         CHAR(36)      NULL,
    `item_type`             ENUM('VISIT','SERVICE','DRUG') NOT NULL,
    `item_ref_id`           CHAR(36)      NOT NULL,
    `item_code`             VARCHAR(50)   NOT NULL,
    `item_name`             VARCHAR(255)  NOT NULL,
    `unit`                  VARCHAR(30)   NOT NULL DEFAULT 'lần',
    `total_quantity`        DECIMAL(12,3) NOT NULL,
    `used_quantity`         DECIMAL(12,3) NOT NULL DEFAULT 0.000,
    `remaining_quantity`    DECIMAL(12,3) GENERATED ALWAYS AS (`total_quantity` - `used_quantity`) STORED,
    `unit_price_snapshot`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `version`               INT           NOT NULL DEFAULT 0,
    `last_used_at`          DATETIME(3)   NULL,
    `low_alerted_at`        DATETIME(3)   NULL,
    `created_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)      NULL,
    `updated_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)      NULL,
    `deleted_at`            DATETIME      NULL,
    `deleted_by`            CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_bal_sub_item` (`subscription_id`, `item_type`, `item_ref_id`),
    INDEX `idx_bal_lookup` (`tenant_id`, `item_type`, `item_ref_id`, `remaining_quantity`),
    INDEX `idx_bal_sub` (`subscription_id`),
    CONSTRAINT `fk_bal_subscription` FOREIGN KEY (`subscription_id`) REFERENCES `diab_his_pkg_subscriptions` (`id`) ON DELETE CASCADE,
    CONSTRAINT `chk_balance_nonneg` CHECK (`used_quantity` >= 0 AND `used_quantity` <= `total_quantity`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Snapshot so du dinh muc theo subscription (FR-1202/1204/1205) - remaining_quantity la GENERATED, khong UPDATE truc tiep';

-- ----------------------------------------------------------------
-- 5) diab_his_pkg_usage_logs - Nhat ky tru dinh muc (FR-1204)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_usage_logs` (
    `id`                    CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT           NOT NULL,
    `branch_id`             INT           NULL,
    `subscription_id`       CHAR(36)      NOT NULL,
    `balance_id`            CHAR(36)      NOT NULL,
    `patient_id`            CHAR(36)      NOT NULL,
    `source_type`           ENUM('APPOINTMENT','ENCOUNTER','LAB_ORDER','RAD_ORDER','PRESCRIPTION') NOT NULL,
    `source_id`             CHAR(36)      NOT NULL,
    `source_item_id`        CHAR(36)      NULL,
    `billing_id`            CHAR(36)      NULL,
    `billing_item_id`       CHAR(36)      NULL,
    `requested_quantity`    DECIMAL(12,3) NOT NULL,
    `covered_quantity`      DECIMAL(12,3) NOT NULL,
    `excess_quantity`       DECIMAL(12,3) NOT NULL DEFAULT 0.000,
    `covered_amount`        DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `action`                ENUM('DEDUCT','REVERSE') NOT NULL DEFAULT 'DEDUCT',
    `reversal_of_id`        CHAR(36)      NULL,
    `idempotency_key`       VARCHAR(120)  NOT NULL,
    `used_at`               DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `performed_by`          CHAR(36)      NULL,
    `created_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_usage_idem` (`tenant_id`, `idempotency_key`, `action`),
    INDEX `idx_usage_balance` (`balance_id`, `used_at`),
    INDEX `idx_usage_source` (`tenant_id`, `source_type`, `source_id`),
    INDEX `idx_usage_patient` (`tenant_id`, `patient_id`, `used_at`),
    INDEX `idx_usage_branch` (`tenant_id`, `branch_id`, `used_at`),
    CONSTRAINT `fk_usage_subscription` FOREIGN KEY (`subscription_id`) REFERENCES `diab_his_pkg_subscriptions` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_usage_balance` FOREIGN KEY (`balance_id`) REFERENCES `diab_his_pkg_entitlement_balances` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nhat ky tru/hoan dinh muc (FR-1204) - bat bien, khong soft-delete, chong trung qua idempotency_key';

-- ----------------------------------------------------------------
-- 6) diab_his_pkg_payment_records - Lich su thu tien goi (FR-1202/1203)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pkg_payment_records` (
    `id`                    CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT           NOT NULL,
    `branch_id`             INT           NULL,
    `subscription_id`       CHAR(36)      NOT NULL,
    `billing_id`            CHAR(36)      NULL,
    `payment_id`            CHAR(36)      NULL,
    `payment_kind`          ENUM('DEPOSIT','SETTLEMENT','REFUND') NOT NULL,
    `amount`                DECIMAL(15,2) NOT NULL,
    `method`                VARCHAR(20)   NOT NULL DEFAULT 'CASH',
    `paid_at`               DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `cashier_user_id`       CHAR(36)      NULL,
    `cashier_shift_id`      CHAR(36)      NULL,
    `einvoice_id`           CHAR(36)      NULL,
    `note`                  VARCHAR(500)  NULL,
    `created_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_pay_sub` (`subscription_id`, `paid_at`),
    INDEX `idx_pay_tenant_branch_date` (`tenant_id`, `branch_id`, `paid_at`),
    INDEX `idx_pay_shift` (`cashier_shift_id`),
    INDEX `idx_pay_billing` (`billing_id`),
    CONSTRAINT `fk_pay_subscription` FOREIGN KEY (`subscription_id`) REFERENCES `diab_his_pkg_subscriptions` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich su thu/hoan tien goi (FR-1202/1203) - bat bien; sai thi tao dong REFUND doi ung';
