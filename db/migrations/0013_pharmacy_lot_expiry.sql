-- ============================================================
-- Migration: 0013_pharmacy_lot_expiry
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-PH-01, US-PH-02, US-PH-03, US-PH-04, US-PH-05
-- Idempotent: YES
-- Ghi chú: pha_stocks đã có BATCH_NUMBER, EXPIRATION_DATE, MANUFACTURE_DATE
--   trong schema cũ → chỉ add thêm gtin và batch_no (alias snake_case mới).
--   Tạo thêm bảng biến động kho để tracking xuất/nhập chi tiết.
-- ============================================================
SET NAMES utf8mb4;

-- Thêm GTIN (Global Trade Item Number - mã vạch thuốc quốc tế)
CALL add_col_if_missing('pha_stocks', 'gtin',
    'VARCHAR(14) NULL COMMENT \'Mã vạch GTIN-14 của thuốc (GS1 standard)\'');

-- Alias snake_case cho BATCH_NUMBER (cột mới tham chiếu từ code mới)
CALL add_col_if_missing('pha_stocks', 'batch_no',
    'VARCHAR(50) NULL COMMENT \'Alias snake_case của BATCH_NUMBER, dùng trong API mới\'');

-- Alias snake_case cho EXPIRATION_DATE
CALL add_col_if_missing('pha_stocks', 'expiry_date',
    'DATE NULL COMMENT \'Alias snake_case của EXPIRATION_DATE, dùng trong API mới\'');

-- Alias snake_case cho MANUFACTURE_DATE
CALL add_col_if_missing('pha_stocks', 'manufacture_date',
    'DATE NULL COMMENT \'Alias snake_case của MANUFACTURE_DATE, dùng trong API mới\'');

-- Bảng lịch sử biến động kho thuốc (nhập/xuất/chuyển/điều chỉnh)
CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_movements` (
    `id`               INT             NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`        INT             NULL                                  COMMENT 'ID tenant',
    `stock_id`         INT             NOT NULL                              COMMENT 'FK → pha_stocks.ID',
    `warehouse_id`     INT             NOT NULL                              COMMENT 'FK → pha_warehouses.ID',
    `movement_type`    ENUM('IMPORT','EXPORT','TRANSFER','ADJUST','RETURN')
                                       NOT NULL                              COMMENT 'Loại biến động: nhập kho/xuất kho/chuyển kho/điều chỉnh/trả hàng',
    `quantity`         DECIMAL(12,3)   NOT NULL                              COMMENT 'Số lượng biến động (dương=nhập, âm=xuất)',
    `unit_price`       DECIMAL(15,2)   NULL                                  COMMENT 'Đơn giá tại thời điểm biến động',
    `reason`           TEXT            NULL                                  COMMENT 'Lý do biến động (bắt buộc khi ADJUST)',
    `reference_type`   VARCHAR(50)     NULL                                  COMMENT 'Loại chứng từ nguồn: PRESCRIPTION, PURCHASE_ORDER, ADJUSTMENT, v.v.',
    `reference_id`     INT             NULL                                  COMMENT 'ID chứng từ nguồn tương ứng với reference_type',
    `movement_at`      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm thực hiện biến động',
    `performed_by`     INT             NULL                                  COMMENT 'ID dược sĩ/thủ kho thực hiện',
    `created_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo bản ghi',
    `created_by`       INT             NULL                                  COMMENT 'ID người tạo',
    `updated_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                           ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`       INT             NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`       DATETIME        NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_stock_mov_tenant_stock`  (`tenant_id`, `stock_id`, `movement_at`),
    INDEX `idx_stock_mov_warehouse`     (`warehouse_id`, `movement_at`),
    INDEX `idx_stock_mov_type`          (`movement_type`, `movement_at`),
    INDEX `idx_stock_mov_reference`     (`reference_type`, `reference_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lịch sử biến động kho thuốc (nhập/xuất/chuyển/điều chỉnh/trả hàng)';
