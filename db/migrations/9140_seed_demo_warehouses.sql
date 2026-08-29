-- =============================================================================
-- 9140_seed_demo_warehouses.sql
-- Seed kho dược demo cho tenant 1. pha_warehouses đang RỖNG -> dropdown chọn kho
-- ở màn Phát thuốc / Điều chỉnh tồn trống, dược sĩ không phát thuốc được
-- (kèm BUG-07 đã fix URL /pharmacy/warehouses). Idempotent qua UNIQUE(tenant_id,code).
-- =============================================================================
INSERT INTO pha_warehouses (tenant_id, code, name, type, address, branch_id)
SELECT 1, 'KHO_CHINH', N'Kho chính', 'MAIN', N'Quầy dược tầng 1', 1
WHERE NOT EXISTS (SELECT 1 FROM pha_warehouses WHERE tenant_id=1 AND code='KHO_CHINH');

INSERT INTO pha_warehouses (tenant_id, code, name, type, address, branch_id)
SELECT 1, 'KHO_LE', N'Kho lẻ cấp phát', 'RETAIL', N'Quầy phát thuốc ngoại trú', 1
WHERE NOT EXISTS (SELECT 1 FROM pha_warehouses WHERE tenant_id=1 AND code='KHO_LE');
