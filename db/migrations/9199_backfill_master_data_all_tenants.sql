-- =============================================================================
-- 9199_backfill_master_data_all_tenants.sql
-- BUG: migration 9140_seed_demo_warehouses.sql (va cac seed truoc do) hardcode
-- "tenant 1" -> dropdown "Kho" (pha_warehouses), "Nha cung cap" (diab_his_pha_suppliers)
-- va ca "Chi nhanh" (diab_his_sys_branches) trong man Kho duoc/Mua hang RONG hoan toan
-- cho tenant 2 ("Phong kham DTD DiaB - Moi truong test", tenant dang dung tren
-- his.diab.vn) va bat ky tenant nao khac tao sau nay khong duoc seed thu cong.
-- Root cause: seed cu luon "SELECT 1, ..." (hardcode tenant_id=1) thay vi lap qua
-- moi tenant dang co trong diab_his_sys_tenants.
--
-- Migration nay BACKFILL cho MOI tenant dang thieu (khong hardcode id), va tu
-- dong ap dung cho tenant moi tao sau nay neu quy trinh onboarding tenant chua
-- tu seed rieng. Idempotent: chi INSERT khi chua co ban ghi tuong ung.
-- =============================================================================

-- 1) Chi nhanh mac dinh — tenant nao chua co branch nao thi tao 1 branch MAIN.
--    Day la nen tang de warehouse/user_branches ben duoi gan vao.
INSERT INTO diab_his_sys_branches (tenant_id, code, name, is_active, is_default, status, sort_order)
SELECT t.id, 'MAIN', t.name, 1, 1, 'ACTIVE', 0
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sys_branches b WHERE b.tenant_id = t.id);

-- 2) Kho duoc mac dinh — tenant nao chua co warehouse nao thi tao 2 kho
--    (giong dung pattern 9140 da lam cho tenant 1), gan vao branch mac dinh.
INSERT INTO pha_warehouses (tenant_id, code, name, type, address, branch_id)
SELECT b.tenant_id, 'KHO_CHINH', N'Kho chính', 'MAIN', N'Quầy dược tầng 1', b.id
FROM diab_his_sys_branches b
WHERE b.is_default = 1
  AND NOT EXISTS (SELECT 1 FROM pha_warehouses w WHERE w.tenant_id = b.tenant_id AND w.code = 'KHO_CHINH');

INSERT INTO pha_warehouses (tenant_id, code, name, type, address, branch_id)
SELECT b.tenant_id, 'KHO_LE', N'Kho lẻ cấp phát', 'RETAIL', N'Quầy phát thuốc ngoại trú', b.id
FROM diab_his_sys_branches b
WHERE b.is_default = 1
  AND NOT EXISTS (SELECT 1 FROM pha_warehouses w WHERE w.tenant_id = b.tenant_id AND w.code = 'KHO_LE');

-- 3) Nha cung cap mac dinh — tenant nao chua co supplier nao (bat ky code gi)
--    thi tao 5 nha phan phoi duoc pham thuc te, de dropdown "Nha cung cap" tren
--    man Don dat hang khong bi rong. QUAN TRONG: gate tren "tenant chua co
--    supplier nao" (khong phai "chua co dung code nay") — tenant co the da co
--    sap san supplier voi code khac (vd tenant 1 dung NCC001..5), neu gate theo
--    code se tao TRUNG LAP cung 1 cong ty duoi 2 code khac nhau.
INSERT INTO diab_his_pha_suppliers (tenant_id, code, name, is_active)
SELECT t.id, 'SUP01', N'Công ty CP Xuất nhập khẩu Y tế Imexpharm', 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_suppliers s WHERE s.tenant_id = t.id);

INSERT INTO diab_his_pha_suppliers (tenant_id, code, name, is_active)
SELECT t.id, 'SUP02', N'Công ty CP Dược phẩm Domesco', 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_suppliers s WHERE s.tenant_id = t.id AND s.code <> 'SUP01');

INSERT INTO diab_his_pha_suppliers (tenant_id, code, name, is_active)
SELECT t.id, 'SUP03', N'Công ty CP Traphaco', 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_suppliers s WHERE s.tenant_id = t.id AND s.code NOT IN ('SUP01','SUP02'));

INSERT INTO diab_his_pha_suppliers (tenant_id, code, name, is_active)
SELECT t.id, 'SUP04', N'Công ty CP Dược Hậu Giang', 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_suppliers s WHERE s.tenant_id = t.id AND s.code NOT IN ('SUP01','SUP02','SUP03'));

INSERT INTO diab_his_pha_suppliers (tenant_id, code, name, is_active)
SELECT t.id, 'SUP05', N'STADA Việt Nam', 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_suppliers s WHERE s.tenant_id = t.id AND s.code NOT IN ('SUP01','SUP02','SUP03','SUP04'));

-- 4) Phan cong user vao chi nhanh — user nao chua duoc gan branch nao thi gan
--    vao branch mac dinh cua tenant minh (tranh cac man loc theo chi nhanh bi
--    rong doi voi user khong phai super admin, vd Bac si/Duoc si/Le tan).
INSERT INTO diab_his_sec_user_branches (id, tenant_id, user_id, branch_id, is_primary)
SELECT UUID(), u.tenant_id, u.id, b.id, 1
FROM diab_his_sec_users u
JOIN diab_his_sys_branches b ON b.tenant_id = u.tenant_id AND b.is_default = 1
WHERE u.deleted_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_user_branches ub WHERE ub.user_id = u.id);
