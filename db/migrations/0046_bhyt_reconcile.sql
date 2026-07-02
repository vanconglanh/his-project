-- ============================================================
-- Migration: 0046_bhyt_reconcile
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-BH06
-- Idempotent: YES
-- Ghi chu: Tao bang luu ket qua doi soat giam dinh BHYT
--   - diab_his_int_bhyt_reconcile_uploads: file ket qua tu cong BHYT
--   - diab_his_int_bhyt_reconcile_items: chi tiet tung dong duoc/bi tu choi
-- ============================================================
SET NAMES utf8mb4;

-- Bang theo doi file upload ket qua giam dinh BHYT
CREATE TABLE IF NOT EXISTS `diab_his_int_bhyt_reconcile_uploads` (
    `id`            CHAR(36)     NOT NULL DEFAULT (UUID())  PRIMARY KEY COMMENT 'UUID khoa chinh',
    `tenant_id`     INT          NOT NULL                               COMMENT 'ID tenant',
    `export_id`     INT          NOT NULL                               COMMENT 'FK -> diab_his_int_bhyt_exports.id',
    `file_path`     VARCHAR(500) NOT NULL                               COMMENT 'Duong dan file XML ket qua tren MinIO',
    `file_size`     BIGINT       NULL                                   COMMENT 'Kich thuoc file (bytes)',
    `uploaded_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP     COMMENT 'Thoi diem upload',
    `parsed_at`     DATETIME     NULL                                   COMMENT 'Thoi diem parse xong',
    `parse_status`  ENUM('PENDING','PARSING','PARSED','FAILED')
                                 NOT NULL DEFAULT 'PENDING'             COMMENT 'Trang thai xu ly file',
    `parse_error`   TEXT         NULL                                   COMMENT 'Loi parse (neu co)',
    `created_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP     COMMENT 'Thoi diem tao',
    `created_by`    CHAR(36)     NULL                                   COMMENT 'UUID nguoi upload',
    `updated_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                     ON UPDATE CURRENT_TIMESTAMP        COMMENT 'Thoi diem cap nhat',
    `updated_by`    CHAR(36)     NULL                                   COMMENT 'UUID nguoi cap nhat',
    `deleted_at`    DATETIME     NULL                                   COMMENT 'Soft delete',

    INDEX `idx_reconcile_uploads_export` (`export_id`),
    INDEX `idx_reconcile_uploads_tenant` (`tenant_id`, `parse_status`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Theo doi file ket qua giam dinh BHYT upload tu phong kham';

-- Bang chi tiet tung dong ket qua doi soat
CREATE TABLE IF NOT EXISTS `diab_his_int_bhyt_reconcile_items` (
    `id`                     CHAR(36)      NOT NULL DEFAULT (UUID())  PRIMARY KEY COMMENT 'UUID khoa chinh',
    `tenant_id`              INT           NOT NULL                               COMMENT 'ID tenant',
    `upload_id`              CHAR(36)      NOT NULL                               COMMENT 'FK -> diab_his_int_bhyt_reconcile_uploads.id',
    `export_id`              INT           NOT NULL                               COMMENT 'FK -> diab_his_int_bhyt_exports.id',
    `export_item_id`         INT           NULL                                   COMMENT 'FK -> diab_his_int_bhyt_export_items.id (matched)',
    `table_no`               TINYINT       NOT NULL                               COMMENT 'So thu tu bang (1-5)',
    `ma_lien_ket`            VARCHAR(200)  NOT NULL                               COMMENT 'Ma lien ket tu file doi soat',
    `request_amount`         DECIMAL(18,2) NOT NULL DEFAULT 0                     COMMENT 'Tien yeu cau goc',
    `approved_amount`        DECIMAL(18,2) NOT NULL DEFAULT 0                     COMMENT 'Tien duoc duyet',
    `rejected_amount`        DECIMAL(18,2) NOT NULL DEFAULT 0                     COMMENT 'Tien bi tu choi',
    `rejection_code`         VARCHAR(50)   NULL                                   COMMENT 'Ma tu choi (BHYT)',
    `rejection_reason`       VARCHAR(500)  NULL                                   COMMENT 'Ly do tu choi',
    `status`                 ENUM('APPROVED','REJECTED','ADJUSTED','DISPUTED','ACCEPTED')
                                           NOT NULL DEFAULT 'APPROVED'            COMMENT 'Trang thai doi soat',
    `dispute_reason`         TEXT          NULL                                   COMMENT 'Ly do khieu nai',
    `dispute_evidence_path`  VARCHAR(500)  NULL                                   COMMENT 'Duong dan file chung minh khieu nai',
    `disputed_at`            DATETIME      NULL                                   COMMENT 'Thoi diem khieu nai',
    `disputed_by`            CHAR(36)      NULL                                   COMMENT 'UUID nguoi khieu nai',
    `accepted_at`            DATETIME      NULL                                   COMMENT 'Thoi diem chap nhan',
    `accepted_by`            CHAR(36)      NULL                                   COMMENT 'UUID nguoi chap nhan',
    `note`                   VARCHAR(500)  NULL                                   COMMENT 'Ghi chu',
    `created_at`             DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP     COMMENT 'Thoi diem tao',
    `updated_at`             DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                               ON UPDATE CURRENT_TIMESTAMP        COMMENT 'Thoi diem cap nhat',

    INDEX `idx_reconcile_items_export`  (`export_id`, `status`),
    INDEX `idx_reconcile_items_upload`  (`upload_id`),
    INDEX `idx_reconcile_items_ma_lk`   (`ma_lien_ket`),
    INDEX `idx_reconcile_items_tenant`  (`tenant_id`, `table_no`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiet doi soat tung dong ket qua giam dinh BHYT';
