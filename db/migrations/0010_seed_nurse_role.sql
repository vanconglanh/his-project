-- ============================================================
-- Migration: 0010_seed_nurse_role
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-19, US-N01
-- Idempotent: YES
-- Ghi chú: Thêm role Điều dưỡng vào sec_roles nếu chưa tồn tại.
--   Dùng ON DUPLICATE KEY UPDATE (no-op) để đảm bảo idempotent.
--   sec_roles dùng cột UPPERCASE: ID, CODE, NAME, DESCRIPTION, ROLE_TYPE,
--   IS_SYSTEM_ROLE, IS_DEFAULT_ROLE, PRIORITY_LEVEL, PHI_ACCESS_LEVEL,
--   CREATED_AT, LAST_UPDATED_AT, STATUS, CREATED_BY, LAST_UPDATED_BY.
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `sec_roles`
    (`CODE`, `NAME`, `DESCRIPTION`, `ROLE_TYPE`, `IS_SYSTEM_ROLE`, `IS_DEFAULT_ROLE`,
     `PRIORITY_LEVEL`, `PHI_ACCESS_LEVEL`, `REQUIRES_MFA`, `APPROVAL_REQUIRED`,
     `CAN_DELEGATE`, `CAN_IMPERSONATE`, `AUDIT_ALL_ACTIONS`,
     `SESSION_CONCURRENT_LIMIT`, `MAX_SESSION_TIME`, `PASSWORD_POLICY`, `STATUS`)
VALUES
    ('DIEUDUONG', 'Điều dưỡng',
     'Nhân viên điều dưỡng: đo dấu hiệu sinh tồn, hỗ trợ bác sĩ, quản lý hồ sơ điều dưỡng',
     'HOSPITAL', 1, 0,
     30, 'PARTIAL', 0, 0,
     0, 0, 1,
     2, 480, 'STANDARD', 1)
ON DUPLICATE KEY UPDATE
    `NAME`        = VALUES(`NAME`),
    `DESCRIPTION` = VALUES(`DESCRIPTION`);
