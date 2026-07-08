-- ============================================================
-- Migration: 9062_fix_cls_uploads_guid
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Muc dich: fix luong "CLS nhap file" hong hoan toan:
--   1. Bang diab_his_fil_cls_uploads dang co id/patient_id/encounter_id/
--      uploaded_by/created_by kieu INT nhung code (FileHandlers.cs) doc/ghi
--      toan bo cac cot nay bang GUID string (CHAR(36)) -> insert/select loi.
--      Bang dang 0 dong (chua co du lieu that) -> DROP + CREATE lai dung kieu.
--   2. Endpoint POST /api/v1/files/upload (generic) va CLS upload deu insert
--      vao bang fil_files (metadata file: bucket/object_key/mime/size...)
--      nhung bang nay CHUA TUNG duoc tao -> loi "Table 'fil_files' doesn't exist".
--      Tao moi bang nay khop dung SQL trong FileHandlers.cs.
-- Luu y: bang diab_his_cls_uploads (EF DbSet cu, khong ai dung) GIU NGUYEN,
--        khong dung toi trong migration nay.
-- Idempotent: YES (DROP...CREATE TABLE cho bang rong; CREATE TABLE IF NOT EXISTS cho bang moi).
-- ============================================================
SET NAMES utf8mb4;

-- 1. fil_files — bang metadata file dung chung (generic upload + CLS upload)
CREATE TABLE IF NOT EXISTS `fil_files` (
    `id`               CHAR(36)     NOT NULL,
    `tenant_id`        INT          NOT NULL,
    `bucket`           VARCHAR(100) NOT NULL,
    `object_key`       VARCHAR(500) NOT NULL,
    `file_name`        VARCHAR(255) NOT NULL,
    `mime_type`        VARCHAR(100) NULL,
    `file_size_bytes`  BIGINT       NULL,
    `category`         VARCHAR(50)  NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_fil_files_tenant` (`tenant_id`),
    KEY `idx_fil_files_tenant_bucket` (`tenant_id`, `bucket`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Metadata file luu tren object storage (MinIO/local) - dung chung generic upload va CLS upload';

-- 2. diab_his_fil_cls_uploads — rebuild dung kieu GUID (CHAR(36)) khop code
--    (chi rebuild neu bang dang co kieu SAI, tranh xoa mat du lieu that neu da co)
DROP PROCEDURE IF EXISTS _fix_cls_uploads_guid;
DELIMITER $$
CREATE PROCEDURE _fix_cls_uploads_guid()
BEGIN
    DECLARE col_type VARCHAR(50);
    DECLARE row_count INT DEFAULT 0;

    SELECT DATA_TYPE INTO col_type
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'diab_his_fil_cls_uploads'
      AND COLUMN_NAME = 'id'
    LIMIT 1;

    IF col_type IS NOT NULL AND col_type <> 'char' THEN
        SELECT COUNT(*) INTO row_count FROM diab_his_fil_cls_uploads;
        IF row_count = 0 THEN
            DROP TABLE diab_his_fil_cls_uploads;
        END IF;
    END IF;
END$$
DELIMITER ;
CALL _fix_cls_uploads_guid();
DROP PROCEDURE IF EXISTS _fix_cls_uploads_guid;

CREATE TABLE IF NOT EXISTS `diab_his_fil_cls_uploads` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `patient_id`       CHAR(36)     NOT NULL,
    `encounter_id`     CHAR(36)     NULL,
    `doc_type`         VARCHAR(100) NOT NULL,
    `file_id`          CHAR(36)     NULL COMMENT 'FK toi fil_files.id',
    `file_path`        VARCHAR(500) NOT NULL COMMENT 'object_key tren bucket cls-uploads',
    `file_name`        VARCHAR(255) NOT NULL,
    `mime_type`        VARCHAR(50)  NULL,
    `file_size_bytes`  BIGINT       NULL,
    `note`             TEXT         NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `uploaded_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)     NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)     NULL,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_cls_uploads_tenant_patient` (`tenant_id`, `patient_id`),
    KEY `idx_cls_uploads_tenant_encounter` (`tenant_id`, `encounter_id`),
    KEY `idx_cls_uploads_uploaded_at` (`uploaded_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Tai lieu CLS (XN/CDHA) upload dang file - khop GUID string voi FileHandlers.cs';
