-- ============================================================
-- Migration: 9005_create_pharmacy
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 8 bảng quản lý kho dược (prefix diab_his_pha_*)
--        Bao gồm: drugs, stock, prescriptions, prescription_items,
--        dispenses, suppliers, purchase_orders, grn
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng danh mục thuốc
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_drugs` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `code`              VARCHAR(50)     NOT NULL                            COMMENT 'Mã thuốc nội bộ',
    `name`              VARCHAR(255)    NOT NULL                            COMMENT 'Tên thuốc',
    `generic_name`      VARCHAR(255)    NULL                                COMMENT 'Tên hoạt chất (INN)',
    `brand_name`        VARCHAR(255)    NULL                                COMMENT 'Tên thương mại',
    `drug_form`         VARCHAR(50)     NULL                                COMMENT 'Dạng bào chế: viên, gói, lọ...',
    `strength`          VARCHAR(100)    NULL                                COMMENT 'Hàm lượng (vd: 500mg, 250mg/5ml)',
    `unit`              VARCHAR(20)     NOT NULL                            COMMENT 'Đơn vị tính (Viên, Lọ, Ống...)',
    `atc_code`          VARCHAR(20)     NULL                                COMMENT 'Mã ATC (phân loại thuốc quốc tế)',
    `drug_category`     VARCHAR(50)     NULL                                COMMENT 'Nhóm thuốc',
    `is_controlled`     TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Thuốc gây nghiện/hướng tâm thần',
    `is_antibiotic`     TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Kháng sinh',
    `requires_rx`       TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Cần kê đơn',
    `sell_price`        DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'Giá bán lẻ (VNĐ)',
    `bhyt_price`        DECIMAL(12,2)   NULL                                COMMENT 'Giá BHYT thanh toán',
    `reorder_level`     INT             NOT NULL DEFAULT 10                 COMMENT 'Mức tồn kho cảnh báo nhập hàng',
    `is_active`         TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn kinh doanh',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_drugs_code_tenant`   (`tenant_id`, `code`),
    INDEX `idx_drugs_name`              (`tenant_id`, `name`),
    INDEX `idx_drugs_active`            (`tenant_id`, `is_active`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục thuốc của phòng khám';

-- ============================================================
-- Bảng tồn kho theo lô
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_stock` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `drug_id`           CHAR(36)        NOT NULL                            COMMENT 'UUID thuốc',
    `lot_number`        VARCHAR(50)     NOT NULL                            COMMENT 'Số lô sản xuất',
    `mfg_date`          DATE            NULL                                COMMENT 'Ngày sản xuất',
    `exp_date`          DATE            NOT NULL                            COMMENT 'Hạn sử dụng',
    `quantity`          INT             NOT NULL DEFAULT 0                  COMMENT 'Số lượng tồn kho hiện tại',
    `import_price`      DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'Giá nhập (VNĐ)',
    `location`          VARCHAR(50)     NULL                                COMMENT 'Vị trí kệ/ngăn trong kho',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',

    PRIMARY KEY (`id`),
    INDEX `idx_stock_drug`      (`tenant_id`, `drug_id`),
    INDEX `idx_stock_exp`       (`tenant_id`, `exp_date`),
    CONSTRAINT `fk_stock_drug` FOREIGN KEY (`drug_id`)
        REFERENCES `diab_his_pha_drugs` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Tồn kho thuốc theo lô và hạn sử dụng';

-- ============================================================
-- Bảng đơn thuốc
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_prescriptions` (
    `id`                    CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`             INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`          CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám',
    `patient_id`            CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `doctor_id`             CHAR(36)        NOT NULL                            COMMENT 'UUID bác sĩ kê đơn',
    `prescription_no`       VARCHAR(30)     NULL                                COMMENT 'Số đơn thuốc',
    `status`                VARCHAR(20)     NOT NULL DEFAULT 'DRAFT'            COMMENT 'Trạng thái: DRAFT, SIGNED, DISPENSED, CANCELLED',
    `dtqg_code`             VARCHAR(50)     NULL                                COMMENT 'Mã đơn thuốc Quốc gia (sau khi đẩy)',
    `dtqg_qr`               VARCHAR(500)    NULL                                COMMENT 'Chuỗi QR code đơn thuốc Quốc gia',
    `dtqg_pushed_at`        DATETIME        NULL                                COMMENT 'Thời điểm đẩy lên ĐTQG thành công',
    `diagnosis_icd10`       VARCHAR(10)     NULL                                COMMENT 'Chẩn đoán chính theo ICD-10',
    `signed_at`             DATETIME        NULL                                COMMENT 'Thời điểm ký đơn',
    `dispensed_at`          DATETIME        NULL                                COMMENT 'Thời điểm cấp phát',
    `note`                  TEXT            NULL                                COMMENT 'Hướng dẫn dùng thuốc chung',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`            CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`            CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`            DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`            CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_presc_encounter`     (`tenant_id`, `encounter_id`),
    INDEX `idx_presc_patient`       (`tenant_id`, `patient_id`),
    INDEX `idx_presc_status`        (`tenant_id`, `status`),
    INDEX `idx_presc_dtqg`          (`dtqg_code`),
    CONSTRAINT `fk_presc_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`),
    CONSTRAINT `fk_presc_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đơn thuốc kê cho bệnh nhân';

-- ============================================================
-- Bảng chi tiết đơn thuốc
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_prescription_items` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `prescription_id`   CHAR(36)        NOT NULL                            COMMENT 'UUID đơn thuốc',
    `drug_id`           CHAR(36)        NOT NULL                            COMMENT 'UUID thuốc',
    `drug_name`         VARCHAR(255)    NOT NULL                            COMMENT 'Tên thuốc tại thời điểm kê đơn',
    `drug_strength`     VARCHAR(100)    NULL                                COMMENT 'Hàm lượng',
    `quantity`          INT             NOT NULL                            COMMENT 'Số lượng kê',
    `unit`              VARCHAR(20)     NOT NULL                            COMMENT 'Đơn vị',
    `dosage`            VARCHAR(255)    NOT NULL                            COMMENT 'Liều dùng (vd: 1 viên x 2 lần/ngày)',
    `frequency`         VARCHAR(100)    NULL                                COMMENT 'Tần suất dùng',
    `duration_days`     INT             NULL                                COMMENT 'Số ngày dùng',
    `route`             VARCHAR(50)     NULL                                COMMENT 'Đường dùng: uống, tiêm, bôi...',
    `unit_price`        DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'Đơn giá',
    `line_total`        DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'Thành tiền',
    `bhyt_applicable`   TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'BHYT có chi trả không',
    `note`              VARCHAR(500)    NULL                                COMMENT 'Ghi chú riêng từng thuốc',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',

    PRIMARY KEY (`id`),
    INDEX `idx_presc_items_presc`   (`tenant_id`, `prescription_id`),
    CONSTRAINT `fk_presc_items_presc` FOREIGN KEY (`prescription_id`)
        REFERENCES `diab_his_pha_prescriptions` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_presc_items_drug` FOREIGN KEY (`drug_id`)
        REFERENCES `diab_his_pha_drugs` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiết từng thuốc trong đơn thuốc';

-- ============================================================
-- Bảng phiếu cấp phát thuốc
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_dispenses` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `prescription_id`   CHAR(36)        NOT NULL                            COMMENT 'UUID đơn thuốc',
    `dispensed_by`      CHAR(36)        NOT NULL                            COMMENT 'UUID dược sĩ cấp phát',
    `dispensed_at`      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm cấp phát',
    `items_json`        JSON            NOT NULL                            COMMENT 'Danh sách thuốc cấp phát (drug_id, lot_number, qty)',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú cấp phát',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',

    PRIMARY KEY (`id`),
    INDEX `idx_dispenses_presc`     (`tenant_id`, `prescription_id`),
    INDEX `idx_dispenses_date`      (`tenant_id`, `dispensed_at`),
    CONSTRAINT `fk_dispenses_presc` FOREIGN KEY (`prescription_id`)
        REFERENCES `diab_his_pha_prescriptions` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phiếu cấp phát thuốc từ kho dược';

-- ============================================================
-- Bảng nhà cung cấp
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_suppliers` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `code`          VARCHAR(30)     NOT NULL                            COMMENT 'Mã nhà cung cấp',
    `name`          VARCHAR(255)    NOT NULL                            COMMENT 'Tên công ty',
    `contact_name`  VARCHAR(100)    NULL                                COMMENT 'Tên người liên hệ',
    `phone`         VARCHAR(30)     NULL                                COMMENT 'Số điện thoại',
    `email`         VARCHAR(100)    NULL                                COMMENT 'Email liên hệ',
    `address`       TEXT            NULL                                COMMENT 'Địa chỉ',
    `tax_code`      VARCHAR(20)     NULL                                COMMENT 'Mã số thuế',
    `drug_license`  VARCHAR(50)     NULL                                COMMENT 'Số giấy phép kinh doanh dược',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn hoạt động',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_suppliers_code_tenant`   (`tenant_id`, `code`),
    INDEX `idx_suppliers_active`            (`tenant_id`, `is_active`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách nhà cung cấp thuốc';

-- ============================================================
-- Bảng đơn đặt hàng nhập dược
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_purchase_orders` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `po_no`         VARCHAR(30)     NOT NULL                            COMMENT 'Số đặt hàng',
    `supplier_id`   CHAR(36)        NOT NULL                            COMMENT 'UUID nhà cung cấp',
    `status`        VARCHAR(20)     NOT NULL DEFAULT 'DRAFT'            COMMENT 'Trạng thái: DRAFT, SENT, PARTIAL, RECEIVED, CANCELLED',
    `order_date`    DATE            NOT NULL                            COMMENT 'Ngày đặt hàng',
    `expected_date` DATE            NULL                                COMMENT 'Ngày dự kiến nhận hàng',
    `total_amount`  DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Tổng giá trị đơn hàng',
    `note`          TEXT            NULL                                COMMENT 'Ghi chú',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_po_no_tenant`    (`tenant_id`, `po_no`),
    INDEX `idx_po_supplier`         (`tenant_id`, `supplier_id`),
    INDEX `idx_po_status`           (`tenant_id`, `status`),
    CONSTRAINT `fk_po_supplier` FOREIGN KEY (`supplier_id`)
        REFERENCES `diab_his_pha_suppliers` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đơn đặt hàng nhập thuốc từ nhà cung cấp';

-- ============================================================
-- Bảng phiếu nhập kho (Goods Receipt Note)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_grn` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `grn_no`        VARCHAR(30)     NOT NULL                            COMMENT 'Số phiếu nhập kho',
    `po_id`         CHAR(36)        NULL                                COMMENT 'UUID đơn đặt hàng (có thể nhập tự do)',
    `supplier_id`   CHAR(36)        NOT NULL                            COMMENT 'UUID nhà cung cấp',
    `received_date` DATE            NOT NULL                            COMMENT 'Ngày nhận hàng',
    `invoice_no`    VARCHAR(50)     NULL                                COMMENT 'Số hóa đơn nhà cung cấp',
    `total_amount`  DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Tổng giá trị nhập kho',
    `items_json`    JSON            NOT NULL                            COMMENT 'Chi tiết từng dòng hàng (drug_id, lot, qty, price, exp)',
    `note`          TEXT            NULL                                COMMENT 'Ghi chú',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_grn_no_tenant`   (`tenant_id`, `grn_no`),
    INDEX `idx_grn_po`              (`tenant_id`, `po_id`),
    INDEX `idx_grn_supplier`        (`tenant_id`, `supplier_id`),
    INDEX `idx_grn_date`            (`tenant_id`, `received_date`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phiếu nhập kho thuốc (Goods Receipt Note)';
