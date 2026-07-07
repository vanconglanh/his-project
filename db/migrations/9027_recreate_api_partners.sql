-- ============================================================
-- Migration: 9027_recreate_api_partners
-- Mo ta: Bang diab_his_api_partners (tao boi 0008) co id INT +
--   thieu cot api_key_prefix/scopes/ip_whitelist. Handler
--   ApiPartnerHandlers.cs dung UUID_TO_BIN/BIN_TO_UUID (can id
--   BINARY(16)) + cac cot tren -> CRUD api-partner 500.
--   Bang la du lieu tich hop ngoai (thuong rong) -> tao lai dung schema.
-- Idempotent: YES (chi tao lai khi id con la INT)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_api_partners_9027;
DELIMITER $$
CREATE PROCEDURE _fix_api_partners_9027()
BEGIN
    DECLARE idt VARCHAR(64);
    SELECT DATA_TYPE INTO idt FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_api_partners' AND COLUMN_NAME = 'id';

    IF idt = 'int' OR idt IS NULL THEN
        SET FOREIGN_KEY_CHECKS = 0;
        DROP TABLE IF EXISTS `diab_his_api_partners`;
        CREATE TABLE `diab_his_api_partners` (
          `id`                 BINARY(16)    NOT NULL,
          `tenant_id`          INT           NOT NULL,
          `name`               VARCHAR(255)  NOT NULL,
          `contact_email`      VARCHAR(255)  NULL,
          `api_key_hash`       VARCHAR(255)  NOT NULL,
          `api_key_prefix`     VARCHAR(30)   NULL,
          `scopes`             JSON          NULL,
          `rate_limit_per_min` INT           NOT NULL DEFAULT 60,
          `daily_quota`        INT           NOT NULL DEFAULT 10000,
          `status`             VARCHAR(20)   NOT NULL DEFAULT 'ACTIVE',
          `expires_at`         DATETIME      NULL,
          `ip_whitelist`       JSON          NULL,
          `created_at`         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
          `created_by`         CHAR(36)      NULL,
          `updated_at`         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
          `updated_by`         CHAR(36)      NULL,
          `deleted_at`         DATETIME      NULL,
          PRIMARY KEY (`id`),
          INDEX `idx_api_partners_tenant` (`tenant_id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
          COMMENT='API partners (id BINARY(16)) — dung boi ApiPartnerHandlers';
        SET FOREIGN_KEY_CHECKS = 1;
    END IF;
END$$
DELIMITER ;

CALL _fix_api_partners_9027();
DROP PROCEDURE IF EXISTS _fix_api_partners_9027;
