-- Migration: 0066_seed_p0_permissions (LEGACY — VÔ HIỆU HÓA)
-- FIX (2026-08-31): File này target ĐÚNG bảng canonical diab_his_sec_permissions nhưng chạy ở
--   vị trí 0066 — TRƯỚC khi bảng được tạo ở 9001_create_sec_all.sql -> lỗi 1146.
--   3 quyền của nó (billing.print, cashier.print_receipt, dtqg.submit) đã được seed vào bảng
--   canonical bởi lớp 90xx (9066_seed_all_gated_permissions). Đã VERIFY: cả 3 mã có mặt trong
--   180 permissions cuối cùng. Để no-op. Xem APPLY_ORDER.md.
SELECT '0066_seed_p0_permissions: no-op (3 quyen billing.print/cashier.print_receipt/dtqg.submit da seed o 90xx)' AS note;
