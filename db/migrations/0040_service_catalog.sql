-- Migration 0040: Service Catalog
-- Idempotent: CREATE TABLE IF NOT EXISTS + unique constraint IF NOT EXISTS

CREATE TABLE IF NOT EXISTS diab_his_bil_services (
    id         CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id  INT            NOT NULL,
    code       VARCHAR(50)    NOT NULL,
    name       VARCHAR(255)   NOT NULL,
    category   VARCHAR(20)    NOT NULL COMMENT 'CONSULTATION|PROCEDURE|LAB|RAD|PHARMACY|OTHER',
    price      DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    vat_rate   TINYINT        NOT NULL DEFAULT 0 COMMENT '0|5|8|10',
    bhyt_code  VARCHAR(50)    NULL,
    bhyt_max_amount DECIMAL(15,2) NULL,
    is_active  TINYINT(1)     NOT NULL DEFAULT 1,
    created_at DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by CHAR(36)       NULL,
    updated_at DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by CHAR(36)       NULL,
    deleted_at DATETIME(3)    NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_service_tenant_code (tenant_id, code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_bil_service_packages (
    id               CHAR(36)      NOT NULL DEFAULT (UUID()),
    tenant_id        INT           NOT NULL,
    code             VARCHAR(50)   NOT NULL,
    name             VARCHAR(255)  NOT NULL,
    discount_percent DECIMAL(5,2)  NOT NULL DEFAULT 0.00,
    valid_from       DATE          NULL,
    valid_to         DATE          NULL,
    is_active        TINYINT(1)    NOT NULL DEFAULT 1,
    created_at       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by       CHAR(36)      NULL,
    updated_at       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by       CHAR(36)      NULL,
    deleted_at       DATETIME(3)   NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_pkg_tenant_code (tenant_id, code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_bil_service_package_items (
    id          CHAR(36)     NOT NULL DEFAULT (UUID()),
    package_id  CHAR(36)     NOT NULL,
    service_id  CHAR(36)     NOT NULL,
    quantity    INT          NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_pkg_item (package_id, service_id),
    CONSTRAINT fk_pkg_item_package  FOREIGN KEY (package_id)  REFERENCES diab_his_bil_service_packages(id) ON DELETE CASCADE,
    CONSTRAINT fk_pkg_item_service  FOREIGN KEY (service_id)  REFERENCES diab_his_bil_services(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
