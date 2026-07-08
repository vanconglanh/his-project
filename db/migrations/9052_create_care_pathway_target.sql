-- ============================================================
-- Migration: 9052_create_care_pathway_target
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Nguong muc tieu dieu tri theo phac do (care pathway). Dung CHUNG cho:
--   - Dashboard trend (ve duong target overlay)
--   - Risk stratification (so chi so vs target)
--   - Recall job (khoang tai kham / khoang HbA1c)
--   - AI reasoner (suy khuyen nghi theo guideline)
--   code = ma phac do (vd DM_T2_5481 = DTD tip 2 theo QD 5481/QD-BYT).
--   tenant_id NULL = phac do chuan; tenant co the override.
-- Nguon: QD 5481/QD-BYT (2020) + ADA Standards of Care.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + seed WHERE NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_care_pathway_target (
    id           CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id    INT          NULL COMMENT 'NULL = phac do chuan; INT = override tenant',
    code         VARCHAR(40)  NOT NULL COMMENT 'Ma phac do (vd DM_T2_5481)',
    param        VARCHAR(30)  NOT NULL COMMENT 'HBA1C|BP_SYS|BP_DIA|LDL|EGFR|VISIT_INTERVAL_DAYS|HBA1C_INTERVAL_DAYS',
    target_op    VARCHAR(4)   NOT NULL COMMENT '<|<=|>|>=|=',
    target_value DECIMAL(10,2) NOT NULL,
    unit         VARCHAR(20)  NULL,
    note         VARCHAR(255) NULL,
    created_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uk_pathway_param (tenant_id, code, param),
    INDEX idx_pathway_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nguong muc tieu dieu tri theo phac do (care pathway)';

-- SEED phac do chuan DTD tip 2 (DM_T2_5481). Idempotent theo (NULL, code, param).
DROP PROCEDURE IF EXISTS seed_pathway_target;
DELIMITER $$
CREATE PROCEDURE seed_pathway_target(
    p_code VARCHAR(40), p_param VARCHAR(30), p_op VARCHAR(4),
    p_val DECIMAL(10,2), p_unit VARCHAR(20), p_note VARCHAR(255))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM diab_his_cli_care_pathway_target
        WHERE tenant_id IS NULL AND code = p_code AND param = p_param
    ) THEN
        INSERT INTO diab_his_cli_care_pathway_target
            (id, tenant_id, code, param, target_op, target_value, unit, note)
        VALUES (UUID(), NULL, p_code, p_param, p_op, p_val, p_unit, p_note);
    END IF;
END$$
DELIMITER ;

CALL seed_pathway_target('DM_T2_5481','HBA1C','<',7.0,'%',
    N'Muc tieu HbA1c chung nguoi lon (ca the hoa: nguoi tre <6.5, nguoi gia/nhieu benh nen 7-8)');
CALL seed_pathway_target('DM_T2_5481','BP_SYS','<',130,'mmHg', N'Huyet ap tam thu muc tieu');
CALL seed_pathway_target('DM_T2_5481','BP_DIA','<',80,'mmHg', N'Huyet ap tam truong muc tieu');
CALL seed_pathway_target('DM_T2_5481','LDL','<',2.6,'mmol/L', N'LDL-C muc tieu (nguy co cao); rat cao <1.8');
CALL seed_pathway_target('DM_T2_5481','EGFR','>=',30,'mL/ph/1.73m2', N'Nguong than trong metformin');
CALL seed_pathway_target('DM_T2_5481','VISIT_INTERVAL_DAYS','<=',90,'ngay',
    N'Khoang tai kham toi da khi chua dat muc tieu (3 thang)');
CALL seed_pathway_target('DM_T2_5481','HBA1C_INTERVAL_DAYS','<=',90,'ngay',
    N'Khoang do HbA1c khi chua on dinh (3 thang); on dinh co the 6 thang');

DROP PROCEDURE IF EXISTS seed_pathway_target;
