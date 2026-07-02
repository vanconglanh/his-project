-- Migration 0042: Payments, QR codes, eInvoice
-- Idempotent

CREATE TABLE IF NOT EXISTS diab_his_bil_payments (
    id               CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id        INT            NOT NULL,
    billing_id       CHAR(36)       NOT NULL,
    cashier_shift_id CHAR(36)       NULL,
    amount           DECIMAL(15,2)  NOT NULL,
    method           VARCHAR(20)    NOT NULL COMMENT 'CASH|BANK_TRANSFER|VISA|MASTER|QR_VIETQR|QR_MOMO|QR_VNPAY|OTHER',
    status           VARCHAR(20)    NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|COMPLETED|FAILED|REFUNDED|VOID',
    reference        VARCHAR(100)   NULL,
    provider         VARCHAR(50)    NULL,
    provider_txn_id  VARCHAR(100)   NULL,
    paid_at          DATETIME(3)    NULL,
    paid_by          CHAR(36)       NULL,
    note             TEXT           NULL,
    refunded_amount  DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    created_at       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by       CHAR(36)       NULL,
    updated_at       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_provider_txn (provider, provider_txn_id),
    INDEX idx_payment_billing (billing_id),
    INDEX idx_payment_shift (cashier_shift_id),
    INDEX idx_payment_tenant_status (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_bil_qr_codes (
    id              CHAR(36)      NOT NULL DEFAULT (UUID()),
    tenant_id       INT           NOT NULL,
    billing_id      CHAR(36)      NOT NULL,
    provider        VARCHAR(20)   NOT NULL COMMENT 'VIETQR|MOMO|VNPAY',
    qr_payload      MEDIUMTEXT    NOT NULL COMMENT 'base64 PNG',
    qr_url          VARCHAR(500)  NULL,
    amount          DECIMAL(15,2) NOT NULL,
    transaction_ref VARCHAR(50)   NOT NULL,
    expires_at      DATETIME(3)   NOT NULL,
    paid_at         DATETIME(3)   NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|PAID|EXPIRED|CANCELLED',
    created_at      DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    INDEX idx_qr_billing (billing_id),
    INDEX idx_qr_expires (expires_at, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_bil_einvoices (
    id              CHAR(36)      NOT NULL DEFAULT (UUID()),
    tenant_id       INT           NOT NULL,
    billing_id      CHAR(36)      NOT NULL,
    provider        VARCHAR(10)   NOT NULL COMMENT 'MISA|VNPT|EFY',
    invoice_no      VARCHAR(50)   NULL,
    invoice_series  VARCHAR(20)   NULL,
    cqt_code        VARCHAR(13)   NULL COMMENT 'Ma CQT 13 ky tu',
    issue_date      DATETIME(3)   NULL,
    total_amount    DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    vat_amount      DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    status          VARCHAR(20)   NOT NULL DEFAULT 'DRAFT' COMMENT 'DRAFT|ISSUED|CANCELLED|REPLACED',
    pdf_url         VARCHAR(500)  NULL,
    xml_url         VARCHAR(500)  NULL,
    signed_at       DATETIME(3)   NULL,
    cancel_reason   TEXT          NULL,
    cancelled_at    DATETIME(3)   NULL,
    retry_count     INT           NOT NULL DEFAULT 0,
    last_error      TEXT          NULL,
    created_at      DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by      CHAR(36)      NULL,
    updated_at      DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    INDEX idx_einvoice_billing (billing_id),
    INDEX idx_einvoice_tenant_status (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
