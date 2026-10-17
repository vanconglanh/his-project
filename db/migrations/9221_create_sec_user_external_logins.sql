-- =============================================================================
-- 9221_create_sec_user_external_logins.sql
-- SSO OIDC (DiaB IdentityServer) - xem docs/api/sso-design-20261017.md muc 6.3 + 12.2
--
-- 1) Bang lien ket tai khoan ngoai (provider, subject) -> user HIS.
--    PK/user_id/created_by dung CHAR(36) cho nhat quan voi cac bang diab_his_sec_* hien co.
--    Unique (provider, subject, tenant_id) chi khi chua xoa mem (cot sinh active_key).
-- 2) Them sso_sid, sso_sub (NULL) vao diab_his_sec_sessions de back-channel logout CHI thu hoi
--    phien SSO (phien mat khau co sso_sid IS NULL -> khong bi anh huong).
--
-- AN TOAN: chi CREATE TABLE IF NOT EXISTS + ADD COLUMN NULL + ADD INDEX. Khong xoa/doi/truncate
-- bang session/user. Cot nullable nen MySQL 8 them INSTANT/INPLACE, khong khoa bang lau.
-- Idempotent: YES (chay lai khong doi gi). Yeu cau 0000_helpers.sql da nap.
-- =============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_sec_user_external_logins (
  id              CHAR(36)     NOT NULL,
  tenant_id       INT          NOT NULL,
  user_id         CHAR(36)     NOT NULL COMMENT 'diab_his_sec_users.id',
  provider        VARCHAR(50)  NOT NULL COMMENT 'vd diab-is',
  subject         VARCHAR(200) NOT NULL COMMENT 'claim sub tu IdP',
  email_at_link   VARCHAR(255) NULL COMMENT 'email luc lien ket (tham khao)',
  last_login_at   DATETIME     NULL,
  last_sid        VARCHAR(200) NULL,
  created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_by      CHAR(36)     NULL,
  updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  updated_by      CHAR(36)     NULL,
  deleted_at      DATETIME     NULL,
  deleted_by      CHAR(36)     NULL,
  -- MySQL khong co partial index: cot sinh de unique chi khi chua xoa
  active_key      VARCHAR(300) GENERATED ALWAYS AS (IF(deleted_at IS NULL, CONCAT(provider,'|',subject,'|',tenant_id), NULL)) STORED,
  PRIMARY KEY (id),
  UNIQUE KEY uq_ext_login_active (active_key),
  KEY ix_ext_login_tenant_user (tenant_id, user_id),
  KEY ix_ext_login_provider_subject (provider, subject)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CALL add_col_if_missing('diab_his_sec_sessions', 'sso_sid', 'VARCHAR(200) NULL COMMENT ''sid cua phien IdP (SSO) - NULL = phien mat khau''');
CALL add_col_if_missing('diab_his_sec_sessions', 'sso_sub', 'VARCHAR(200) NULL COMMENT ''sub cua IdP (SSO)''');
CALL add_index_if_missing('diab_his_sec_sessions', 'ix_sessions_sso_sid', '(sso_sid)');
CALL add_index_if_missing('diab_his_sec_sessions', 'ix_sessions_sso_sub', '(sso_sub)');

-- Rollback (KHONG khuyen nghi - cot/bang moi nullable, code cu bo qua):
--   DROP TABLE IF EXISTS diab_his_sec_user_external_logins;
--   ALTER TABLE diab_his_sec_sessions DROP COLUMN sso_sid, DROP COLUMN sso_sub;
