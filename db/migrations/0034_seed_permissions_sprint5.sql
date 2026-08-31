-- Migration: 0034_seed_permissions_sprint5 (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): Seed vào bảng `iam_permissions` — bảng NÀY KHÔNG BAO GIỜ tồn tại trong
--   toàn bộ chain (tên bảng sai của thế hệ cũ) -> lỗi 1146. Đã superseded bởi lớp 90xx seed
--   vào bảng canonical diab_his_sec_permissions. Verify: 180 permissions + 296 mappings.
--   Để no-op. Xem APPLY_ORDER.md.
SELECT '0034_seed_permissions_sprint5: no-op (target iam_permissions khong ton tai; superseded by 90xx)' AS note;
