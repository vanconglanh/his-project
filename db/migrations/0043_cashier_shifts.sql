-- Migration 0043: Cashier Shifts
-- Idempotent

CREATE TABLE IF NOT EXISTS diab_his_bil_cashier_shifts (
    id                CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id         INT            NOT NULL,
    cashier_user_id   CHAR(36)       NOT NULL,
    shift_date        DATE           NOT NULL,
    shift_start       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    shift_end         DATETIME(3)    NULL,
    opening_balance   DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    closing_balance   DECIMAL(15,2)  NULL,
    expected_cash     DECIMAL(15,2)  NULL,
    actual_cash       DECIMAL(15,2)  NULL,
    difference        DECIMAL(15,2)  NULL,
    total_cash        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_card        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_transfer    DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_qr          DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_other       DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_refund      DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    total_void        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    count_transactions INT           NOT NULL DEFAULT 0,
    breakdown_json    JSON           NULL,
    status            VARCHAR(10)    NOT NULL DEFAULT 'OPEN' COMMENT 'OPEN|CLOSED',
    note              TEXT           NULL,
    closed_by         CHAR(36)       NULL,
    created_at        DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at        DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    INDEX idx_shift_tenant_user (tenant_id, cashier_user_id),
    INDEX idx_shift_date (tenant_id, shift_date),
    INDEX idx_shift_status (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
