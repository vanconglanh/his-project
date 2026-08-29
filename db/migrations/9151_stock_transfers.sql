-- ============================================================================
-- 9151_stock_transfers.sql
-- E/Đợt 3: điều chuyển kho nội bộ giữa các chi nhánh (mục 4.2 BRD).
--   State machine 8 trạng thái, ngưỡng duyệt 5.000.000đ (Q5 default, cấu hình theo tenant).
--   BO đã xác nhận CẦN LÀM (mục E/Đợt3 TASKLIST).
-- Căn cứ: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md mục 4.2, AC-4.1.1.
-- Idempotent: CREATE IF NOT EXISTS. Cần 0000_helpers.sql.
-- ============================================================================
SET NAMES utf8mb4;

-- --- Phiếu điều chuyển (2 chi nhánh: from + to) ------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_transfers` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT           NOT NULL,
    `transfer_no`     VARCHAR(30)   NOT NULL COMMENT 'So phieu dieu chuyen (bo dem theo tenant)',
    `from_branch_id`  INT           NOT NULL,
    `to_branch_id`    INT           NOT NULL,
    `status`          VARCHAR(20)   NOT NULL DEFAULT 'DRAFT'
                      COMMENT 'DRAFT|PENDING_APPROVAL|APPROVED|REJECTED|IN_TRANSIT|RECEIVED|COMPLETED|CANCELLED',
    `total_value`     DECIMAL(15,2) NOT NULL DEFAULT 0.00 COMMENT 'Tong gia tri uoc tinh (xet nguong duyet)',
    `requires_approval` TINYINT(1)  NOT NULL DEFAULT 0 COMMENT 'total_value >= nguong (5tr mac dinh)',
    `reason`          VARCHAR(500)  NULL,
    `requested_by`    CHAR(36)      NULL,
    `requested_at`    DATETIME      NULL,
    `approved_by`     CHAR(36)      NULL,
    `approved_at`     DATETIME      NULL,
    `rejected_reason` VARCHAR(500)  NULL,
    `shipped_by`      CHAR(36)      NULL,
    `shipped_at`      DATETIME      NULL,
    `received_by`     CHAR(36)      NULL,
    `received_at`     DATETIME      NULL,
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      CHAR(36)      NULL,
    `updated_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      CHAR(36)      NULL,
    `deleted_at`      DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_transfer_no` (`tenant_id`, `transfer_no`),
    INDEX `idx_transfer_from` (`tenant_id`, `from_branch_id`, `status`),
    INDEX `idx_transfer_to`   (`tenant_id`, `to_branch_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu dieu chuyen kho noi bo giua chi nhanh (BR-51..BR-60)';

-- --- Dòng phiếu (giữ lô + HSD, khớp AC-4.1.1) -------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_transfer_items` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `transfer_id`     CHAR(36)      NOT NULL,
    `tenant_id`       INT           NOT NULL,
    `drug_id`         CHAR(36)      NOT NULL,
    `lot_no`          VARCHAR(100)  NULL,
    `expiry_date`     DATE          NULL,
    `qty_requested`   DECIMAL(15,3) NOT NULL DEFAULT 0.000,
    `qty_shipped`     DECIMAL(15,3) NOT NULL DEFAULT 0.000,
    `qty_received`    DECIMAL(15,3) NOT NULL DEFAULT 0.000,
    `unit_cost`       DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `note`            VARCHAR(300)  NULL,
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_transfer_item_parent` (`transfer_id`),
    INDEX `idx_transfer_item_drug`   (`tenant_id`, `drug_id`),
    CONSTRAINT `fk_transfer_item_parent` FOREIGN KEY (`transfer_id`)
        REFERENCES `diab_his_pha_stock_transfers`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Dong phieu dieu chuyen - giu lot_no/expiry cho hang dang di duong';

-- --- Ngưỡng duyệt điều chuyển (Q5 default 5tr, cấu hình theo tenant) --------
-- Ghi vào sys_settings nếu bảng tồn tại (idempotent, không ghi đè nếu đã có).
DROP PROCEDURE IF EXISTS _seed_transfer_threshold;
DELIMITER $$
CREATE PROCEDURE _seed_transfer_threshold()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
             WHERE table_schema=DATABASE() AND table_name='diab_his_sys_settings') THEN
    INSERT IGNORE INTO diab_his_sys_settings (tenant_id, `key`, `value`, description)
    SELECT DISTINCT b.tenant_id, 'stock_transfer_approval_threshold', '5000000',
           'Nguong gia tri phieu dieu chuyen kho can duyet (VND) - Q5 default'
    FROM diab_his_sys_branches b
    WHERE NOT EXISTS (SELECT 1 FROM diab_his_sys_settings s
                      WHERE s.tenant_id = b.tenant_id AND s.`key` = 'stock_transfer_approval_threshold');
  END IF;
END$$
DELIMITER ;
-- Bọc lỗi nếu sys_settings có cấu trúc cột khác (an toan, khong chan migration).
DROP PROCEDURE IF EXISTS _try_seed_threshold;
DELIMITER $$
CREATE PROCEDURE _try_seed_threshold()
BEGIN
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
    SELECT 'CANH BAO 9151: bo qua seed nguong duyet do cau truc sys_settings khac - cau hinh tay' AS warn;
  CALL _seed_transfer_threshold();
END$$
DELIMITER ;
CALL _try_seed_threshold();
DROP PROCEDURE IF EXISTS _seed_transfer_threshold;
DROP PROCEDURE IF EXISTS _try_seed_threshold;

-- --- Quyền điều chuyển kho ---------------------------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'stock_transfer', SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'stock_transfer.read'    AS code, 'Xem phieu dieu chuyen kho'        AS descr UNION ALL
    SELECT 'stock_transfer.create', 'Tao phieu dieu chuyen kho'                 UNION ALL
    SELECT 'stock_transfer.approve','Duyet phieu dieu chuyen kho (>= nguong)'    UNION ALL
    SELECT 'stock_transfer.ship',   'Xuat hang di (chuyen IN_TRANSIT)'          UNION ALL
    SELECT 'stock_transfer.receive','Nhan hang (chuyen RECEIVED/COMPLETED)'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

DROP PROCEDURE IF EXISTS _grant_transfer_perm;
DELIMITER $$
CREATE PROCEDURE _grant_transfer_perm(IN p_role VARCHAR(50), IN p_perm VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36); DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id=v_role_id AND permission_id=v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
-- Duoc si: tao/ship/receive. Admin: full (bypass san). Duyet (approve) mac dinh admin;
-- duoc si truong khong tach role rieng nen tam cap approve cho admin only -> BO review.
CALL _grant_transfer_perm('duoc_si', 'stock_transfer.read');
CALL _grant_transfer_perm('duoc_si', 'stock_transfer.create');
CALL _grant_transfer_perm('duoc_si', 'stock_transfer.ship');
CALL _grant_transfer_perm('duoc_si', 'stock_transfer.receive');
CALL _grant_transfer_perm('admin',   'stock_transfer.approve');
CALL _grant_transfer_perm('admin',   'stock_transfer.read');
DROP PROCEDURE IF EXISTS _grant_transfer_perm;
