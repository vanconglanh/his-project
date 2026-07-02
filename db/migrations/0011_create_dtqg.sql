-- ============================================================
-- Migration: 0011_create_dtqg
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-PR-04, US-PR-05
-- Idempotent: YES
-- Ghi chú: Tích hợp Đơn thuốc Quốc gia (donthuocquocgia.vn) theo
--   TT 27/2021/TT-BYT. Mỗi tenant có credentials riêng (mã hóa AES-256-GCM).
--   Sau khi submit thành công nhận ma_don_thuoc để in QR code.
-- ============================================================
SET NAMES utf8mb4;

-- Thông tin xác thực ĐTQG per tenant
CREATE TABLE IF NOT EXISTS `diab_his_int_dtqg_credentials` (
    `id`              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`       INT          NOT NULL UNIQUE                      COMMENT 'FK → diab_his_sys_tenants.id (1 tenant 1 credential)',
    `cskcb_id`        VARCHAR(20)  NULL                                  COMMENT 'Mã cơ sở KCB đăng ký với ĐTQG',
    `partner_code`    VARCHAR(50)  NULL                                  COMMENT 'Mã đối tác do ĐTQG cấp',
    `token_encrypted` TEXT         NULL                                  COMMENT 'Token xác thực đã mã hóa AES-256-GCM',
    `token_expires_at` DATETIME    NULL                                  COMMENT 'Thời điểm hết hạn token',
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`      INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                       ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`      INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`      DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm'
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Credentials xác thực Đơn thuốc Quốc gia theo TT 27/2021/TT-BYT';

-- Theo dõi trạng thái submit đơn thuốc lên ĐTQG
CREATE TABLE IF NOT EXISTS `diab_his_int_dtqg_submissions` (
    `id`              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`       INT          NULL                                  COMMENT 'ID tenant',
    `prescription_id` INT          NOT NULL                              COMMENT 'FK → pha_prescriptions.ID',
    `ma_don_thuoc`    VARCHAR(100) NULL                                  COMMENT 'Mã đơn thuốc ĐTQG cấp sau khi accept',
    `qr_payload`      TEXT         NULL                                  COMMENT 'Nội dung QR code (mã + URL verify)',
    `qr_image_path`   VARCHAR(500) NULL                                  COMMENT 'Đường dẫn ảnh QR code trên MinIO',
    `status`          ENUM('PENDING','SUBMITTED','ACCEPTED','REJECTED')
                                   NOT NULL DEFAULT 'PENDING'            COMMENT 'Trạng thái submit đơn thuốc',
    `error_code`      VARCHAR(50)  NULL                                  COMMENT 'Mã lỗi từ ĐTQG (nếu bị từ chối)',
    `error_message`   TEXT         NULL                                  COMMENT 'Mô tả lỗi chi tiết từ ĐTQG',
    `submitted_at`    DATETIME     NULL                                  COMMENT 'Thời điểm gửi lên ĐTQG',
    `accepted_at`     DATETIME     NULL                                  COMMENT 'Thời điểm ĐTQG xác nhận hợp lệ',
    `retry_count`     INT          NOT NULL DEFAULT 0                    COMMENT 'Số lần thử lại khi gặp lỗi tạm thời',
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`      INT          NULL                                  COMMENT 'ID người tạo (bác sĩ kê đơn)',
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                       ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`      INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`      DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_dtqg_tenant_status`  (`tenant_id`, `status`),
    INDEX `idx_dtqg_prescription`   (`prescription_id`),
    INDEX `idx_dtqg_submitted`      (`submitted_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Theo dõi submit đơn thuốc lên Đơn thuốc Quốc gia';
