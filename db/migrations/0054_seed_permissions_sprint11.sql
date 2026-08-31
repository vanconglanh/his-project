-- Migration: 0054_seed_permissions_sprint11 (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): Seed permission thế hệ cũ dùng cột `updated_at` không tồn tại trên bảng
--   permission (diab_his_sec_permissions chỉ có created_at) -> lỗi 1054.
--   Đã superseded bởi lớp 90xx seed vào bảng canonical. Verify: 180 permissions + 296 mappings.
--   Để no-op. Xem APPLY_ORDER.md.
SELECT '0054_seed_permissions_sprint11: no-op (superseded by 90xx)' AS note;
