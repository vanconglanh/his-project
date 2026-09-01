-- ============================================================
-- Migration: 9194_sys_setting_meta
-- Muc dich: Viec 3.1 + Viec 4 (audit-hardcode-vs-master-data) - bang metadata
--   mo ta cac key trong diab_his_sys_settings (nhan tieng Viet, kieu du lieu,
--   nhom hien thi UI admin, co cho FE public doc hay khong).
--   is_public=1 -> key duoc lo ra qua GET /api/v1/settings/public (moi user
--   dang nhap deu doc duoc) de FE khong con hardcode nguong nhu 5.000.000.
--   CHI danh dau is_public=1 cho key KHONG nhay cam (khong phai token/bi mat).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT ... ON DUPLICATE KEY)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sys_setting_meta` (
    `setting_key`    VARCHAR(100) NOT NULL,
    `label_vi`       VARCHAR(200) NOT NULL,
    `description_vi` VARCHAR(500) NULL,
    `data_type`      VARCHAR(20)  NOT NULL COMMENT 'int|decimal|bool|string',
    `value_group`    VARCHAR(50)  NOT NULL DEFAULT 'Chung',
    `sort_order`     INT          NOT NULL DEFAULT 0,
    `is_public`      TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'FE duoc doc qua GET /api/v1/settings/public',
    `default_value`  VARCHAR(500) NULL,
    `created_at`     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updated_at`     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`setting_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Metadata mo ta cac key trong diab_his_sys_settings, phuc vu UI admin cau hinh + whitelist public';

INSERT INTO `diab_his_sys_setting_meta`
    (`setting_key`, `label_vi`, `description_vi`, `data_type`, `value_group`, `sort_order`, `is_public`, `default_value`)
VALUES
    ('stock_transfer_approval_threshold',
        'Ngưỡng duyệt điều chuyển kho',
        'Giá trị điều chuyển kho vượt ngưỡng này sẽ yêu cầu phê duyệt bổ sung',
        'decimal', 'Kho', 10, 1, '5000000'),
    ('pkg.min_deposit_percent',
        'Tỷ lệ cọc tối thiểu gói (%)',
        'Tỷ lệ % tối thiểu khách hàng phải đặt cọc khi mua gói dịch vụ trả trước',
        'decimal', 'Gói dịch vụ', 10, 0, '50'),
    ('pkg.expiry_remind_days',
        'Số ngày nhắc trước hết hạn gói',
        'Số ngày trước khi gói hết hạn để hệ thống gửi cảnh báo nhắc nhở',
        'int', 'Gói dịch vụ', 20, 0, '7'),
    ('pkg.overdue_alert_days',
        'Số ngày cảnh báo công nợ quá hạn',
        'Số ngày quá hạn công nợ (tính từ ngày mua gói) để cảnh báo',
        'int', 'Gói dịch vụ', 30, 0, '30'),
    ('package_expiry_extension_days',
        'Số ngày gia hạn gói đã hết hạn',
        'Số ngày cho phép gia hạn gói dịch vụ đã hết hạn nhưng còn định mức (0 = tắt tính năng)',
        'int', 'Gói dịch vụ', 40, 0, '0')
ON DUPLICATE KEY UPDATE
    `label_vi` = VALUES(`label_vi`),
    `description_vi` = VALUES(`description_vi`),
    `data_type` = VALUES(`data_type`),
    `value_group` = VALUES(`value_group`),
    `sort_order` = VALUES(`sort_order`),
    `default_value` = VALUES(`default_value`);

-- Permission quan ly settings qua UI admin
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'setting.manage', 'setting', 'manage', 'Quan ly cau hinh he thong (sys_settings) qua UI admin', NOW()
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = 'setting.manage');

DROP PROCEDURE IF EXISTS _grant_setting_perm_9194;
DELIMITER $$
CREATE PROCEDURE _grant_setting_perm_9194(IN p_role VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36); DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'setting.manage' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id=v_role_id AND permission_id=v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_setting_perm_9194('admin');
DROP PROCEDURE IF EXISTS _grant_setting_perm_9194;
