-- Sprint 10 / EPIC 8: VAPID keys + Notification tables
-- MySQL 8

CREATE TABLE IF NOT EXISTS diab_his_nti_vapid_keys (
    id                      BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id               INT             NOT NULL,
    public_key              VARCHAR(255)    NOT NULL COMMENT 'ECDSA P-256 public key base64url',
    private_key_encrypted   VARBINARY(512)  NOT NULL COMMENT 'AES-256-GCM encrypted private key',
    created_at              DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at              DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_vapid_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_nti_notifications (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    user_id         BINARY(16)      NOT NULL,
    type            VARCHAR(100)    NOT NULL,
    title           VARCHAR(300)    NOT NULL,
    body            TEXT            NOT NULL,
    data_json       JSON            NULL,
    read_at         DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_nti_user (tenant_id, user_id, created_at DESC),
    INDEX idx_nti_unread (tenant_id, user_id, read_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_nti_web_push_subs (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    user_id         BINARY(16)      NOT NULL,
    endpoint        VARCHAR(1000)   NOT NULL,
    p256dh_key      VARCHAR(200)    NOT NULL,
    auth_key        VARCHAR(100)    NOT NULL,
    user_agent      VARCHAR(500)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_push_endpoint (endpoint(200)),
    INDEX idx_push_user (tenant_id, user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_nti_preferences (
    id                  BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id           INT             NOT NULL,
    user_id             BINARY(16)      NOT NULL,
    position            VARCHAR(20)     NOT NULL DEFAULT 'TOP_RIGHT',
    sound_enabled       TINYINT(1)      NOT NULL DEFAULT 1,
    sound_name          VARCHAR(50)     NOT NULL DEFAULT 'default',
    browser_push_enabled TINYINT(1)     NOT NULL DEFAULT 0,
    types_disabled      JSON            NULL,
    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_pref_user (tenant_id, user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
