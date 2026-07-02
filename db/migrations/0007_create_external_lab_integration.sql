-- ============================================================
-- Migration: 0007_create_external_lab_integration
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-13, US-SUNS-14, US-SUNS-15
-- Idempotent: YES
-- Ghi chú: Tích hợp với hệ thống xét nghiệm bên ngoài (REST/HL7 MLLP).
--   Credentials API key được mã hóa AES-256-GCM trước khi lưu.
-- ============================================================
SET NAMES utf8mb4;

-- Danh sách đối tác xét nghiệm bên ngoài (lab partner) của tenant
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_partners` (
    `id`                   INT           NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`            INT           NULL                                  COMMENT 'ID tenant sử dụng đối tác này',
    `code`                 VARCHAR(50)   NOT NULL                              COMMENT 'Mã định danh ngắn của lab (vd: MEDLATEC)',
    `name`                 VARCHAR(255)  NOT NULL                              COMMENT 'Tên đầy đủ đối tác xét nghiệm',
    `endpoint_url`         VARCHAR(500)  NULL                                  COMMENT 'URL API endpoint của đối tác',
    `auth_type`            ENUM('NONE','API_KEY','BEARER')
                                         NOT NULL DEFAULT 'API_KEY'            COMMENT 'Phương thức xác thực',
    `credentials_encrypted` TEXT         NULL                                  COMMENT 'Thông tin xác thực đã mã hóa AES-256-GCM',
    `transport`            ENUM('REST','HL7_MLLP')
                                         NOT NULL DEFAULT 'REST'               COMMENT 'Giao thức kết nối',
    `status`               ENUM('ACTIVE','INACTIVE','TESTING')
                                         NOT NULL DEFAULT 'TESTING'            COMMENT 'Trạng thái kết nối',
    `created_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`           INT           NULL                                  COMMENT 'ID người tạo',
    `updated_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                             ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`           INT           NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`           DATETIME      NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_lab_partners_tenant` (`tenant_id`, `status`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách đối tác xét nghiệm bên ngoài tích hợp với hệ thống';

-- Bảng theo dõi đơn XN gửi ra ngoài (outbound)
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_orders_outbound` (
    `id`                INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`         INT          NULL                                  COMMENT 'ID tenant',
    `lab_partner_id`    INT          NOT NULL                              COMMENT 'FK → diab_his_int_lab_partners.id',
    `lab_order_id`      INT          NOT NULL                              COMMENT 'FK → cli_lab_orders.ID (đơn XN nội bộ)',
    `external_order_id` VARCHAR(100) NULL                                  COMMENT 'Mã đơn XN phía đối tác cấp sau khi nhận',
    `payload_json`      JSON         NULL                                  COMMENT 'Nội dung request JSON gửi đối tác',
    `status`            ENUM('PENDING','SENT','ACKED','FAILED')
                                     NOT NULL DEFAULT 'PENDING'            COMMENT 'Trạng thái gửi đơn',
    `error_message`     TEXT         NULL                                  COMMENT 'Thông báo lỗi nếu gửi thất bại',
    `sent_at`           DATETIME     NULL                                  COMMENT 'Thời điểm gửi thành công',
    `acked_at`          DATETIME     NULL                                  COMMENT 'Thời điểm đối tác xác nhận nhận',
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`        INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`        INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`        DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_lab_out_tenant_status` (`tenant_id`, `status`),
    INDEX `idx_lab_out_order`         (`lab_order_id`),
    INDEX `idx_lab_out_partner`       (`lab_partner_id`, `sent_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đơn xét nghiệm gửi ra hệ thống lab bên ngoài';

-- Bảng nhận kết quả XN từ đối tác (inbound)
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_results_inbound` (
    `id`                 INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`          INT          NULL                                  COMMENT 'ID tenant',
    `lab_partner_id`     INT          NOT NULL                              COMMENT 'FK → diab_his_int_lab_partners.id',
    `outbound_id`        INT          NULL                                  COMMENT 'FK → diab_his_int_lab_orders_outbound.id (NULL nếu không match được)',
    `external_result_id` VARCHAR(100) NULL                                  COMMENT 'Mã kết quả phía đối tác',
    `payload_json`       JSON         NOT NULL                              COMMENT 'Kết quả XN dạng JSON chuẩn hóa',
    `raw_hl7_message`    MEDIUMTEXT   NULL                                  COMMENT 'Raw HL7 v2.x message (nếu transport = HL7_MLLP)',
    `received_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm hệ thống nhận kết quả',
    `processed_at`       DATETIME     NULL                                  COMMENT 'Thời điểm xử lý và map vào cli_lab_results',
    `status`             ENUM('RECEIVED','PROCESSING','PROCESSED','ERROR')
                                      NOT NULL DEFAULT 'RECEIVED'           COMMENT 'Trạng thái xử lý kết quả',
    `created_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`         INT          NULL                                  COMMENT 'ID người tạo (hệ thống)',
    `updated_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`         INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`         DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_lab_in_tenant_status`  (`tenant_id`, `status`),
    INDEX `idx_lab_in_outbound`       (`outbound_id`),
    INDEX `idx_lab_in_partner_recv`   (`lab_partner_id`, `received_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Kết quả xét nghiệm nhận từ lab bên ngoài';
