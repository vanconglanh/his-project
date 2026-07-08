-- ============================================================
-- Catalog (Control Plane) — 001_catalog_init
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Mo ta: Database DIEU KHIEN rieng (prodiab_catalog), TACH khoi cac DB du lieu
--   cua tung phong kham (prodiab_t_<code>). Luu so do map tenant -> database +
--   domain + tien trinh provisioning. Day la he thong co quyen cao nhat (tao DB,
--   grant user) nen phai co cach ly ha tang khoi data plane.
-- Ket noi qua ConnectionStrings:Catalog (khac DefaultConnection cua tung tenant).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

-- ── 1. Tenants (danh muc phong kham o control plane) ────────
-- Nguon su that ve tenant_id (INT) — moi tenant DB chua dung 1 row diab_his_sys_tenants
-- co id = cat_tenants.id nay (defense-in-depth query filter van dung tenant_id).
CREATE TABLE IF NOT EXISTS `cat_tenants` (
    `id`               INT          NOT NULL AUTO_INCREMENT,
    `code`             VARCHAR(20)  NOT NULL COMMENT 'Ma ngan dinh danh tenant (slug), vd ABC',
    `name`             VARCHAR(200) NOT NULL COMMENT 'Ten day du phong kham',
    `subdomain`        VARCHAR(63)  NOT NULL COMMENT 'Subdomain mac dinh, vd abc -> abc.<BaseDomain>',
    `status`           ENUM('provisioning','active','suspended','expired','archived')
                                    NOT NULL DEFAULT 'provisioning',
    `specialty_preset` VARCHAR(50)  NULL COMMENT 'Preset chuyen khoa: default|noi_tiet|da_khoa|san_phu|nhi...',
    `expires_at`       DATETIME     NULL COMMENT 'Ngay het han goi dich vu',
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       INT          NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       INT          NULL,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cat_tenants_code` (`code`),
    UNIQUE KEY `uq_cat_tenants_subdomain` (`subdomain`),
    KEY `idx_cat_tenants_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: danh muc tenant + trang thai vong doi';

-- ── 2. Tenant databases (shard map: tenant -> connection) ───
CREATE TABLE IF NOT EXISTS `cat_tenant_databases` (
    `tenant_id`             INT          NOT NULL,
    `server_host`           VARCHAR(255) NOT NULL DEFAULT 'mysql' COMMENT 'Host MySQL (scale ra nhieu server chi doi cot nay)',
    `server_port`           INT          NOT NULL DEFAULT 3306,
    `db_name`               VARCHAR(64)  NOT NULL COMMENT 'Ten database, vd prodiab_t_abc',
    `db_user`               VARCHAR(64)  NOT NULL COMMENT 'MySQL user rieng, GRANT chi tren db_name',
    `db_password_encrypted` VARBINARY(512) NOT NULL COMMENT 'Mat khau ma hoa AES-256-GCM (IEncryptionService)',
    `schema_version`        VARCHAR(20)  NOT NULL DEFAULT '' COMMENT 'Migration moi nhat da apply cho DB nay',
    `updated_at`            DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`tenant_id`),
    UNIQUE KEY `uq_cat_tenant_db_name` (`db_name`),
    CONSTRAINT `fk_cat_tenant_db_tenant` FOREIGN KEY (`tenant_id`)
        REFERENCES `cat_tenants` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: map tenant -> database/connection (shard map)';

-- ── 3. Tenant domains (subdomain + custom domain) ──────────
CREATE TABLE IF NOT EXISTS `cat_tenant_domains` (
    `id`                  INT          NOT NULL AUTO_INCREMENT,
    `tenant_id`           INT          NOT NULL,
    `domain`              VARCHAR(255) NOT NULL COMMENT 'Ca subdomain.<BaseDomain> lan custom domain (vd phongkham.vn)',
    `type`                ENUM('subdomain','custom') NOT NULL,
    `is_primary`          TINYINT(1)   NOT NULL DEFAULT 0,
    `verification_status` ENUM('pending','verified','failed') NOT NULL DEFAULT 'pending',
    `verification_token`  VARCHAR(64)  NULL COMMENT 'Token de kiem tra TXT record (custom domain)',
    `ssl_status`          ENUM('none','pending','active') NOT NULL DEFAULT 'none',
    `released_at`         DATETIME     NULL COMMENT 'Thoi diem thu hoi (chong takeover) — subdomain khong tai cap 90 ngay',
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cat_tenant_domain` (`domain`),
    KEY `idx_cat_tenant_domains_tenant` (`tenant_id`),
    CONSTRAINT `fk_cat_tenant_domains_tenant` FOREIGN KEY (`tenant_id`)
        REFERENCES `cat_tenants` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: domain routing per tenant (subdomain + custom)';

-- ── 4. Provisioning runs (state machine resumable) ─────────
CREATE TABLE IF NOT EXISTS `cat_provisioning_runs` (
    `id`            INT          NOT NULL AUTO_INCREMENT,
    `tenant_id`     INT          NOT NULL,
    `current_step`  VARCHAR(40)  NOT NULL DEFAULT 'CREATE_DATABASE'
                    COMMENT 'CREATE_DATABASE|CREATE_DB_USER|RUN_MIGRATIONS|SEED_GLOBAL_DICTS|SEED_TENANT_DEFAULTS|CREATE_ADMIN_USER|REGISTER_DOMAIN|SEND_INVITE_EMAIL|COMPLETE',
    `status`        ENUM('running','failed','completed') NOT NULL DEFAULT 'running',
    `error_message` TEXT         NULL,
    `payload`       JSON         NULL COMMENT 'Input onboarding (de resume tu step fail)',
    `started_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `finished_at`   DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_cat_prov_tenant` (`tenant_id`),
    KEY `idx_cat_prov_status` (`status`),
    CONSTRAINT `fk_cat_prov_tenant` FOREIGN KEY (`tenant_id`)
        REFERENCES `cat_tenants` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: tien trinh provisioning tenant (idempotent, resume duoc)';

-- ── 5. Schema migrations tracking (per tenant DB) ──────────
CREATE TABLE IF NOT EXISTS `cat_schema_migrations` (
    `tenant_id`      INT          NOT NULL,
    `migration_file` VARCHAR(255) NOT NULL,
    `checksum`       CHAR(64)     NOT NULL COMMENT 'SHA-256 noi dung file de phat hien drift',
    `applied_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`tenant_id`, `migration_file`),
    CONSTRAINT `fk_cat_schema_mig_tenant` FOREIGN KEY (`tenant_id`)
        REFERENCES `cat_tenants` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: MigrationRunner tracking migration da apply cho tung tenant DB';

-- ── 6. Super-admin users (portal admin.<BaseDomain>) ───────
-- Tach khoi user cua tung tenant: super-admin dang nhap o host co dinh, JWT khong co tenant_id.
CREATE TABLE IF NOT EXISTS `cat_superadmin_users` (
    `id`            CHAR(36)     NOT NULL DEFAULT (UUID()),
    `email`         VARCHAR(255) NOT NULL,
    `full_name`     VARCHAR(255) NOT NULL,
    `password_hash` VARCHAR(500) NOT NULL,
    `status`        ENUM('active','disabled') NOT NULL DEFAULT 'active',
    `totp_secret`   VARCHAR(255) NULL,
    `created_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cat_superadmin_email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Control plane: tai khoan super-admin quan ly toan bo tenant';
