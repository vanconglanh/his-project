-- ============================================================
-- Migration: 9196_setting_mandatory_mfa_roles
-- Muc dich: Viec 1 - dua cau hinh "role bat buoc 2FA" (FR-1011) vao man
--   /admin/settings de admin bat/tat qua UI, ap dung ngay khong can deploy lai.
--   Truoc day chi doc tu appsettings Security:MandatoryMfaRoles (bien moi truong luc deploy).
--   Setting key moi: security.mandatory_mfa_roles (CSV cac role_code, vd "admin" hoac "admin,bac_si").
--   - Chuoi rong ("") = KHONG bat buoc 2FA cho role nao (admin da tat han).
--   - Chua co row setting nao => LoginCommandHandler fallback ve appsettings roi ve ["admin"]
--     (giu nguyen hanh vi hien tai, khong pha vo).
--   data_type = 'string' => UI SettingRow.tsx render text input, admin nhap CSV role_code.
--   is_public = 0 (nhay cam bao mat, khong lo ra GET /settings/public).
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE tren PK setting_key)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sys_setting_meta`
    (`setting_key`, `label_vi`, `description_vi`, `data_type`, `value_group`, `sort_order`, `is_public`, `default_value`)
VALUES
    ('security.mandatory_mfa_roles',
        'Vai trò bắt buộc xác thực 2 lớp (2FA)',
        'Danh sách mã vai trò (role_code) bắt buộc bật 2FA khi đăng nhập, phân tách bằng dấu phẩy (vd: admin hoặc admin,bac_si). Để trống = không bắt buộc 2FA cho vai trò nào. Áp dụng ngay ở lần đăng nhập kế tiếp.',
        'string', 'Bảo mật', 10, 0, 'admin')
ON DUPLICATE KEY UPDATE
    `label_vi` = VALUES(`label_vi`),
    `description_vi` = VALUES(`description_vi`),
    `data_type` = VALUES(`data_type`),
    `value_group` = VALUES(`value_group`),
    `sort_order` = VALUES(`sort_order`),
    `is_public` = VALUES(`is_public`),
    `default_value` = VALUES(`default_value`);
