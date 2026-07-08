-- ============================================================
-- Migration: 9046_create_cdss_rules_v2
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Bang rule CDSS the he moi (GUID + tenant_id + audit dong bo), tach khoi
--   bang dump cu `cdss_rules` (PK int, UPPER_CASE, khong tenant). Dung cho cac
--   rule DIEU KIEN (drug-allergy, trung hoat chat, drug-lab, critical lab). Cap
--   tuong tac thuoc-thuoc luu rieng o diab_his_cdss_ddi_pairs (9045).
--   definition_json luu theo schema Microsoft.RulesEngine (Workflow/Rules) hoac
--   cau truc dieu kien noi bo tuy rule_type.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + seed WHERE NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cdss_rules (
    id              CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id       INT          NULL COMMENT 'NULL = rule chuan dung chung; INT = rule rieng tenant',
    code            VARCHAR(60)  NOT NULL,
    rule_name       VARCHAR(200) NOT NULL,
    rule_type       VARCHAR(24)  NOT NULL COMMENT 'DRUG_DRUG|DRUG_ALLERGY|DUPLICATE_INGREDIENT|DRUG_LAB|CRITICAL_LAB',
    category        VARCHAR(60)  NULL,
    definition_json JSON         NULL COMMENT 'Dieu kien rule (RulesEngine workflow hoac cau truc noi bo)',
    message_vi      TEXT         NULL COMMENT 'Thong diep canh bao (tieng Viet co dau)',
    management_vi   TEXT         NULL COMMENT 'Khuyen cao xu tri',
    severity        VARCHAR(16)  NOT NULL DEFAULT 'MODERATE' COMMENT 'CONTRAINDICATED|MAJOR|MODERATE|MINOR',
    is_interruptive TINYINT(1)   NOT NULL DEFAULT 0 COMMENT '1 = chan luong ky don khi chua override',
    priority        INT          NOT NULL DEFAULT 100,
    is_active       TINYINT(1)   NOT NULL DEFAULT 1,
    effective_date  DATE         NULL,
    expiration_date DATE         NULL,
    source          VARCHAR(120) NULL,
    created_at      DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by      CHAR(36)     NULL,
    updated_at      DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by      CHAR(36)     NULL,
    deleted_at      DATETIME(3)  NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_cdss_rule_code (tenant_id, code),
    INDEX idx_cdss_rule_type (rule_type, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='CDSS: rule dieu kien (drug-lab, d-allergy, duplicate, critical lab)';

-- ------------------------------------------------------------
-- SEED rule chuan (tenant_id NULL). definition_json mo ta dieu kien de engine
-- danh gia (khong phai LambdaExpression cung, engine tu map theo rule_type +
-- cac truong: ingredient/atc_prefix/lab_code/op/threshold).
-- Idempotent theo (tenant_id NULL, code).
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS seed_cdss_rule;
DELIMITER $$
CREATE PROCEDURE seed_cdss_rule(
    p_code VARCHAR(60), p_name VARCHAR(200), p_type VARCHAR(24), p_cat VARCHAR(60),
    p_def JSON, p_msg TEXT, p_mgmt TEXT, p_sev VARCHAR(16), p_interruptive TINYINT,
    p_priority INT, p_src VARCHAR(120))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM diab_his_cdss_rules WHERE tenant_id IS NULL AND code = p_code
    ) THEN
        INSERT INTO diab_his_cdss_rules
            (id, tenant_id, code, rule_name, rule_type, category, definition_json,
             message_vi, management_vi, severity, is_interruptive, priority, source)
        VALUES
            (UUID(), NULL, p_code, p_name, p_type, p_cat, p_def,
             p_msg, p_mgmt, p_sev, p_interruptive, p_priority, p_src);
    END IF;
END$$
DELIMITER ;

-- Drug-lab: Metformin khi eGFR < 30 (chong chi dinh)
CALL seed_cdss_rule('DRUGLAB_METFORMIN_EGFR30','Metformin khi eGFR < 30','DRUG_LAB','renal',
    JSON_OBJECT('ingredient','metformin','lab_code','EGFR','op','<','threshold',30),
    N'Metformin chong chi dinh khi eGFR < 30 mL/phut/1.73m2 do nguy co nhiem toan lactic.',
    N'Ngung metformin; can nhac nhom thuoc khac phu hop chuc nang than.',
    'CONTRAINDICATED',1,10,'ADA Standards of Care; Duoc thu QG');

-- Drug-lab: Metformin khi eGFR 30-45 (than trong)
CALL seed_cdss_rule('DRUGLAB_METFORMIN_EGFR45','Metformin khi eGFR 30-45','DRUG_LAB','renal',
    JSON_OBJECT('ingredient','metformin','lab_code','EGFR','op','<','threshold',45),
    N'eGFR 30-45: can than trong voi metformin (khong khoi dau moi, can nhac giam lieu).',
    N'Danh gia lai lieu metformin, theo doi chuc nang than moi 3 thang.',
    'MODERATE',0,50,'ADA Standards of Care');

-- Critical lab: Kali mau > 6.0 (nguy hiem)
CALL seed_cdss_rule('CRITLAB_POTASSIUM_HIGH','Tang kali mau nguy hiem','CRITICAL_LAB','critical',
    JSON_OBJECT('lab_code','K','op','>','threshold',6.0),
    N'Kali mau > 6.0 mmol/L: tang kali mau nguy hiem, nguy co roi loan nhip.',
    N'Xu tri cap cuu tang kali mau; ra soat thuoc giu kali (ACEi/ARB/loi tieu giu kali).',
    'MAJOR',1,10,'Nguong critical noi bo');

-- Critical lab: duong huyet qua thap
CALL seed_cdss_rule('CRITLAB_GLUCOSE_LOW','Ha duong huyet nang','CRITICAL_LAB','critical',
    JSON_OBJECT('lab_code','GLUCOSE','op','<','threshold',54),
    N'Duong huyet < 54 mg/dL: ha duong huyet co y nghia lam sang.',
    N'Xu tri ha duong huyet; ra soat lieu insulin/sulfonylurea.',
    'MAJOR',1,10,'ADA Standards of Care');

DROP PROCEDURE IF EXISTS seed_cdss_rule;
