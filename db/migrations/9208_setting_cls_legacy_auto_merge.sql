-- ============================================================
-- Migration: 9208_setting_cls_legacy_auto_merge
-- Nguon: Lark Bug-Feedback (BM-18) - "Chua ro luong xu ly muc 'Chi dinh
-- chua gom dot'". PO xac nhan (2026-09-10): ho tro CA HAI che do, chon qua
-- man hinh /admin/settings:
--   - true  = TU DONG gom moi chi dinh "chua gom dot" cua luot kham vao dot
--     MOI ngay khi Bac si tao dot (xem CreateClsRoundCommandHandler).
--   - false (MAC DINH) = Bac si TU CHON gom qua endpoint rieng
--     POST /cls-rounds/{id}/assign-legacy-orders (AssignLegacyOrdersToRoundCommand).
-- Dich vu sau khi gom (ca 2 che do) duoc tinh tien + hien tren phieu in binh
-- thuong nhu dich vu khac trong dot (khong xu ly rieng).
-- data_type = 'bool' => UI SettingRow.tsx render toggle bat/tat.
-- is_public = 1: khong nhay cam, cho phep FE doc qua GET /settings/public
-- neu sau nay can hien thi/an nut "Gop vao dot" theo che do dang chon.
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE tren PK setting_key).
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sys_setting_meta`
    (`setting_key`, `label_vi`, `description_vi`, `data_type`, `value_group`, `sort_order`, `is_public`, `default_value`)
VALUES
    ('cls.legacy_auto_merge',
        'Tự động gộp chỉ định chưa gộp đợt vào đợt mới',
        'Bật: hệ thống tự động gộp các chỉ định XN/CĐHA "chưa gộp đợt" của lượt khám vào đợt chỉ định mới ngay khi bác sĩ tạo. Tắt (mặc định): bác sĩ tự chọn gộp qua nút "Gộp vào đợt" ở mục Chỉ định chưa gộp đợt.',
        'bool', 'Cận lâm sàng', 20, 1, '0')
ON DUPLICATE KEY UPDATE
    `label_vi` = VALUES(`label_vi`),
    `description_vi` = VALUES(`description_vi`),
    `data_type` = VALUES(`data_type`),
    `value_group` = VALUES(`value_group`),
    `sort_order` = VALUES(`sort_order`),
    `is_public` = VALUES(`is_public`),
    `default_value` = VALUES(`default_value`);
