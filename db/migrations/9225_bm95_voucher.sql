-- =============================================================================
-- 9225_bm95_voucher.sql
-- BM-95 (HIS-49): Voucher engine - bang voucher + lich su dung voucher.
-- Idempotent: CREATE TABLE IF NOT EXISTS. Moi bang co tenant_id.
-- =============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_bil_vouchers (
    id               CHAR(36)      NOT NULL,
    tenant_id        INT           NOT NULL,
    code             VARCHAR(50)   NOT NULL,
    description      VARCHAR(255)  NULL,
    discount_percent DECIMAL(5,2)  NOT NULL,
    applies_to       VARCHAR(20)   NOT NULL DEFAULT 'ALL' COMMENT 'EXAM|LAB|IMAGING|DRUG|ALL',
    valid_from       DATE          NULL,
    valid_to         DATE          NULL,
    max_uses         INT           NULL,
    used_count       INT           NOT NULL DEFAULT 0,
    is_active        TINYINT       NOT NULL DEFAULT 1,
    created_at       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by       CHAR(36)      NULL,
    updated_at       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by       CHAR(36)      NULL,
    deleted_at       DATETIME(3)   NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_voucher_tenant_code (tenant_id, code),
    KEY ix_voucher_tenant_active (tenant_id, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_bil_voucher_usages (
    id              CHAR(36)       NOT NULL,
    tenant_id       INT            NOT NULL,
    voucher_id      CHAR(36)       NOT NULL,
    billing_id      CHAR(36)       NOT NULL,
    discount_amount DECIMAL(15,2)  NOT NULL,
    used_at         DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    used_by         CHAR(36)       NULL,
    PRIMARY KEY (id),
    KEY ix_voucher_usage_voucher (tenant_id, voucher_id),
    KEY ix_voucher_usage_billing (tenant_id, billing_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
