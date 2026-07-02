-- Migration 0061: Feature flags table
-- Sprint 13 — Basic feature flag system

CREATE TABLE IF NOT EXISTS diab_his_sys_feature_flags (
    `key`       VARCHAR(100) NOT NULL,
    enabled     TINYINT(1)   NOT NULL DEFAULT 0,
    description VARCHAR(500) NULL,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Feature flags — doc tu DB, config override uu tien cao hon';

-- Seed initial flags
INSERT IGNORE INTO diab_his_sys_feature_flags (`key`, enabled, description)
VALUES
    ('fhir.export.bundle',  1, 'Cho phep export FHIR Bundle cho encounter'),
    ('fhir.allergy.sync',   1, 'Dong bo di ung vao FHIR AllergyIntolerance'),
    ('ai.diagnosis.suggest',0, 'AI goi y chan doan (Azure OpenAI GPT-4o)'),
    ('bhyt.auto.submit',    0, 'Tu dong gui ho so BHYT sau khi dong ca'),
    ('cashier.einvoice',    1, 'Phat hanh hoa don dien tu sau khi thu tien');
