-- ============================================================================
-- 9150_branch_groups_and_status.sql
-- E/Đợt 2: nhóm chi nhánh (chỉ type REGION, bỏ HOSPITAL theo Q3=Không) + quyền
--          branch.group_view (BR-02, BR-33) + trạng thái vòng đời chi nhánh (BR-08).
-- Căn cứ: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md
-- Idempotent: YES (CREATE IF NOT EXISTS + add_col_if_missing + INSERT IGNORE).
-- Cần 0000_helpers.sql (add_col_if_missing / add_index_if_missing) đã chạy trước.
-- ============================================================================
SET NAMES utf8mb4;

-- --- Bảng nhóm chi nhánh (khu vực) ------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_sys_branch_groups` (
    `id`           INT           NOT NULL AUTO_INCREMENT,
    `tenant_id`    INT           NOT NULL,
    `code`         VARCHAR(50)   NOT NULL,
    `name`         VARCHAR(255)  NOT NULL,
    `type`         VARCHAR(20)   NOT NULL DEFAULT 'REGION' COMMENT 'Chỉ REGION (Q3=Không dùng HOSPITAL)',
    `sort_order`   INT           NOT NULL DEFAULT 0,
    `is_active`    TINYINT(1)    NOT NULL DEFAULT 1,
    `created_at`   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   INT           NULL,
    `updated_at`   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`   INT           NULL,
    `deleted_at`   DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_branch_group_code` (`tenant_id`, `code`),
    INDEX `idx_branch_group_tenant` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nhom chi nhanh theo khu vuc (REGION) - BR-02/BR-03';

-- --- Gắn group_id + status vòng đời vào chi nhánh ---------------------------
CALL add_col_if_missing('diab_his_sys_branches', 'group_id', 'INT NULL COMMENT "FK diab_his_sys_branch_groups"');
CALL add_col_if_missing('diab_his_sys_branches', 'status',
     "VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' COMMENT 'DRAFT|CONFIGURING|READY_CHECK|ACTIVE|SUSPENDED|CLOSED (BR-08/BR-110)'");
CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_group', '(`tenant_id`, `group_id`)');
CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_status', '(`tenant_id`, `status`)');

-- Chi nhánh cũ đang is_active=1 nhưng chưa có status -> coi như ACTIVE (đã set default).
-- Chi nhánh is_active=0 -> SUSPENDED để phản ánh đúng (không tự CLOSED để tránh mất truy cập).
UPDATE diab_his_sys_branches SET status = 'SUSPENDED'
 WHERE is_active = 0 AND status = 'ACTIVE';

-- --- Quyền branch.group_view (giám đốc khu vực - Q9=Có) ---------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'branch.group_view', 'branch', 'group_view',
       'Xem du lieu tat ca chi nhanh trong cung nhom/khu vuc', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'branch.group_view');

-- Mặc định cấp cho admin (super admin bypass sẵn) + ke_toan (tổng hợp khu vực).
DROP PROCEDURE IF EXISTS _grant_group_view;
DELIMITER $$
CREATE PROCEDURE _grant_group_view(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'branch.group_view' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_group_view('admin');
CALL _grant_group_view('ke_toan');
DROP PROCEDURE IF EXISTS _grant_group_view;
