-- ============================================================
-- Migration: 9031_fix_lab_partners_schema
-- Mo ta: Bang deploy diab_his_int_lab_partners dang shape CU (0007: id INT,
--   credentials_encrypted, thieu api_key_encrypted/bearer_token_encrypted/
--   api_key_masked/contact_email/contact_phone/supported_tests) trong khi EF
--   (LabPartnerConfiguration) + handler dung shape MOI (9006b: id CHAR(36)).
--   -> moi thao tac lab-partners deu 500 "Unknown column 'api_key_encrypted'".
--   Bang rong (moi create deu 500) nen recreate an toan theo dung shape 9006b + deleted_by.
-- Idempotent: YES (chi recreate khi thieu cot api_key_encrypted VA bang rong)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_lab_partners_9031;
DELIMITER $$
CREATE PROCEDURE _fix_lab_partners_9031()
BEGIN
    DECLARE has_col INT DEFAULT 0;
    DECLARE row_cnt INT DEFAULT 0;

    SELECT COUNT(*) INTO has_col FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_int_lab_partners'
       AND COLUMN_NAME = 'api_key_encrypted';

    IF has_col = 0 THEN
        SELECT COUNT(*) INTO row_cnt FROM diab_his_int_lab_partners;
        IF row_cnt = 0 THEN
            DROP TABLE diab_his_int_lab_partners;
            CREATE TABLE diab_his_int_lab_partners (
                id                      CHAR(36)     NOT NULL DEFAULT (UUID()),
                tenant_id               INT          NOT NULL,
                code                    VARCHAR(50)  NOT NULL,
                name                    VARCHAR(255) NOT NULL,
                endpoint_url            VARCHAR(500) NOT NULL,
                auth_type               VARCHAR(30)  NOT NULL DEFAULT 'API_KEY',
                api_key_encrypted       BLOB         NULL,
                bearer_token_encrypted  BLOB         NULL,
                api_key_masked          VARCHAR(100) NULL,
                transport               VARCHAR(20)  NOT NULL DEFAULT 'REST',
                supported_tests         JSON         NULL,
                status                  VARCHAR(20)  NOT NULL DEFAULT 'INACTIVE',
                contact_email           VARCHAR(255) NULL,
                contact_phone           VARCHAR(20)  NULL,
                created_at              DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_by              CHAR(36)     NULL,
                updated_at              DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                updated_by              CHAR(36)     NULL,
                deleted_at              DATETIME     NULL,
                deleted_by              CHAR(36)     NULL,
                PRIMARY KEY (id),
                UNIQUE KEY uq_lab_partner_tenant_code (tenant_id, code),
                INDEX idx_lab_partner_status (tenant_id, status)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        END IF;
    END IF;
END$$
DELIMITER ;
CALL _fix_lab_partners_9031();
DROP PROCEDURE IF EXISTS _fix_lab_partners_9031;
