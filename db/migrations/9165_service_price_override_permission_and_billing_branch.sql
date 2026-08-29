-- ============================================================================
-- 9165_service_price_override_permission_and_billing_branch.sql
-- E/Đợt 3: quyền service.price_override (BR-74) cho CRUD override giá dịch vụ
--   + đảm bảo diab_his_bil_billing có branch_id (đã có ở nhánh khác nhưng migration
--     idempotent nên kiểm tra lại an toàn, phục vụ resolver giá 3 tầng BR-70..76).
-- Idempotent: CREATE IF NOT EXISTS + add_col_if_missing + INSERT IGNORE.
-- Cần 0000_helpers.sql (add_col_if_missing/add_index_if_missing) đã chạy trước.
-- ============================================================================
SET NAMES utf8mb4;

-- --- Bảo đảm cột branch_id tồn tại trên diab_his_bil_billing (dùng để resolve giá theo BR-70) --
CALL add_col_if_missing('diab_his_bil_billing', 'branch_id', 'INT NULL');
CALL add_index_if_missing('diab_his_bil_billing', 'idx_billing_branch', '(`tenant_id`, `branch_id`)');

-- --- Quyền service.price_override (BR-74: chỉ admin/quan_ly_vung được sửa giá override) --------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'service.price_override', 'service', 'price_override',
       'Tao/sua/xoa gia override dich vu theo chi nhanh/nhom (BR-70..BR-76)', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'service.price_override');

-- Cấp cho admin + quan_ly_vung (neu role da ton tai; role chua co thi bo qua an toan,
-- khong lam fail migration - xem mau _grant_group_view trong 9150).
DROP PROCEDURE IF EXISTS _grant_price_override;
DELIMITER $$
CREATE PROCEDURE _grant_price_override(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'service.price_override' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_price_override('admin');
CALL _grant_price_override('quan_ly_vung');
DROP PROCEDURE IF EXISTS _grant_price_override;
