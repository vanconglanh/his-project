-- ============================================================================
-- 9152_service_branch_price_override.sql
-- E/Đợt 3: giá override 3 tầng (tenant gốc -> group -> branch) — BR-70, AC-5.1.x.
--   Snapshot giá vào bil_billing_items (BR-73). BO đã xác nhận CẦN LÀM.
-- Nguyên tắc AN TOÀN (BRD mục 5.1): KHÔNG thêm branch_id vào bảng giá gốc
--   (diab_his_bil_services). Tạo bảng PHỤ override; logic lấy giá: override theo
--   branch nếu có hiệu lực, không thì group, không thì giá gốc.
-- Idempotent: CREATE IF NOT EXISTS + add_col_if_missing.
-- ============================================================================
SET NAMES utf8mb4;

-- --- Bảng override giá dịch vụ theo phạm vi (BRANCH hoặc GROUP) --------------
CREATE TABLE IF NOT EXISTS `diab_his_bil_service_branch_prices` (
    `id`             CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT           NOT NULL,
    `service_id`     CHAR(36)      NOT NULL COMMENT 'FK diab_his_bil_services.id',
    `scope`          VARCHAR(10)   NOT NULL DEFAULT 'BRANCH' COMMENT 'BRANCH|GROUP',
    `branch_id`      INT           NULL COMMENT 'khi scope=BRANCH',
    `group_id`       INT           NULL COMMENT 'khi scope=GROUP',
    `price`          DECIMAL(15,2) NOT NULL,
    `effective_from` DATE          NOT NULL,
    `effective_to`   DATE          NULL COMMENT 'NULL = vo thoi han',
    `note`           VARCHAR(300)  NULL,
    `created_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)      NULL,
    `updated_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)      NULL,
    `deleted_at`     DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_svc_price_lookup` (`tenant_id`, `service_id`, `scope`, `branch_id`, `group_id`, `effective_from`),
    INDEX `idx_svc_price_branch` (`tenant_id`, `branch_id`, `effective_from`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Override gia dich vu theo branch/group - BR-70. Chong chong lap kiem tra o app (PRICE_OVERLAP AC-5.1.3)';

-- --- Snapshot giá đã áp vào dòng hoá đơn (BR-73) -----------------------------
-- Ghi lại giá gốc + nguồn override tại thời điểm lập hoá đơn để đối soát về sau,
-- không phụ thuộc override đổi sau này.
CALL add_col_if_missing('diab_his_bil_billing_items', 'base_unit_price',
     "DECIMAL(15,2) NULL COMMENT 'Gia goc dich vu truoc override (snapshot BR-73)'");
CALL add_col_if_missing('diab_his_bil_billing_items', 'price_source',
     "VARCHAR(20) NULL COMMENT 'BASE|BRANCH_OVERRIDE|GROUP_OVERRIDE - nguon gia da ap'");
CALL add_col_if_missing('diab_his_bil_billing_items', 'price_override_id',
     "CHAR(36) NULL COMMENT 'FK diab_his_bil_service_branch_prices.id neu ap override'");

-- Ghi chú: bảng bil_billing_items base charset unicode_ci; cột them van tuong thich.
-- Logic chọn giá (override -> group -> base) + kiểm PRICE_OVERLAP thực thi ở tầng
-- application (chưa có handler — thuộc PR code Đợt 3, xem docs/leader/tong-ket).
