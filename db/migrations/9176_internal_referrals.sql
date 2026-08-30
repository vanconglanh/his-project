-- ============================================================================
-- 9176_internal_referrals.sql
-- Dot 5: Chuyen co so noi bo / Internal referral giua 2 chi nhanh (BR-29)
-- Can cu: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md muc 2.2 (BR-29).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT IGNORE).
-- Can 0000_helpers.sql da chay truoc.
-- ============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_clinic_internal_referrals` (
    `id`                  INT           NOT NULL AUTO_INCREMENT,
    `tenant_id`           INT           NOT NULL,
    `patient_id`          CHAR(36)      NOT NULL,
    `source_branch_id`    INT           NOT NULL,
    `target_branch_id`    INT           NOT NULL,
    `encounter_id`        CHAR(36)      NULL,
    `referring_doctor_id` CHAR(36)      NULL,
    `reason`              TEXT          NULL,
    `status`              VARCHAR(20)   NOT NULL DEFAULT 'SENT' COMMENT 'SENT|ACCEPTED|COMPLETED|CANCELLED',
    `note`                TEXT          NULL,
    `created_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`          CHAR(36)      NULL,
    `updated_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`          CHAR(36)      NULL,
    `deleted_at`          DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_referral_target_status` (`tenant_id`, `target_branch_id`, `status`),
    INDEX `idx_referral_patient` (`tenant_id`, `patient_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chuyen co so noi bo giua 2 chi nhanh cung tenant - BR-29';

-- --- Quyen internal_referral.read / internal_referral.write -----------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'internal_referral', SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'internal_referral.read' AS code, 'Xem danh sach chuyen co so noi bo giua cac chi nhanh - BR-29' AS descr
    UNION ALL
    SELECT 'internal_referral.write', 'Tao/cap nhat trang thai chuyen co so noi bo giua cac chi nhanh - BR-29'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

DROP PROCEDURE IF EXISTS _grant_internal_referral_perm;
DELIMITER $$
CREATE PROCEDURE _grant_internal_referral_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_internal_referral_perm('admin', 'internal_referral.read');
CALL _grant_internal_referral_perm('admin', 'internal_referral.write');
CALL _grant_internal_referral_perm('bac_si', 'internal_referral.read');
CALL _grant_internal_referral_perm('bac_si', 'internal_referral.write');
CALL _grant_internal_referral_perm('le_tan', 'internal_referral.read');
CALL _grant_internal_referral_perm('le_tan', 'internal_referral.write');
DROP PROCEDURE IF EXISTS _grant_internal_referral_perm;
