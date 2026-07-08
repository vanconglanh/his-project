-- ============================================================
-- Migration: 9059_rep_definitions_add_shared_roles
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Mo ta: Report Builder P3.2 — chia se bao cao tu tao theo role. Mo rong
--   visibility PRIVATE|TENANT them ROLE (owner + role trong shared_roles_json
--   duoc xem/chay). Cot moi thuan JSON array ma role, vd ["bac_si","ke_toan"].
-- Idempotent: YES (dung add_col_if_missing tu 0000_helpers.sql).
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing(
    'diab_his_rep_definitions',
    'shared_roles_json',
    'JSON NULL COMMENT ''Danh sach role code duoc xem/chay khi visibility=ROLE, vd ["bac_si","ke_toan"]'' AFTER `visibility`'
);
