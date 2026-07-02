-- Sprint 10 / EPIC 8: API Partner tables + scope dictionary seed
-- MySQL 8

CREATE TABLE IF NOT EXISTS diab_his_api_partners (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    name            VARCHAR(200)    NOT NULL,
    contact_email   VARCHAR(255)    NULL,
    api_key_hash    VARCHAR(64)     NOT NULL COMMENT 'SHA-256 hex of raw key',
    api_key_prefix  VARCHAR(20)     NOT NULL COMMENT 'pdh_live_****xxxx for display',
    scopes          JSON            NOT NULL DEFAULT ('[]'),
    rate_limit_per_min INT          NOT NULL DEFAULT 60,
    daily_quota     INT             NOT NULL DEFAULT 10000,
    status          ENUM('ACTIVE','DISABLED','EXPIRED') NOT NULL DEFAULT 'ACTIVE',
    expires_at      DATETIME        NULL,
    ip_whitelist    JSON            NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BINARY(16)      NULL,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by      BINARY(16)      NULL,
    deleted_at      DATETIME        NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_api_key_hash (api_key_hash),
    INDEX idx_partners_tenant (tenant_id),
    INDEX idx_partners_status (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_api_request_logs (
    id              BINARY(16)      NOT NULL DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id       INT             NOT NULL,
    partner_id      BINARY(16)      NOT NULL,
    method          VARCHAR(10)     NOT NULL,
    path            VARCHAR(500)    NOT NULL,
    status_code     SMALLINT        NOT NULL,
    duration_ms     INT             NOT NULL DEFAULT 0,
    ip              VARCHAR(45)     NULL,
    error_code      VARCHAR(100)    NULL,
    called_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_req_logs_partner (partner_id, called_at),
    INDEX idx_req_logs_tenant (tenant_id, called_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS diab_his_api_scope_dict (
    code            VARCHAR(100)    NOT NULL,
    description_vi  VARCHAR(300)    NOT NULL,
    PRIMARY KEY (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO diab_his_api_scope_dict (code, description_vi) VALUES
    ('public.patient.read',       'Xem thông tin bệnh nhân qua Public API'),
    ('public.patient.write',      'Tạo / cập nhật bệnh nhân qua Public API'),
    ('public.appointment.read',   'Xem lịch hẹn qua Public API'),
    ('public.appointment.write',  'Đặt / hủy lịch hẹn qua Public API'),
    ('public.catalog.read',       'Xem danh mục dịch vụ, bác sĩ qua Public API'),
    ('public.visit.lookup',       'Tra cứu lịch sử khám qua OTP bệnh nhân');
