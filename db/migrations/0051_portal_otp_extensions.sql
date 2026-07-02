-- Sprint 10 / EPIC 8: Patient Portal tables
-- MySQL 8

CREATE TABLE IF NOT EXISTS diab_his_pat_portal_accounts (
    id                  BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id           INT             NOT NULL,
    patient_id          BINARY(16)      NOT NULL,
    phone               VARCHAR(20)     NOT NULL COMMENT 'E.164 format',
    failed_attempts     INT             NOT NULL DEFAULT 0,
    locked_until        DATETIME        NULL,
    last_otp_sent_at    DATETIME        NULL,
    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_portal_phone_tenant (tenant_id, phone),
    INDEX idx_portal_patient (tenant_id, patient_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_pat_portal_otp_log (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    phone           VARCHAR(20)     NOT NULL,
    otp_hash        VARCHAR(100)    NOT NULL COMMENT 'bcrypt hash of 6-digit OTP',
    purpose         ENUM('LOGIN','LOOKUP') NOT NULL DEFAULT 'LOGIN',
    sent_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    verified_at     DATETIME        NULL,
    expires_at      DATETIME        NOT NULL,
    attempts        INT             NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    INDEX idx_otp_phone (tenant_id, phone, sent_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_pat_portal_sessions (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    patient_id      BINARY(16)      NOT NULL,
    jti             VARCHAR(100)    NOT NULL COMMENT 'JWT ID for revocation',
    issued_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at      DATETIME        NOT NULL,
    revoked_at      DATETIME        NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_session_jti (jti),
    INDEX idx_session_patient (tenant_id, patient_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
