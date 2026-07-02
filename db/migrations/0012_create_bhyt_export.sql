-- ============================================================
-- Migration: 0012_create_bhyt_export
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-BH-01, US-BH-02, US-BH-03, US-BH-04, US-BH-05
-- Idempotent: YES
-- Ghi chú: Xuất hồ sơ BHYT theo QĐ 4750/QĐ-BYT (5 bảng XML).
--   Mỗi kỳ xuất (tháng) tạo 1 bản ghi export, chi tiết từng bảng
--   lưu trong export_items.
-- ============================================================
SET NAMES utf8mb4;

-- Bảng theo dõi lần xuất dữ liệu BHYT theo kỳ
CREATE TABLE IF NOT EXISTS `diab_his_int_bhyt_exports` (
    `id`               INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`        INT          NULL                                  COMMENT 'ID tenant',
    `period_month`     CHAR(7)      NOT NULL                              COMMENT 'Kỳ xuất dạng YYYY-MM (vd: 2026-05)',
    `xml_file_path`    VARCHAR(500) NULL                                  COMMENT 'Đường dẫn file XML tổng hợp trên MinIO',
    `file_size_bytes`  BIGINT       NULL                                  COMMENT 'Kích thước file XML tính bằng byte',
    `status`           ENUM('DRAFT','EXPORTED','SUBMITTED','APPROVED','REJECTED')
                                    NOT NULL DEFAULT 'DRAFT'              COMMENT 'Trạng thái hồ sơ BHYT',
    `submitted_at`     DATETIME     NULL                                  COMMENT 'Thời điểm nộp lên cổng giám định BHYT',
    `response_message` TEXT         NULL                                  COMMENT 'Phản hồi từ cổng giám định BHYT',
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`       INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`       INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`       DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    UNIQUE KEY `uq_bhyt_export_period` (`tenant_id`, `period_month`),
    INDEX `idx_bhyt_export_status`    (`tenant_id`, `status`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Quản lý xuất hồ sơ BHYT theo kỳ tháng (QĐ 4750/QĐ-BYT)';

-- Chi tiết từng bảng trong hồ sơ BHYT (Bảng 1-5 theo QĐ 4750)
CREATE TABLE IF NOT EXISTS `diab_his_int_bhyt_export_items` (
    `id`           INT      NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `export_id`    INT      NOT NULL                              COMMENT 'FK → diab_his_int_bhyt_exports.id',
    `table_no`     TINYINT  NOT NULL                              COMMENT 'Số thứ tự bảng theo QĐ 4750 (1=KCB, 2=Thuốc, 3=DVKT, 4=CĐHA_XN, 5=Tổng hợp)',
    `record_count` INT      NOT NULL DEFAULT 0                   COMMENT 'Số bản ghi trong bảng này',
    `payload_json` JSON     NULL                                  COMMENT 'Nội dung bảng dạng JSON (trước khi render XML)',
    `generated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP   COMMENT 'Thời điểm sinh dữ liệu bảng này',

    UNIQUE KEY `uq_export_table` (`export_id`, `table_no`),
    INDEX `idx_export_items_export` (`export_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiết từng bảng dữ liệu BHYT theo QĐ 4750/QĐ-BYT (Bảng 1-5)';
