-- Migration: 0030_seed_permissions_sprint3 (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): Seed vào diab_his_sec_permissions nhưng chạy ở vị trí 0030 — TRƯỚC khi
--   bảng này được tạo ở 9001_create_sec_all.sql -> lỗi 1146 "Table doesn't exist".
--   Đã superseded bởi lớp 90xx seed vào bảng canonical. Verify: 180 permissions + 296 mappings.
--   Để no-op. Xem APPLY_ORDER.md.
SELECT '0030_seed_permissions_sprint3: no-op (superseded by 90xx)' AS note;
