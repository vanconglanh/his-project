-- Migration: 0060_seed_permissions_sprint13 (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): Seed vào `diab_his_permissions` — bảng KHÔNG tồn tại trong chain -> lỗi 1146.
--   Đã superseded bởi lớp 90xx seed vào bảng canonical diab_his_sec_permissions.
--   Verify: 180 permissions + 296 mappings. Để no-op. Xem APPLY_ORDER.md.
SELECT '0060_seed_permissions_sprint13: no-op (target diab_his_permissions khong ton tai; superseded by 90xx)' AS note;
