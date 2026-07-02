-- Migration 0060: Seed permissions Sprint 13 — fhir.read
-- Sprint 13 — FHIR R4 access permission

INSERT IGNORE INTO diab_his_permissions (`key`, description, module, created_at, updated_at)
VALUES
    ('fhir.read', 'Truy cap FHIR R4 endpoint de doc du lieu benh nhan/luot kham/don thuoc', 'FHIR', NOW(), NOW());

-- Gan fhir.read cho role ADMIN va BACSI
INSERT IGNORE INTO diab_his_role_permissions (role_id, permission_key, created_at)
SELECT r.id, 'fhir.read', NOW()
FROM diab_his_roles r
WHERE r.code IN ('ADMIN', 'BACSI');
