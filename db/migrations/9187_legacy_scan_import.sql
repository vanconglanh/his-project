-- ============================================================================
-- 9187_legacy_scan_import.sql
--
-- Nhap lieu hang loat ho so giay cu dang anh scan: admin upload 1 file ZIP
-- chua nhieu anh (jpg/png) -> giai nen an toan -> OCR (Tesseract) tung anh ->
-- tao item cho admin review/match benh nhan -> confirm -> luu thanh tai lieu
-- dinh kem ho so benh nhan (INSERT vao diab_his_fil_cls_uploads, KHONG tu tao
-- benh an/luot kham).
--
--   1. diab_his_leg_import_batch — 1 lan upload 1 file ZIP.
--   2. diab_his_leg_import_item  — 1 anh trong ZIP, 1 item cho toi khi confirm/reject.
--
-- Idempotent: CREATE TABLE IF NOT EXISTS + INSERT permission dung WHERE NOT EXISTS.
-- ============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_leg_import_batch` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `zip_file_name`    VARCHAR(255) NULL,
    `zip_object_key`   VARCHAR(500) NULL COMMENT 'bucket legacy-scans - file ZIP goc',
    `total_items`      INT          NOT NULL DEFAULT 0,
    `processed_items`  INT          NOT NULL DEFAULT 0,
    `status`           VARCHAR(20)  NOT NULL DEFAULT 'pending' COMMENT 'pending|processing|done|failed',
    `error_message`    VARCHAR(1000) NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_leg_import_batch_tenant_status` (`tenant_id`, `status`),
    KEY `idx_leg_import_batch_tenant_created` (`tenant_id`, `created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Batch nhap lieu ho so giay cu tu file ZIP anh scan (migration 1 lan, chi admin)';

CREATE TABLE IF NOT EXISTS `diab_his_leg_import_item` (
    `id`                  CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT          NOT NULL,
    `batch_id`            CHAR(36)     NOT NULL,
    `original_filename`   VARCHAR(255) NULL,
    `image_object_key`    VARCHAR(500) NULL COMMENT 'bucket/key anh tren legacy-scans',
    `ocr_text`            LONGTEXT     NULL,
    `ocr_confidence`      DECIMAL(5,2) NULL,
    `matched_patient_id`  CHAR(36)     NULL,
    `match_method`        VARCHAR(20)  NULL COMMENT 'filename_auto|manual',
    `status`              VARCHAR(20)  NOT NULL DEFAULT 'pending_match' COMMENT 'pending_match|pending_review|confirmed|rejected|failed',
    `saved_cls_upload_id` CHAR(36)     NULL COMMENT 'id dong diab_his_fil_cls_uploads sinh ra khi confirm',
    `item_error`          VARCHAR(1000) NULL,
    `confirmed_by`        CHAR(36)     NULL,
    `confirmed_at`        DATETIME     NULL,
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`          DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_leg_import_item_tenant_batch` (`tenant_id`, `batch_id`),
    KEY `idx_leg_import_item_tenant_status` (`tenant_id`, `status`),
    KEY `idx_leg_import_item_tenant_patient` (`tenant_id`, `matched_patient_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='1 anh trong batch nhap lieu ho so giay cu - cho admin review/match/confirm';

-- --- Quyen legacy_import.write (chi admin thao tac tinh nang migration nhay cam nay) ---------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'legacy_import.write', 'legacy_import', 'write',
       'Nhap lieu hang loat ho so giay cu dang anh scan (upload ZIP, OCR, match, confirm)', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'legacy_import.write');

-- Cap cho role admin (tinh nang chi danh cho admin).
DROP PROCEDURE IF EXISTS _grant_legacy_import_write;
DELIMITER $$
CREATE PROCEDURE _grant_legacy_import_write(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'legacy_import.write' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_legacy_import_write('admin');
DROP PROCEDURE IF EXISTS _grant_legacy_import_write;
