-- Migration 0053: Cache tables for nightly report materialization (Sprint 11)
-- MySQL 8 compatible

CREATE TABLE IF NOT EXISTS diab_his_rep_daily_revenue_cache (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      CHAR(36)     NOT NULL,
    period_key     VARCHAR(20)  NOT NULL COMMENT 'e.g. 2026-05-22 or 2026-W21 or 2026-05',
    data_json      JSON         NOT NULL,
    refreshed_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_rev_cache (tenant_id, period_key),
    INDEX idx_rev_tenant_period (tenant_id, period_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_rep_doctor_kpi_cache (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      CHAR(36)     NOT NULL,
    period_key     VARCHAR(20)  NOT NULL,
    data_json      JSON         NOT NULL,
    refreshed_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_kpi_cache (tenant_id, period_key),
    INDEX idx_kpi_tenant_period (tenant_id, period_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_rep_top_drugs_cache (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      CHAR(36)     NOT NULL,
    period_key     VARCHAR(20)  NOT NULL,
    data_json      JSON         NOT NULL,
    refreshed_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_drugs_cache (tenant_id, period_key),
    INDEX idx_drugs_tenant_period (tenant_id, period_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_rep_inventory_value_cache (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      CHAR(36)     NOT NULL,
    period_key     VARCHAR(20)  NOT NULL,
    data_json      JSON         NOT NULL,
    refreshed_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_inv_cache (tenant_id, period_key),
    INDEX idx_inv_tenant_period (tenant_id, period_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_rep_diabetes_cohort_cache (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      CHAR(36)     NOT NULL,
    period_key     VARCHAR(20)  NOT NULL,
    data_json      JSON         NOT NULL,
    refreshed_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uq_cohort_cache (tenant_id, period_key),
    INDEX idx_cohort_tenant_period (tenant_id, period_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
