-- ============================================================
-- Migration: 0065_add_letterhead_fields
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-26
-- Story refs: Feature "In báo cáo A4" (plan jaunty-cherny)
-- Idempotent: YES (dùng add_col_if_missing từ 0000_helpers.sql)
-- Mục đích: Bổ sung các cột letterhead cho `diab_his_sys_tenants`
--           phục vụ render header/footer báo cáo PDF & HTML preview.
-- Quyết định: KHÔNG tạo bảng `diab_his_sys_tenant_settings` mới.
--             Letterhead 1-1 với tenant -> mở rộng trực tiếp `diab_his_sys_tenants`.
-- ============================================================
SET NAMES utf8mb4;

-- Tên pháp nhân (in dòng dưới tên trung tâm trong letterhead)
CALL add_col_if_missing(
    'diab_his_sys_tenants',
    'company_name',
    'VARCHAR(255) NULL COMMENT ''Tên pháp nhân in trên letterhead báo cáo'' AFTER `name`'
);

-- URL logo (MinIO) cho letterhead
CALL add_col_if_missing(
    'diab_his_sys_tenants',
    'logo_url',
    'VARCHAR(512) NULL COMMENT ''URL/path logo (MinIO) dùng cho letterhead'' AFTER `company_name`'
);

-- Email hỗ trợ (in trên footer) — tách khỏi `email` liên hệ chính
CALL add_col_if_missing(
    'diab_his_sys_tenants',
    'email_support',
    'VARCHAR(100) NULL COMMENT ''Email hỗ trợ in trên footer báo cáo'' AFTER `email`'
);
