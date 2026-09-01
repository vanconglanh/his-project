-- ============================================================
-- Migration: 9195_bank_reconciliation
-- Muc dich: F-02 Doi soat ngan hang/POS - import file sao ke ngan hang
--   (Excel/CSV) + auto-matching voi khoan thu trong diab_his_bil_payments.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_bil_bank_statements` (
    `id`             CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT          NOT NULL,
    `branch_id`      INT          NULL,
    `file_name`      VARCHAR(255) NOT NULL,
    `bank_code`      VARCHAR(50)  NULL,
    `statement_date` DATE         NULL,
    `total_lines`    INT          NOT NULL DEFAULT 0,
    `matched_lines`  INT          NOT NULL DEFAULT 0,
    `uploaded_by`    CHAR(36)     NULL,
    `uploaded_at`    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_at`     DATETIME(3)  NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`     CHAR(36)     NULL,
    `updated_at`     DATETIME(3)  NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `deleted_at`     DATETIME(3)  NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_bank_stmt_tenant_branch` (`tenant_id`, `branch_id`),
    INDEX `idx_bank_stmt_tenant_date` (`tenant_id`, `statement_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Sao ke ngan hang/POS da import de doi soat voi diab_his_bil_payments';

CREATE TABLE IF NOT EXISTS `diab_his_bil_bank_statement_lines` (
    `id`                 CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`          INT             NOT NULL,
    `statement_id`       CHAR(36)        NOT NULL,
    `transaction_date`   DATE            NULL,
    `amount`             DECIMAL(15,2)   NOT NULL,
    `reference_no`       VARCHAR(100)    NULL,
    `description`        VARCHAR(500)    NULL,
    `matched_payment_id` CHAR(36)        NULL,
    `match_status`       ENUM('MATCHED','UNMATCHED','MANUAL_MATCHED','IGNORED') NOT NULL DEFAULT 'UNMATCHED',
    `matched_at`         DATETIME(3)     NULL,
    `matched_by`         CHAR(36)        NULL,
    `created_at`         DATETIME(3)     NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updated_at`         DATETIME(3)     NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`id`),
    INDEX `idx_bank_stmt_line_stmt` (`tenant_id`, `statement_id`),
    INDEX `idx_bank_stmt_line_status` (`tenant_id`, `match_status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiet tung dong sao ke ngan hang va trang thai khop voi payment';

-- Permission da co san: payment.read (GET), payment.collect (import/manual-match/ignore/unmatch)
-- Khong can migration them permission moi cho tinh nang nay.
