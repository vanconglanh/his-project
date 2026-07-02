-- ============================================================
-- Migration: 0014_payment_qr_card
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-20, US-SUNS-21
-- Idempotent: YES
-- Ghi chú: bil_billing đã có PAYMENT_METHOD và REFERENCE_NUMBER trong
--   schema cũ. Migration này:
--   1. Thêm payment_method_v2 (snake_case, ENUM chuẩn mới) nếu cần
--   2. Thêm payment_reference (alias snake_case)
--   3. Tạo bảng QR code thanh toán (VietQR, MoMo, VNPay)
-- ============================================================
SET NAMES utf8mb4;

-- Phương thức thanh toán chuẩn mới (ENUM có nhiều giá trị hơn PAYMENT_METHOD cũ)
-- Giữ cột cũ PAYMENT_METHOD để backward compatible, thêm cột mới
CALL add_col_if_missing('bil_billing', 'payment_method_v2',
    "VARCHAR(30) NULL DEFAULT 'CASH' COMMENT 'Phương thức thanh toán chuẩn mới: CASH, BANK_TRANSFER, VISA, MASTER, QR_VIETQR, QR_MOMO, QR_VNPAY'");

-- Alias snake_case cho REFERENCE_NUMBER
CALL add_col_if_missing('bil_billing', 'payment_reference',
    'VARCHAR(100) NULL COMMENT \'Mã tham chiếu giao dịch (alias snake_case của REFERENCE_NUMBER)\'');

-- Bảng quản lý QR code thanh toán
CREATE TABLE IF NOT EXISTS `diab_his_bil_qr_codes` (
    `id`              INT           NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`       INT           NULL                                  COMMENT 'ID tenant',
    `billing_id`      INT           NOT NULL                              COMMENT 'FK → bil_billing.ID',
    `provider`        VARCHAR(30)   NOT NULL                              COMMENT 'Nhà cung cấp: VIETQR, MOMO, VNPAY, ZALOPAY',
    `qr_payload`      TEXT          NOT NULL                              COMMENT 'Nội dung encode thành QR (chuỗi thanh toán)',
    `qr_image_path`   VARCHAR(500)  NULL                                  COMMENT 'Đường dẫn ảnh QR code trên MinIO',
    `amount`          DECIMAL(15,2) NOT NULL                              COMMENT 'Số tiền thanh toán',
    `expires_at`      DATETIME      NULL                                  COMMENT 'Thời điểm hết hạn QR code',
    `paid_at`         DATETIME      NULL                                  COMMENT 'Thời điểm thanh toán thành công (NULL = chưa thanh toán)',
    `transaction_ref` VARCHAR(100)  NULL                                  COMMENT 'Mã giao dịch từ nhà cung cấp sau khi thanh toán',
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo QR code',
    `created_by`      INT           NULL                                  COMMENT 'ID người tạo (thu ngân)',
    `updated_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`      INT           NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`      DATETIME      NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_qr_billing`    (`billing_id`),
    INDEX `idx_qr_tenant_paid` (`tenant_id`, `paid_at`),
    INDEX `idx_qr_expires`    (`expires_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='QR code thanh toán (VietQR, MoMo, VNPay) cho hóa đơn';
