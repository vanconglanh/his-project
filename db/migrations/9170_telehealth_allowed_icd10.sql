-- ============================================================================
-- 9170_telehealth_allowed_icd10.sql
-- H-8 (FR-804): Danh mục ICD-10 được phép tư vấn từ xa (configurable theo tenant,
--   KHONG hardcode trong code). Dung de:
--   - Canh bao mem khi dat lich telehealth (CreateTelehealthAppointmentCommand) neu
--     benh nhan da co chan doan ngoai danh muc (thuong chua co o buoc dat lich).
--   - Chan cung khi ke don telehealth (CreatePrescriptionCommand, IsTelehealthContext=true)
--     neu chan doan chinh cua encounter khong nam trong danh muc dang active.
-- Idempotent: CREATE TABLE IF NOT EXISTS + INSERT IGNORE. Can 0000_helpers.sql.
-- ============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_tel_allowed_icd10` (
    `id`          CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`   INT           NOT NULL COMMENT 'ID tenant (moi tenant tu cau hinh danh muc rieng)',
    `icd10_code`  VARCHAR(10)   NOT NULL COMMENT 'Ma ICD-10 duoc phep tu van tu xa',
    `icd10_name`  VARCHAR(255)  NOT NULL COMMENT 'Ten chan doan tieng Viet',
    `is_active`   TINYINT(1)    NOT NULL DEFAULT 1,
    `note`        VARCHAR(500)  NULL,
    `created_at`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`  CHAR(36)      NULL,
    `updated_at`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`  CHAR(36)      NULL,
    `deleted_at`  DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_tel_icd10_tenant_code` (`tenant_id`, `icd10_code`),
    KEY `idx_tel_icd10_tenant_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='FR-804: Danh muc ICD-10 duoc phep tu van tu xa (configurable theo tenant)';

-- --- Seed vai ma pho bien cho tat ca tenant hien co (BO co the sua/xoa qua Admin API) --------
DROP PROCEDURE IF EXISTS _seed_tel_allowed_icd10;
DELIMITER $$
CREATE PROCEDURE _seed_tel_allowed_icd10()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
             WHERE table_schema=DATABASE() AND table_name='diab_his_sys_tenants') THEN
    INSERT IGNORE INTO diab_his_tel_allowed_icd10 (id, tenant_id, icd10_code, icd10_name, is_active, note, created_at, updated_at)
    SELECT UUID(), t.id, x.code, x.name, 1, 'Seed mac dinh 9170 - co the chinh qua Admin API', NOW(), NOW()
    FROM diab_his_sys_tenants t
    CROSS JOIN (
        SELECT 'E11' AS code, N'Đái tháo đường type 2' AS name UNION ALL
        SELECT 'E10', N'Đái tháo đường type 1'          UNION ALL
        SELECT 'I10', N'Tăng huyết áp'                   UNION ALL
        SELECT 'E78', N'Rối loạn chuyển hóa lipoprotein và tình trạng nhiễm lipid huyết khác' UNION ALL
        SELECT 'E03', N'Suy giáp khác'                   UNION ALL
        SELECT 'E66', N'Béo phì'
    ) AS x
    WHERE t.deleted_at IS NULL;
  END IF;
END$$
DELIMITER ;
DROP PROCEDURE IF EXISTS _try_seed_tel_allowed_icd10;
DELIMITER $$
CREATE PROCEDURE _try_seed_tel_allowed_icd10()
BEGIN
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
    SELECT 'CANH BAO 9170: bo qua seed danh muc ICD-10 telehealth do cau truc sys_tenants khac - cau hinh tay' AS warn;
  CALL _seed_tel_allowed_icd10();
END$$
DELIMITER ;
CALL _try_seed_tel_allowed_icd10();
DROP PROCEDURE IF EXISTS _seed_tel_allowed_icd10;
DROP PROCEDURE IF EXISTS _try_seed_tel_allowed_icd10;

-- --- Quyen quan tri danh muc ICD-10 telehealth --------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'telehealth_icd10', SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'telehealth.icd10_read'   AS code, 'Xem danh muc ICD-10 duoc phep tu van tu xa'  AS descr UNION ALL
    SELECT 'telehealth.icd10_manage','Quan tri danh muc ICD-10 duoc phep tu van tu xa'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

DROP PROCEDURE IF EXISTS _grant_tel_icd10_perm;
DELIMITER $$
CREATE PROCEDURE _grant_tel_icd10_perm(IN p_role VARCHAR(50), IN p_perm VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36); DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id=v_role_id AND permission_id=v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_tel_icd10_perm('admin', 'telehealth.icd10_read');
CALL _grant_tel_icd10_perm('admin', 'telehealth.icd10_manage');
CALL _grant_tel_icd10_perm('bac_si', 'telehealth.icd10_read');
DROP PROCEDURE IF EXISTS _grant_tel_icd10_perm;
