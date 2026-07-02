-- ============================================================
-- Migration: 9004_create_labrad
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 5 bảng cận lâm sàng (xét nghiệm + chẩn đoán hình ảnh)
--        Bao gồm: lab_orders, lab_results, rad_orders, rad_results, cls_uploads
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng chỉ định xét nghiệm
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_lab_orders` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`      CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám',
    `test_code`         VARCHAR(50)     NOT NULL                            COMMENT 'Mã xét nghiệm',
    `test_name`         VARCHAR(255)    NOT NULL                            COMMENT 'Tên xét nghiệm',
    `sample_type`       VARCHAR(50)     NULL                                COMMENT 'Loại mẫu: máu, nước tiểu, đờm...',
    `priority`          VARCHAR(20)     NOT NULL DEFAULT 'NORMAL'           COMMENT 'Mức ưu tiên: NORMAL, URGENT, STAT',
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'ordered'          COMMENT 'Trạng thái: ordered, sample_taken, processing, done, cancelled',
    `ordered_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm chỉ định',
    `ordered_by`        CHAR(36)        NULL                                COMMENT 'UUID bác sĩ chỉ định',
    `scheduled_for`     DATETIME        NULL                                COMMENT 'Thời điểm dự kiến lấy mẫu',
    `lab_partner_id`    CHAR(36)        NULL                                COMMENT 'UUID đơn vị xét nghiệm (ngoài/liên kết)',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú lâm sàng',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_lab_orders_encounter`    (`tenant_id`, `encounter_id`),
    INDEX `idx_lab_orders_status`       (`tenant_id`, `status`),
    CONSTRAINT `fk_lab_orders_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phiếu chỉ định xét nghiệm cận lâm sàng';

-- ============================================================
-- Bảng kết quả xét nghiệm
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_lab_results` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `order_id`          CHAR(36)        NOT NULL                            COMMENT 'UUID phiếu chỉ định',
    `test_code`         VARCHAR(50)     NOT NULL                            COMMENT 'Mã xét nghiệm',
    `test_name`         VARCHAR(255)    NOT NULL                            COMMENT 'Tên xét nghiệm',
    `result_value`      VARCHAR(255)    NULL                                COMMENT 'Giá trị kết quả',
    `result_unit`       VARCHAR(50)     NULL                                COMMENT 'Đơn vị đo',
    `normal_range`      VARCHAR(100)    NULL                                COMMENT 'Khoảng bình thường tham chiếu',
    `is_abnormal`       TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Kết quả bất thường',
    `result_flag`       VARCHAR(10)     NULL                                COMMENT 'Cờ: H (cao), L (thấp), C (nguy cấp)',
    `result_json`       JSON            NULL                                COMMENT 'Kết quả đầy đủ dạng JSON (nhiều chỉ số)',
    `result_pdf_path`   VARCHAR(500)    NULL                                COMMENT 'Đường dẫn file PDF kết quả',
    `performed_at`      DATETIME        NULL                                COMMENT 'Thời điểm thực hiện xét nghiệm',
    `performed_by`      VARCHAR(255)    NULL                                COMMENT 'Tên kỹ thuật viên thực hiện',
    `note`              TEXT            NULL                                COMMENT 'Nhận xét kết quả',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_lab_results_order`   (`tenant_id`, `order_id`),
    CONSTRAINT `fk_lab_results_order` FOREIGN KEY (`order_id`)
        REFERENCES `diab_his_lab_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Kết quả xét nghiệm cận lâm sàng';

-- ============================================================
-- Bảng chỉ định chẩn đoán hình ảnh
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_rad_orders` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`      CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám',
    `modality`          VARCHAR(20)     NOT NULL                            COMMENT 'Phương thức: XR, CT, MRI, US, ECG...',
    `body_part`         VARCHAR(100)    NULL                                COMMENT 'Vùng cơ thể chụp',
    `contrast`          TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Có sử dụng thuốc cản quang không',
    `procedure_code`    VARCHAR(50)     NOT NULL                            COMMENT 'Mã thủ thuật CĐHA',
    `procedure_name`    VARCHAR(255)    NOT NULL                            COMMENT 'Tên thủ thuật',
    `priority`          VARCHAR(20)     NOT NULL DEFAULT 'NORMAL'           COMMENT 'Mức ưu tiên: NORMAL, URGENT, STAT',
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'ordered'          COMMENT 'Trạng thái: ordered, scheduled, in_progress, done, cancelled',
    `ordered_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm chỉ định',
    `ordered_by`        CHAR(36)        NULL                                COMMENT 'UUID bác sĩ chỉ định',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú lâm sàng',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_rad_orders_encounter`    (`tenant_id`, `encounter_id`),
    INDEX `idx_rad_orders_status`       (`tenant_id`, `status`),
    CONSTRAINT `fk_rad_orders_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phiếu chỉ định chẩn đoán hình ảnh';

-- ============================================================
-- Bảng kết quả chẩn đoán hình ảnh
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_rad_results` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `order_id`          CHAR(36)        NOT NULL                            COMMENT 'UUID phiếu chỉ định',
    `impression`        TEXT            NULL                                COMMENT 'Kết luận đọc phim',
    `description`       TEXT            NULL                                COMMENT 'Mô tả chi tiết hình ảnh',
    `recommendation`    TEXT            NULL                                COMMENT 'Khuyến nghị điều trị',
    `result_pdf_path`   VARCHAR(500)    NULL                                COMMENT 'Đường dẫn file PDF kết quả',
    `image_paths`       JSON            NULL                                COMMENT 'Danh sách đường dẫn ảnh DICOM/JPEG',
    `performed_at`      DATETIME        NULL                                COMMENT 'Thời điểm thực hiện',
    `performed_by`      VARCHAR(255)    NULL                                COMMENT 'Tên bác sĩ đọc phim',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_rad_results_order`   (`tenant_id`, `order_id`),
    CONSTRAINT `fk_rad_results_order` FOREIGN KEY (`order_id`)
        REFERENCES `diab_his_rad_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Kết quả chẩn đoán hình ảnh';

-- ============================================================
-- Bảng tài liệu CLS đính kèm (upload MinIO)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_cls_uploads` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `encounter_id`      CHAR(36)        NULL                                COMMENT 'UUID lượt khám (có thể null)',
    `doc_type`          VARCHAR(50)     NOT NULL                            COMMENT 'Loại tài liệu: LAB_RESULT, RAD_IMAGE, ECG, OTHER',
    `file_id`           CHAR(36)        NULL                                COMMENT 'UUID file trong MinIO',
    `file_path`         VARCHAR(500)    NOT NULL                            COMMENT 'Đường dẫn MinIO bucket/object',
    `file_name`         VARCHAR(255)    NOT NULL                            COMMENT 'Tên file gốc',
    `mime_type`         VARCHAR(100)    NULL                                COMMENT 'MIME type (image/jpeg, application/pdf...)',
    `file_size_bytes`   BIGINT          NULL                                COMMENT 'Kích thước file (byte)',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú tài liệu',
    `uploaded_by`       CHAR(36)        NULL                                COMMENT 'UUID người upload',
    `uploaded_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm upload',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    INDEX `idx_cls_uploads_patient`     (`tenant_id`, `patient_id`),
    INDEX `idx_cls_uploads_encounter`   (`tenant_id`, `encounter_id`),
    INDEX `idx_cls_uploads_doc_type`    (`tenant_id`, `doc_type`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Metadata tài liệu CLS đính kèm (ảnh, PDF kết quả)';
