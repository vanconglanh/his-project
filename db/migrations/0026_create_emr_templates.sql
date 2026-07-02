-- ============================================================
-- Migration: 0026_create_emr_templates
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-SUNS-06, US-SUNS-07
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_emr_templates (
    id          CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id   INT          NULL     COMMENT 'NULL = system template',
    name        VARCHAR(200) NOT NULL,
    content_json LONGTEXT    NOT NULL COMMENT 'Tiptap JSON',
    speciality  VARCHAR(50)  NOT NULL DEFAULT 'GENERAL'
                COMMENT 'GENERAL|DIABETES|CARDIOLOGY|ENDOCRINOLOGY|NEPHROLOGY|OPHTHALMOLOGY|OTHER',
    is_system   TINYINT(1)   NOT NULL DEFAULT 0,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by  CHAR(36)     NULL,
    updated_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by  CHAR(36)     NULL,
    deleted_at  DATETIME     NULL,
    PRIMARY KEY (id),
    INDEX idx_emr_tpl_tenant (tenant_id),
    INDEX idx_emr_tpl_spec   (speciality)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='EMR templates (system + per-tenant custom)';

-- Seed 2 system templates nếu chưa có
INSERT IGNORE INTO diab_his_cli_emr_templates
    (id, tenant_id, name, content_json, speciality, is_system, created_at, updated_at)
VALUES
(
    'aaaaaaaa-0001-0000-0000-000000000001',
    NULL,
    'Mẫu bệnh án tổng quát',
    '{"type":"doc","content":[{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Lý do khám"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Tiền sử"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Khám lâm sàng"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Cận lâm sàng"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Chẩn đoán"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Hướng xử trí"}]},{"type":"paragraph"}]}',
    'GENERAL',
    1,
    NOW(),
    NOW()
),
(
    'aaaaaaaa-0002-0000-0000-000000000002',
    NULL,
    'Mẫu bệnh án đái tháo đường',
    '{"type":"doc","content":[{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Lý do khám"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Tiền sử đái tháo đường"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Khám lâm sàng"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Chẩn đoán & biến chứng"}]},{"type":"paragraph"},{"type":"heading","attrs":{"level":2},"content":[{"type":"text","text":"Mục tiêu điều trị & hướng xử trí"}]},{"type":"paragraph"}]}',
    'DIABETES',
    1,
    NOW(),
    NOW()
);
