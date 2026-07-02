-- ============================================================
-- Migration: 9022_fix_legacy_perm_views
-- Engine: MySQL 8.0+
-- Mô tả: Bổ sung 2 VIEW legacy còn THIẾU mà 9009_create_legacy_views.sql bỏ sót.
--        JwtService.LoadPermissions() (raw SQL) JOIN sec_user_roles → sec_role_permissions
--        → sec_permissions. 9009 chỉ tạo view `sec_user_roles`, thiếu 2 view kia
--        → query lỗi bị nuốt (catch) → token user thường KHÔNG có claim `permissions`
--        → mọi endpoint [RequirePermission] trả 403 cho role thật (le_tan/bac_si/duoc_si/ke_toan).
-- Idempotent: YES (CREATE OR REPLACE VIEW)
-- ============================================================
CREATE OR REPLACE VIEW sec_permissions      AS SELECT * FROM diab_his_sec_permissions;
CREATE OR REPLACE VIEW sec_role_permissions AS SELECT * FROM diab_his_sec_role_permissions;
