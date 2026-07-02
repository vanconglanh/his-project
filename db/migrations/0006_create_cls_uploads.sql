-- ============================================================
-- Migration: 0006_create_cls_uploads
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-03, US-SUNS-04, US-SUNS-05
-- Idempotent: YES
-- Ghi chú: Lưu metadata file CLS (phiếu XN, CĐHA, v.v.) do lễ tân
--   hoặc bác sĩ upload. File thực tế lưu trên MinIO.
-- ============================================================
SET NAMES utf8mb4;

-- Bảng lưu metadata các file CLS upload của bệnh nhân
CREATE TABLE IF NOT EXISTS `diab_his_fil_cls_uploads` (
    `id`               INT           NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`        INT           NULL                                  COMMENT 'ID tenant sở hữu bản ghi',
    `patient_id`       INT           NOT NULL                              COMMENT 'FK → pat_patients.ID',
    `encounter_id`     INT           NULL                                  COMMENT 'FK → cli_visits.ID (NULL nếu upload trước khi khám)',
    `doc_type`         VARCHAR(100)  NOT NULL                              COMMENT 'Loại tài liệu lễ tân nhập tự do (vd: Xét nghiệm máu, X-quang)',
    `file_path`        VARCHAR(500)  NOT NULL                              COMMENT 'Đường dẫn file trên MinIO (bucket/object-key)',
    `file_name`        VARCHAR(255)  NOT NULL                              COMMENT 'Tên file gốc khi upload',
    `mime_type`        VARCHAR(50)   NULL                                  COMMENT 'MIME type (application/pdf, image/jpeg, v.v.)',
    `file_size_bytes`  BIGINT        NULL                                  COMMENT 'Kích thước file tính bằng byte',
    `uploaded_by`      INT           NULL                                  COMMENT 'ID người upload',
    `uploaded_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm upload',
    `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo bản ghi',
    `created_by`       INT           NULL                                  COMMENT 'ID người tạo',
    `updated_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật gần nhất',
    `updated_by`       INT           NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`       DATETIME      NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_cls_uploads_patient`   (`tenant_id`, `patient_id`),
    INDEX `idx_cls_uploads_encounter` (`encounter_id`),
    INDEX `idx_cls_uploads_uploaded`  (`uploaded_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Metadata file CLS (xét nghiệm, CĐHA) do lễ tân/bác sĩ upload';
