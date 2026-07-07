-- ============================================================
-- Migration: 9026_create_pha_warehouses
-- Mo ta: Tao bang pha_warehouses (kho duoc). WarehouseHandlers.cs
--   INSERT/UPDATE/DELETE tham chieu bang nay nhung bang chua ton tai
--   -> CREATE/UPDATE/DELETE warehouse 500.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `pha_warehouses` (
  `id`               INT           NOT NULL AUTO_INCREMENT,
  `tenant_id`        INT           NOT NULL,
  `code`             VARCHAR(30)   NOT NULL,
  `name`             VARCHAR(255)  NOT NULL,
  `type`             VARCHAR(20)   NULL,
  `address`          TEXT          NULL,
  `manager_user_id`  INT           NULL,
  `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at`       DATETIME      NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_pha_warehouses_code_tenant` (`tenant_id`, `code`),
  INDEX `idx_pha_warehouses_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Kho duoc (pha_warehouses) — dung boi WarehouseHandlers';
