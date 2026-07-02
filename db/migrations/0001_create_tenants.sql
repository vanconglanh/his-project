-- ============================================================
-- Migration: 0001_create_tenants
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-TENANT-01, US-TENANT-02
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Bảng quản lý tenant (phòng khám) trong hệ thống SaaS multi-tenant
CREATE TABLE IF NOT EXISTS `diab_his_sys_tenants` (
    `id`                INT             NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `code`              VARCHAR(20)     NOT NULL UNIQUE                      COMMENT 'Mã ngắn định danh tenant (slug), vd: PK001',
    `name`              VARCHAR(255)    NOT NULL                              COMMENT 'Tên đầy đủ phòng khám',
    `cskcb_code`        VARCHAR(20)     NULL                                  COMMENT 'Mã cơ sở khám chữa bệnh do BYT cấp',
    `status`            ENUM('ACTIVE','SUSPENDED','TERMINATED')
                                        NOT NULL DEFAULT 'ACTIVE'             COMMENT 'Trạng thái hoạt động của tenant',
    `tax_code`          VARCHAR(20)     NULL                                  COMMENT 'Mã số thuế doanh nghiệp',
    `address`           TEXT            NULL                                  COMMENT 'Địa chỉ phòng khám',
    `phone`             VARCHAR(20)     NULL                                  COMMENT 'Số điện thoại liên hệ',
    `email`             VARCHAR(100)    NULL                                  COMMENT 'Email liên hệ chính thức',
    `subdomain`         VARCHAR(63)     NULL UNIQUE                           COMMENT 'Subdomain truy cập, vd: phongkhamxyz.suns.com.vn',
    `storage_quota_gb`  INT             NOT NULL DEFAULT 20                   COMMENT 'Hạn mức lưu trữ tính bằng GB',
    `expires_at`        DATETIME        NULL                                  COMMENT 'Ngày hết hạn gói dịch vụ (NULL = không giới hạn)',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo bản ghi',
    `created_by`        INT             NULL                                  COMMENT 'ID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật gần nhất',
    `updated_by`        INT             NULL                                  COMMENT 'ID người cập nhật gần nhất',
    `deleted_at`        DATETIME        NULL                                  COMMENT 'Thời điểm xóa mềm (NULL = chưa xóa)',

    INDEX `idx_tenants_status`  (`status`),
    INDEX `idx_tenants_cskcb`   (`cskcb_code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách phòng khám (tenant) trong hệ thống SaaS';
