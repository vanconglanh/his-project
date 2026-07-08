-- ============================================================
-- Migration: 9041_tenants_add_slogan_website
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-07
-- Story refs: Redesign header trang (letterhead) cho PDF bao cao/giay to/don thuoc
-- Idempotent: YES (dung add_col_if_missing tu 0000_helpers.sql)
-- Muc dich: Bo sung cot slogan + website cho diab_his_sys_tenants de
--           phuc vu header trang moi (dong bo mau don thuoc that diaB).
-- ============================================================
SET NAMES utf8mb4;

-- Slogan/khau hieu trung tam (dong nho phia tren ten phong kham trong letterhead)
CALL add_col_if_missing(
    'diab_his_sys_tenants',
    'slogan',
    'VARCHAR(255) NULL COMMENT ''Slogan/khau hieu in tren letterhead PDF'' AFTER `company_name`'
);

-- Website phong kham (in trong hang lien he cua letterhead)
CALL add_col_if_missing(
    'diab_his_sys_tenants',
    'website',
    'VARCHAR(255) NULL COMMENT ''Website phong kham in tren letterhead PDF'' AFTER `email_support`'
);

-- Backfill du lieu demo cho tenant dev (id=1) de xem duoc full look header moi
-- khi cac cot con NULL/rong (khong ghi de du lieu da co).
UPDATE diab_his_sys_tenants
   SET slogan = 'TRUNG TÂM ĐIỀU TRỊ TÍCH HỢP CÂN NẶNG VÀ BỆNH MẠN TÍNH'
 WHERE id = 1 AND (slogan IS NULL OR slogan = '');

UPDATE diab_his_sys_tenants
   SET website = 'diab.com.vn'
 WHERE id = 1 AND (website IS NULL OR website = '');

UPDATE diab_his_sys_tenants
   SET company_name = 'CÔNG TY TNHH CÔNG NGHỆ Y TẾ DIA-B'
 WHERE id = 1 AND (company_name IS NULL OR company_name = '');
