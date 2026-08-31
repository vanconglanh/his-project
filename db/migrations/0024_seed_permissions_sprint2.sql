-- Migration: 0024_seed_permissions_sprint2 (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): Seed permission thế hệ cũ (INSERT `sec_permissions`.resource lowercase)
--   -> lỗi 1054. Đã superseded bởi lớp 90xx seed vào bảng canonical diab_his_sec_permissions.
--   Verify: 180 permissions + 296 mappings. Để no-op. Xem APPLY_ORDER.md.
SELECT '0024_seed_permissions_sprint2: no-op (superseded by 90xx)' AS note;
