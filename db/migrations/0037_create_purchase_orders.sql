-- ============================================================
-- Migration: 0037_create_purchase_orders
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-PH-30 US-PH-31 (Sprint 6-7 EPIC 5)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Suppliers
CREATE TABLE IF NOT EXISTS `diab_his_pha_suppliers` (
    `id`           CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT          NOT NULL,
    `code`         VARCHAR(50)  NOT NULL,
    `name`         VARCHAR(255) NOT NULL,
    `tax_code`     VARCHAR(20)  NULL COMMENT 'Ma so thue',
    `address`      TEXT         NULL,
    `phone`        VARCHAR(20)  NULL,
    `email`        VARCHAR(100) NULL,
    `contact_name` VARCHAR(100) NULL,
    `is_active`    TINYINT(1)   NOT NULL DEFAULT 1,
    `created_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   INT          NULL,
    `updated_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`   INT          NULL,
    `deleted_at`   DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_supplier_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_supplier_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sach nha cung cap thuoc';

-- Purchase Orders
CREATE TABLE IF NOT EXISTS `diab_his_pha_purchase_orders` (
    `id`                CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT          NOT NULL,
    `supplier_id`       CHAR(36)     NOT NULL COMMENT 'FK -> diab_his_pha_suppliers.id',
    `warehouse_id`      INT          NOT NULL COMMENT 'FK -> pha_warehouses.ID',
    `order_no`          VARCHAR(50)  NULL COMMENT 'So phieu dat hang',
    `status`            ENUM('DRAFT','SENT','PARTIAL','RECEIVED','CANCELLED')
                                     NOT NULL DEFAULT 'DRAFT',
    `ordered_at`        DATETIME     NULL,
    `expected_delivery` DATE         NULL COMMENT 'Ngay du kien giao hang',
    `total_amount`      DECIMAL(15,2) NOT NULL DEFAULT 0,
    `note`              TEXT         NULL,
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`        INT          NULL,
    `updated_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        INT          NULL,
    `deleted_at`        DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_po_tenant_status` (`tenant_id`, `status`),
    INDEX `idx_po_supplier`      (`supplier_id`),
    INDEX `idx_po_warehouse`     (`warehouse_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu dat hang thuoc';

-- Purchase Order Items
CREATE TABLE IF NOT EXISTS `diab_his_pha_purchase_order_items` (
    `id`                CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT           NOT NULL,
    `purchase_order_id` CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_pha_purchase_orders.id',
    `drug_id`           INT           NOT NULL COMMENT 'FK -> pha_drug_master.ID',
    `quantity_ordered`  DECIMAL(10,2) NOT NULL,
    `quantity_received` DECIMAL(10,2) NOT NULL DEFAULT 0,
    `unit_price`        DECIMAL(15,2) NOT NULL,
    `line_amount`       DECIMAL(15,2) AS (`quantity_ordered` * `unit_price`) STORED,
    `created_at`        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`        DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_poi_po`     (`purchase_order_id`),
    INDEX `idx_poi_drug`   (`drug_id`),
    INDEX `idx_poi_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiet phieu dat hang thuoc';

-- Goods Received Notes (GRN) header
CREATE TABLE IF NOT EXISTS `diab_his_pha_grn` (
    `id`                CHAR(36)  NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT       NOT NULL,
    `purchase_order_id` CHAR(36)  NOT NULL,
    `received_at`       DATETIME  NOT NULL,
    `received_by`       INT       NULL,
    `note`              TEXT      NULL,
    `created_at`        DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_grn_po` (`purchase_order_id`),
    INDEX `idx_grn_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu nhap kho (Goods Received Note)';
