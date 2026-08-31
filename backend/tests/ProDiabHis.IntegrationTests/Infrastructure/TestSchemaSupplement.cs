namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// DDL bo sung cho DB test: cac bang CO THAT trong he thong nhung KHONG co entity EF
/// (chi duoc tao boi db/migrations/*.sql) nen EnsureCreated() khong tao ra.
/// DDL duoi day COPY NGUYEN VAN tu db/migrations — khong bia cot.
///
/// Luu y ve thu tu: diab_his_pkg_service_packages phai chay TRUOC
/// diab_his_pkg_subscriptions (FK fk_sub_package).
/// Hai VIEW pat_patients / sec_users tro sang bang co entity EF
/// (PatientConfiguration -> diab_his_pat_patients, UserConfiguration -> diab_his_sec_users)
/// nen EnsureCreated() da tao san bang goc -> view tao duoc.
/// </summary>
public static class TestSchemaSupplement
{
    /// <summary>Moi phan tu la 1 cau lenh DDL doc lap (chay tuan tu, bo qua loi tung cau).</summary>
    public static readonly string[] Statements =
    {
        // ------------------------------------------------------------------
        // Nguon: db/migrations/0018_seed_master_data.sql (muc 3)
        // + cot is_billable them boi db/migrations/9061_fix_legacy_views_icd10_billable.sql
        //   (Icd10Handlers SELECT is_billable) -> gop inline vao CREATE TABLE.
        // (0028_seed_icd10.sql cung co CREATE TABLE IF NOT EXISTS nhung chay SAU 0018
        //  nen la no-op tren DB that; lay shape cua 0018 lam chuan.)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_dict_icd10` (
    `id`          INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khoa chinh tu tang',
    `code`        VARCHAR(10)  NOT NULL UNIQUE                      COMMENT 'Ma ICD-10 (vd: E11, E11.9)',
    `name_vi`     VARCHAR(500) NOT NULL                             COMMENT 'Ten benh tieng Viet',
    `name_en`     VARCHAR(500) NULL                                 COMMENT 'Ten benh tieng Anh',
    `parent_code` VARCHAR(10)  NULL                                 COMMENT 'Ma cha (vd: E11 la cha cua E11.9)',
    `category`    VARCHAR(50)  NULL                                 COMMENT 'Nhom benh (Endocrine, v.v.)',
    `is_active`   TINYINT(1)   NOT NULL DEFAULT 1                   COMMENT 'Con su dung trong he thong',
    `is_billable` TINYINT(1)   NOT NULL DEFAULT 1                   COMMENT 'Them boi 9061_fix_legacy_views_icd10_billable.sql',
    `created_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP   COMMENT 'Thoi diem tao',

    INDEX `idx_icd10_parent` (`parent_code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh muc ICD-10'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9006_create_clinic.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_sys_rooms` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khoa chinh',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `branch_id`     INT             NULL                                COMMENT 'ID chi nhanh (NULL = phong kham chinh)',
    `code`          VARCHAR(20)     NOT NULL                            COMMENT 'Ma phong',
    `name`          VARCHAR(100)    NOT NULL                            COMMENT 'Ten phong',
    `room_type`     VARCHAR(30)     NOT NULL DEFAULT 'EXAM'             COMMENT 'EXAM, LAB, RADIOLOGY, WAITING, CASHIER...',
    `floor`         VARCHAR(10)     NULL                                COMMENT 'Tang',
    `capacity`      INT             NOT NULL DEFAULT 1                  COMMENT 'So benh nhan toi da cung luc',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Con su dung',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thoi diem tao',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID nguoi tao',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thoi diem cap nhat',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID nguoi cap nhat',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thoi diem xoa mem',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID nguoi xoa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_rooms_code_tenant`   (`tenant_id`, `code`),
    INDEX `idx_rooms_tenant`            (`tenant_id`, `room_type`),
    INDEX `idx_rooms_branch`            (`tenant_id`, `branch_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phong kham / phong chuc nang'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0022_create_reception_queue.sql (shape day du,
        //   chay truoc nen la ban chuan; 9011_create_missing_tables.sql chi la
        //   fallback rut gon va thanh no-op tren DB that).
        // Sua kieu theo cac migration sau:
        //   - 9023_fix_rcp_queue_tickets_patient_id_type.sql: patient_id INT -> CHAR(36)
        //   - 9012_add_deleted_by_all.sql : them cot deleted_by
        //   - 9084_add_branch_id_columns.sql : them cot branch_id INT NULL
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_rcp_queue_tickets` (
    `id`               CHAR(36)      NOT NULL DEFAULT (UUID()) COMMENT 'PK UUID',
    `tenant_id`        INT           NULL                      COMMENT 'ID tenant so huu ban ghi',
    `branch_id`        INT           NULL                      COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `patient_id`       CHAR(36)      NOT NULL                  COMMENT 'FK -> pat_patients.id (UUID CHAR(36)), theo 9023',
    `room_id`          CHAR(36)      NOT NULL                  COMMENT 'FK -> rooms.id',
    `doctor_id`        CHAR(36)      NULL                      COMMENT 'FK -> sec_users.id (bac si truc)',
    `ticket_no`        VARCHAR(10)   NOT NULL                  COMMENT 'So thu tu trong ngay: 001, 002...',
    `ticket_date`      DATE          NOT NULL                  COMMENT 'Ngay tao ticket (local timezone)',
    `status`           VARCHAR(20)   NOT NULL DEFAULT 'WAITING' COMMENT 'WAITING|CALLED|IN_PROGRESS|DONE|SKIPPED|CANCELLED',
    `priority`         VARCHAR(20)   NOT NULL DEFAULT 'NORMAL'  COMMENT 'NORMAL|PRIORITY|EMERGENCY',
    `reason_for_visit` VARCHAR(1000) NULL                       COMMENT 'Ly do den kham',
    `note`             VARCHAR(1000) NULL                       COMMENT 'Ghi chu cua le tan',
    `cancel_reason`    VARCHAR(500)  NULL                       COMMENT 'Ly do huy (neu status=CANCELLED)',
    `service_packages` JSON          NULL                       COMMENT 'Danh sach goi dich vu JSON',
    `checked_in_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thoi diem check-in',
    `called_at`        DATETIME      NULL                       COMMENT 'Thoi diem goi BN vao phong',
    `started_at`       DATETIME      NULL                       COMMENT 'Thoi diem bat dau kham',
    `finished_at`      DATETIME      NULL                       COMMENT 'Thoi diem ket thuc kham',
    `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)      NULL,
    `updated_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)      NULL,
    `deleted_at`       DATETIME      NULL,
    `deleted_by`       CHAR(36)      NULL,

    PRIMARY KEY (`id`),
    UNIQUE KEY `UK_TICKET_ROOM_DATE_NO` (`tenant_id`, `room_id`, `ticket_date`, `ticket_no`),
    INDEX `idx_rcp_ticket_tenant_date`  (`tenant_id`, `ticket_date`),
    INDEX `idx_rcp_ticket_patient`      (`tenant_id`, `patient_id`),
    INDEX `idx_rcp_ticket_room_status`  (`tenant_id`, `room_id`, `status`, `ticket_date`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Hang doi tiep don benh nhan'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9092_create_pkg_tables.sql (muc 1)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pkg_service_packages` (
    `id`                    CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT           NOT NULL,
    `code`                  VARCHAR(50)   NOT NULL,
    `name`                  VARCHAR(255)  NOT NULL,
    `description`           TEXT          NULL,
    `duration_days`         INT           NOT NULL DEFAULT 365,
    `list_price`            DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `vat_rate`              TINYINT       NOT NULL DEFAULT 0,
    `min_deposit_percent`   DECIMAL(5,2)  NULL,
    `is_active`             TINYINT(1)    NOT NULL DEFAULT 1,
    `valid_from`            DATE          NULL,
    `valid_to`              DATE          NULL,
    `created_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)      NULL,
    `updated_at`            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)      NULL,
    `deleted_at`            DATETIME      NULL,
    `deleted_by`            CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pkg_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_pkg_tenant_active` (`tenant_id`, `is_active`, `deleted_at`),
    CONSTRAINT `chk_pkg_duration_positive` CHECK (`duration_days` > 0),
    CONSTRAINT `chk_pkg_deposit_range` CHECK (`min_deposit_percent` IS NULL OR (`min_deposit_percent` BETWEEN 0 AND 100))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Template goi dinh muc tra truoc (FR-1201)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9092_create_pkg_tables.sql (muc 3)
        // Phu thuoc FK -> diab_his_pkg_service_packages (phai chay sau).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pkg_subscriptions` (
    `id`                        CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT           NOT NULL,
    `branch_id`                 INT           NULL,
    `patient_id`                CHAR(36)      NOT NULL,
    `package_id`                CHAR(36)      NOT NULL,
    `subscription_no`           VARCHAR(30)   NOT NULL,
    `package_code_snapshot`     VARCHAR(50)   NOT NULL,
    `package_name_snapshot`     VARCHAR(255)  NOT NULL,
    `purchase_date`             DATE          NOT NULL,
    `effective_date`            DATE          NOT NULL,
    `expiry_date`               DATE          NOT NULL,
    `duration_days_snapshot`    INT           NOT NULL,
    `total_price`               DECIMAL(15,2) NOT NULL,
    `amount_paid`               DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `amount_due`                DECIMAL(15,2) GENERATED ALWAYS AS (`total_price` - `amount_paid`) STORED,
    `payment_status`            ENUM('unpaid','deposit_paid','paid_full','refunded') NOT NULL DEFAULT 'unpaid',
    `status`                    ENUM('pending_payment','active','suspended','expired','exhausted','cancelled') NOT NULL DEFAULT 'pending_payment',
    `activated_at`              DATETIME(3)   NULL,
    `suspended_at`              DATETIME(3)   NULL,
    `suspend_reason`            VARCHAR(255)  NULL,
    `cancelled_at`              DATETIME(3)   NULL,
    `cancel_reason`             VARCHAR(255)  NULL,
    `refunded_amount`           DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `expiry_reminded_at`        DATETIME(3)   NULL,
    `overdue_alerted_at`        DATETIME(3)   NULL,
    `note`                      TEXT          NULL,
    `created_at`                DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)      NULL,
    `updated_at`                DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`                CHAR(36)      NULL,
    `deleted_at`                DATETIME      NULL,
    `deleted_by`                CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_sub_tenant_no` (`tenant_id`, `subscription_no`),
    INDEX `idx_sub_patient_active` (`tenant_id`, `patient_id`, `status`, `expiry_date`),
    INDEX `idx_sub_tenant_branch` (`tenant_id`, `branch_id`),
    INDEX `idx_sub_expiry` (`tenant_id`, `status`, `expiry_date`),
    INDEX `idx_sub_debt` (`tenant_id`, `payment_status`, `amount_due`),
    INDEX `idx_sub_package` (`tenant_id`, `package_id`),
    CONSTRAINT `fk_sub_package` FOREIGN KEY (`package_id`) REFERENCES `diab_his_pkg_service_packages` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Benh nhan mua goi dinh muc (FR-1202)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9174_inter_branch_debts.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_bil_inter_branch_debts` (
    `id`                  CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT           NOT NULL,
    `debtor_branch_id`    INT           NOT NULL COMMENT 'Chi nhanh no',
    `creditor_branch_id`  INT           NOT NULL COMMENT 'Chi nhanh duoc no',
    `amount`              DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `source_type`         VARCHAR(30)   NOT NULL COMMENT 'CROSS_BRANCH_PAYMENT|STOCK_TRANSFER',
    `source_ref_id`       CHAR(36)      NULL,
    `source_ref_code`     VARCHAR(50)   NULL,
    `status`              VARCHAR(20)   NOT NULL DEFAULT 'OPEN' COMMENT 'OPEN|SETTLED',
    `note`                VARCHAR(500)  NULL,
    `settled_at`          DATETIME      NULL,
    `settled_by`          CHAR(36)      NULL,
    `created_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`          CHAR(36)      NULL,
    `updated_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`          CHAR(36)      NULL,
    `deleted_at`          DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_ibd_debtor`   (`tenant_id`, `debtor_branch_id`),
    INDEX `idx_ibd_creditor` (`tenant_id`, `creditor_branch_id`),
    INDEX `idx_ibd_status`   (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Cong no noi bo giua cac chi nhanh (BR-84/85/87)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9009_create_legacy_views.sql
        // pat_patients / sec_users la VIEW alias (KHONG phai bang) tro sang
        // bang co prefix — hai bang goc deu co entity EF nen EnsureCreated()
        // da tao san.
        // (db/diab_his_pat_patients.sql va db/diab_his_sec_users.sql la dump
        //  legacy cua he thong tham chieu cu, KHONG dung cho schema hien tai.)
        // ------------------------------------------------------------------
        @"CREATE OR REPLACE VIEW pat_patients AS SELECT * FROM diab_his_pat_patients",
        @"CREATE OR REPLACE VIEW sec_users    AS SELECT * FROM diab_his_sec_users",

        // ==================================================================
        // ===== BO SUNG DOT 2 — cac bang con thieu theo log chay test =====
        // Ghi chu chung:
        //   - Cac FK tro sang bang KHONG chac ton tai da duoc BO (giu lai cot).
        //   - Cot them boi migration sau da duoc GOP INLINE vao CREATE TABLE.
        // ==================================================================

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0031_create_lab_rad_orders.sql
        // + 3 cot reference_range_low/high, unit them boi 0033_lab_partners_seed_dict.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_dict_lab_tests (
    code          VARCHAR(50)  NOT NULL,
    name          VARCHAR(300) NOT NULL,
    sample_type   VARCHAR(100) NULL,
    default_price DECIMAL(12,2) NULL,
    bhyt_price    DECIMAL(12,2) NULL,
    is_active     TINYINT(1)   NOT NULL DEFAULT 1,
    reference_range_low  DECIMAL(18,4) NULL COMMENT 'Them boi 0033_lab_partners_seed_dict.sql',
    reference_range_high DECIMAL(18,4) NULL COMMENT 'Them boi 0033_lab_partners_seed_dict.sql',
    unit                 VARCHAR(32)   NULL COMMENT 'Them boi 0033_lab_partners_seed_dict.sql',
    PRIMARY KEY (code),
    FULLTEXT INDEX ft_lab_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Lab test catalog'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9046_create_cdss_rules_v2.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_cdss_rules (
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
  COMMENT='CDSS: rule dieu kien (drug-lab, d-allergy, duplicate, critical lab)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9052_create_care_pathway_target.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_cli_care_pathway_target (
    id           CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id    INT          NULL COMMENT 'NULL = phac do chuan; INT = override tenant',
    code         VARCHAR(40)  NOT NULL COMMENT 'Ma phac do (vd DM_T2_5481)',
    param        VARCHAR(30)  NOT NULL COMMENT 'HBA1C|BP_SYS|BP_DIA|LDL|EGFR|VISIT_INTERVAL_DAYS|HBA1C_INTERVAL_DAYS',
    target_op    VARCHAR(4)   NOT NULL COMMENT '<|<=|>|>=|=',
    target_value DECIMAL(10,2) NOT NULL,
    unit         VARCHAR(20)  NULL,
    note         VARCHAR(255) NULL,
    created_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at   DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uk_pathway_param (tenant_id, code, param),
    INDEX idx_pathway_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nguong muc tieu dieu tri theo phac do (care pathway)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9051_create_followup_recall.sql
        // + 3 cot notified_at/notify_channel/notify_status them boi
        //   db/migrations/9074_recall_notify_tracking.sql -> gop inline.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_cli_followup_recall (
    id            CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id     INT          NOT NULL,
    patient_id    CHAR(36)     NOT NULL,
    recall_type   VARCHAR(24)  NOT NULL COMMENT 'OVERDUE_VISIT|OVERDUE_HBA1C|RISK_ESCALATION',
    due_date      DATE         NULL,
    reason_json   JSON         NULL,
    priority      VARCHAR(10)  NOT NULL DEFAULT 'NORMAL' COMMENT 'HIGH|NORMAL',
    status        VARCHAR(12)  NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|CONTACTED|SCHEDULED|DONE|DISMISSED',
    channel       VARCHAR(12)  NULL COMMENT 'SMS|WEBPUSH|PHONE|ZALO',
    note          TEXT         NULL,
    contacted_at  DATETIME(3)  NULL,
    contacted_by  CHAR(36)     NULL,
    notified_at    DATETIME    NULL COMMENT 'Them boi 9074_recall_notify_tracking.sql',
    notify_channel VARCHAR(12) NULL COMMENT 'Them boi 9074_recall_notify_tracking.sql',
    notify_status  VARCHAR(12) NULL COMMENT 'Them boi 9074_recall_notify_tracking.sql',
    created_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    deleted_at    DATETIME(3)  NULL,
    PRIMARY KEY (id),
    INDEX idx_recall_worklist (tenant_id, status, due_date),
    INDEX idx_recall_patient (tenant_id, patient_id, recall_type, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Recall/nhac tai kham chu dong theo nguy co lam sang'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9050_create_patient_risk_flag.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_cli_patient_risk_flag (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NOT NULL,
    patient_id     CHAR(36)     NOT NULL,
    risk_level     VARCHAR(10)  NOT NULL DEFAULT 'LOW' COMMENT 'HIGH|MEDIUM|LOW',
    risk_score     DECIMAL(6,2) NOT NULL DEFAULT 0,
    reasons_json   JSON         NULL COMMENT 'Mang ly do: HbA1c cao, eGFR thap, HA cao, qua han...',
    latest_hba1c   DECIMAL(5,2) NULL,
    latest_egfr    DECIMAL(8,2) NULL,
    latest_bp_sys  INT          NULL,
    latest_bp_dia  INT          NULL,
    hba1c_trend    VARCHAR(12)  NULL COMMENT 'RISING|STABLE|FALLING',
    last_visit_at  DATETIME     NULL,
    last_hba1c_at  DATETIME     NULL,
    computed_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uk_risk_patient (tenant_id, patient_id),
    INDEX idx_risk_level (tenant_id, risk_level),
    INDEX idx_risk_score (tenant_id, risk_score)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phan tang nguy co benh nhan (job upsert, phuc vu risk-list)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9143_create_cls_order_rounds.sql (muc 1)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_cls_order_rounds` (
    `id`             CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT           NOT NULL,
    `encounter_id`   CHAR(36)      NOT NULL      COMMENT 'FK -> diab_his_enc_encounters.id',
    `round_no`       INT           NOT NULL      COMMENT 'So thu tu dot trong luot kham, bat dau 1',
    `status`         VARCHAR(20)   NOT NULL DEFAULT 'OPEN'
                     COMMENT 'OPEN|SUBMITTED|IN_PROGRESS|COMPLETED|CANCELLED',
    `payment_status` VARCHAR(20)   NOT NULL DEFAULT 'UNPAID'
                     COMMENT 'UNPAID|PAID|WAIVED',
    `total_amount`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `billing_id`     CHAR(36)      NULL          COMMENT 'FK -> diab_his_bil_billing.id',
    `paid_at`        DATETIME      NULL,
    `paid_by`        CHAR(36)      NULL,
    `waived_reason`  VARCHAR(500)  NULL          COMMENT 'Ly do mien/no vien phi (payment_status=WAIVED)',
    `cancel_reason`  VARCHAR(500)  NULL,
    `note`           TEXT          NULL,
    `created_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)      NULL,
    `updated_at`     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)      NULL,
    `deleted_at`     DATETIME      NULL,
    `deleted_by`     CHAR(36)      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_clsround_enc_no` (`tenant_id`, `encounter_id`, `round_no`),
    INDEX `idx_clsround_enc`  (`tenant_id`, `encounter_id`),
    INDEX `idx_clsround_pay`  (`tenant_id`, `payment_status`, `status`),
    INDEX `idx_clsround_bill` (`tenant_id`, `billing_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Dot chi dinh CLS - don vi thanh toan va gate thuc hien'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9062_fix_cls_uploads_guid.sql (ban chuan GUID,
        //   ghi de shape cu cua 0006_create_cls_uploads.sql).
        // + cot branch_id them boi 9084_add_branch_id_columns.sql -> gop inline.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_fil_cls_uploads` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `branch_id`        INT          NULL COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `patient_id`       CHAR(36)     NOT NULL,
    `encounter_id`     CHAR(36)     NULL,
    `doc_type`         VARCHAR(100) NOT NULL,
    `file_id`          CHAR(36)     NULL COMMENT 'FK toi fil_files.id',
    `file_path`        VARCHAR(500) NOT NULL COMMENT 'object_key tren bucket cls-uploads',
    `file_name`        VARCHAR(255) NOT NULL,
    `mime_type`        VARCHAR(50)  NULL,
    `file_size_bytes`  BIGINT       NULL,
    `note`             TEXT         NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `uploaded_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)     NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)     NULL,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_cls_uploads_tenant_patient` (`tenant_id`, `patient_id`),
    KEY `idx_cls_uploads_tenant_encounter` (`tenant_id`, `encounter_id`),
    KEY `idx_cls_uploads_uploaded_at` (`uploaded_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Tai lieu CLS (XN/CDHA) upload dang file - khop GUID string voi FileHandlers.cs'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9096_create_telehealth_docosan.sql (muc 3)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_service_mapping` (
    `id`                    CHAR(36)        NOT NULL,
    `tenant_id`             INT             NOT NULL,
    `branch_id`             INT             NULL,
    `his_service_id`        CHAR(36)        NULL                    COMMENT 'FK -> dich vu HIS (diab_his_bil_services.id) neu can doi soat doanh thu',
    `docosan_service_id`    INT             NOT NULL                COMMENT 'services[].id gui trong payment_info',
    `docosan_service_type`  VARCHAR(30)     NOT NULL DEFAULT 'telemedicine' COMMENT 'telemedicine | at_clinic | at_home - quyet dinh apt_mode ben Docosan',
    `service_name`          VARCHAR(255)    NULL,
    `default_quantity`      INT             NOT NULL DEFAULT 1,
    `environment`           VARCHAR(20)     NOT NULL DEFAULT 'production',
    `is_active`             TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_service_tenant_remote_env` (`tenant_id`, `docosan_service_id`, `environment`),
    KEY `idx_doco_service_tenant_type` (`tenant_id`, `docosan_service_type`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Anh xa dich vu telehealth HIS <-> service Docosan'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0011_create_dtqg.sql (ban goc, chay truoc
        //   9011_create_missing_tables.sql nen la ban chuan).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_int_dtqg_submissions` (
    `id`              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`       INT          NULL,
    `prescription_id` INT          NOT NULL,
    `ma_don_thuoc`    VARCHAR(100) NULL,
    `qr_payload`      TEXT         NULL,
    `qr_image_path`   VARCHAR(500) NULL,
    `status`          ENUM('PENDING','SUBMITTED','ACCEPTED','REJECTED')
                                   NOT NULL DEFAULT 'PENDING',
    `error_code`      VARCHAR(50)  NULL,
    `error_message`   TEXT         NULL,
    `submitted_at`    DATETIME     NULL,
    `accepted_at`     DATETIME     NULL,
    `retry_count`     INT          NOT NULL DEFAULT 0,
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      INT          NULL,
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                       ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      INT          NULL,
    `deleted_at`      DATETIME     NULL,

    INDEX `idx_dtqg_tenant_status`  (`tenant_id`, `status`),
    INDEX `idx_dtqg_prescription`   (`prescription_id`),
    INDEX `idx_dtqg_submitted`      (`submitted_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Theo doi submit don thuoc len Don thuoc Quoc gia'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9160_notification_channels.sql (muc 1)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_int_notification_channels` (
    `id`               CHAR(36)     NOT NULL PRIMARY KEY DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `branch_id`        INT          NULL,
    `channel`          VARCHAR(20)  NOT NULL COMMENT 'SMS | ZALO_ZNS',
    `provider`         VARCHAR(30)  NOT NULL COMMENT 'ESMS (SMS) | ZALO_OA (Zalo ZNS)',
    `config_encrypted` TEXT         NOT NULL COMMENT 'JSON cau hinh da ma hoa AES-256-GCM',
    `is_active`        TINYINT(1)   NOT NULL DEFAULT 1,
    `last_tested_at`   DATETIME     NULL,
    `last_test_ok`     TINYINT(1)   NOT NULL DEFAULT 0,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       INT          NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       INT          NULL,
    `deleted_at`       DATETIME     NULL,
    UNIQUE KEY `uq_notif_channel_scope` (`tenant_id`, `branch_id`, `channel`),
    INDEX `idx_notif_channel_tenant` (`tenant_id`),
    INDEX `idx_notif_channel_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='FR-112: cau hinh kenh gui thong bao SMS/Zalo ZNS per-tenant/branch'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9187_legacy_scan_import.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_leg_import_batch` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `zip_file_name`    VARCHAR(255) NULL,
    `zip_object_key`   VARCHAR(500) NULL COMMENT 'bucket legacy-scans - file ZIP goc',
    `total_items`      INT          NOT NULL DEFAULT 0,
    `processed_items`  INT          NOT NULL DEFAULT 0,
    `status`           VARCHAR(20)  NOT NULL DEFAULT 'pending' COMMENT 'pending|processing|done|failed',
    `error_message`    VARCHAR(1000) NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_leg_import_batch_tenant_status` (`tenant_id`, `status`),
    KEY `idx_leg_import_batch_tenant_created` (`tenant_id`, `created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Batch nhap lieu ho so giay cu tu file ZIP anh scan'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0035_create_prescription_extensions.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pha_ddi_rules` (
    `id`             CHAR(36)   NOT NULL DEFAULT (UUID()),
    `drug1_id`       INT        NOT NULL COMMENT 'FK drug_master.ID (sorted: drug1_id < drug2_id)',
    `drug2_id`       INT        NOT NULL COMMENT 'FK drug_master.ID',
    `severity`       ENUM('MINOR','MODERATE','MAJOR','CONTRAINDICATED')
                                NOT NULL,
    `description`    TEXT       NOT NULL,
    `evidence_level` CHAR(1)    NOT NULL DEFAULT 'B',
    `source`         VARCHAR(100) NULL,
    `created_at`     DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     INT        NULL,
    `updated_at`     DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     INT        NULL,
    `deleted_at`     DATETIME   NULL,

    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_ddi_drug_pair` (`drug1_id`, `drug2_id`),
    INDEX `idx_ddi_drug1` (`drug1_id`),
    INDEX `idx_ddi_drug2` (`drug2_id`),
    INDEX `idx_ddi_severity` (`severity`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Quy tac tuong tac thuoc (Drug-Drug Interaction)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0036_drug_master_extensions.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pha_drug_categories` (
    `id`         CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT          NOT NULL,
    `code`       VARCHAR(50)  NOT NULL,
    `name`       VARCHAR(255) NOT NULL,
    `parent_id`  CHAR(36)     NULL COMMENT 'Self-reference for hierarchy',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at` DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_drug_cat_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_drug_cat_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nhom thuoc (ATC / custom)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9026_create_pha_warehouses.sql
        // pha_warehouses la BANG THAT (khong phai view), ten legacy khong prefix.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `pha_warehouses` (
  `id`               INT           NOT NULL AUTO_INCREMENT,
  `tenant_id`        INT           NOT NULL,
  `code`             VARCHAR(30)   NOT NULL,
  `name`             VARCHAR(255)  NOT NULL,
  `type`             VARCHAR(20)   NULL,
  `address`          TEXT          NULL,
  `manager_user_id`  INT           NULL,
  `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted_at`       DATETIME      NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_pha_warehouses_code_tenant` (`tenant_id`, `code`),
  INDEX `idx_pha_warehouses_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Kho duoc (pha_warehouses) - dung boi WarehouseHandlers'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0038_create_dispense_records.sql
        // Sua kieu theo 9025_fix_dispense_fk_types.sql:
        //   prescription_id INT -> CHAR(36), warehouse_id INT -> VARCHAR(36)
        // + cot branch_id them boi 9084_add_branch_id_columns.sql -> gop inline.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pha_dispense_records` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT           NOT NULL,
    `branch_id`       INT           NULL COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `prescription_id` CHAR(36)      NOT NULL COMMENT 'Theo 9025: INT -> CHAR(36)',
    `warehouse_id`    VARCHAR(36)   NOT NULL COMMENT 'Theo 9025: INT -> VARCHAR(36)',
    `dispensed_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `dispensed_by`    INT           NULL COMMENT 'FK -> sec_users.ID',
    `status`          ENUM('DISPENSED','REJECTED','RETURNED','PARTIAL')
                                    NOT NULL DEFAULT 'DISPENSED',
    `note`            TEXT          NULL,
    `total_amount`    DECIMAL(15,2) NOT NULL DEFAULT 0,
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      INT           NULL,
    `updated_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      INT           NULL,
    `deleted_at`      DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_dispense_prescription` (`prescription_id`, `tenant_id`),
    INDEX `idx_dispense_tenant_status`   (`tenant_id`, `status`),
    INDEX `idx_dispense_dispensed_at`    (`dispensed_at`),
    INDEX `idx_dispense_warehouse`       (`warehouse_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu phat thuoc (Dispense Record)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0013_pharmacy_lot_expiry.sql
        // Sua kieu theo 9025_fix_dispense_fk_types.sql:
        //   stock_id INT -> CHAR(36), warehouse_id INT -> VARCHAR(36),
        //   reference_id INT -> VARCHAR(36)
        // + cot branch_id them boi 9084_add_branch_id_columns.sql -> gop inline.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_movements` (
    `id`               INT             NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`        INT             NULL,
    `branch_id`        INT             NULL COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `stock_id`         CHAR(36)        NOT NULL COMMENT 'Theo 9025: INT -> CHAR(36)',
    `warehouse_id`     VARCHAR(36)     NOT NULL COMMENT 'Theo 9025: INT -> VARCHAR(36)',
    `movement_type`    ENUM('IMPORT','EXPORT','TRANSFER','ADJUST','RETURN')
                                       NOT NULL,
    `quantity`         DECIMAL(12,3)   NOT NULL,
    `unit_price`       DECIMAL(15,2)   NULL,
    `reason`           TEXT            NULL,
    `reference_type`   VARCHAR(50)     NULL,
    `reference_id`     VARCHAR(36)     NULL COMMENT 'Theo 9025: INT -> VARCHAR(36)',
    `movement_at`      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `performed_by`     INT             NULL,
    `created_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       INT             NULL,
    `updated_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                           ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       INT             NULL,
    `deleted_at`       DATETIME        NULL,

    INDEX `idx_stock_mov_tenant_stock`  (`tenant_id`, `stock_id`, `movement_at`),
    INDEX `idx_stock_mov_warehouse`     (`warehouse_id`, `movement_at`),
    INDEX `idx_stock_mov_type`          (`movement_type`, `movement_at`),
    INDEX `idx_stock_mov_reference`     (`reference_type`, `reference_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich su bien dong kho thuoc'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9151_stock_transfers.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_transfers` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT           NOT NULL,
    `transfer_no`     VARCHAR(30)   NOT NULL COMMENT 'So phieu dieu chuyen (bo dem theo tenant)',
    `from_branch_id`  INT           NOT NULL,
    `to_branch_id`    INT           NOT NULL,
    `status`          VARCHAR(20)   NOT NULL DEFAULT 'DRAFT'
                      COMMENT 'DRAFT|PENDING_APPROVAL|APPROVED|REJECTED|IN_TRANSIT|RECEIVED|COMPLETED|CANCELLED',
    `total_value`     DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `requires_approval` TINYINT(1)  NOT NULL DEFAULT 0,
    `reason`          VARCHAR(500)  NULL,
    `requested_by`    CHAR(36)      NULL,
    `requested_at`    DATETIME      NULL,
    `approved_by`     CHAR(36)      NULL,
    `approved_at`     DATETIME      NULL,
    `rejected_reason` VARCHAR(500)  NULL,
    `shipped_by`      CHAR(36)      NULL,
    `shipped_at`      DATETIME      NULL,
    `received_by`     CHAR(36)      NULL,
    `received_at`     DATETIME      NULL,
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      CHAR(36)      NULL,
    `updated_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      CHAR(36)      NULL,
    `deleted_at`      DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_transfer_no` (`tenant_id`, `transfer_no`),
    INDEX `idx_transfer_from` (`tenant_id`, `from_branch_id`, `status`),
    INDEX `idx_transfer_to`   (`tenant_id`, `to_branch_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu dieu chuyen kho noi bo giua chi nhanh (BR-51..BR-60)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9004_create_labrad.sql
        // + cot them boi 9037_rad_results_add_workflow_columns.sql
        //   (conclusion, status, verified_at, verified_by, dicom_count),
        //   9084_add_branch_id_columns.sql (branch_id),
        //   9188_lab_rad_ocr_source_and_raw.sql (source_file_id, ocr_raw_text)
        //   -> gop inline.
        // BO FK fk_rad_results_order (diab_his_rad_orders khong chac ton tai).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_rad_results` (
    `id`                CHAR(36)        NOT NULL,
    `tenant_id`         INT             NOT NULL,
    `branch_id`         INT             NULL COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `order_id`          CHAR(36)        NOT NULL,
    `impression`        TEXT            NULL,
    `conclusion`        TEXT            NULL COMMENT 'Them boi 9037',
    `description`       TEXT            NULL,
    `recommendation`    TEXT            NULL,
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'DRAFT' COMMENT 'Them boi 9037',
    `verified_at`       DATETIME        NULL COMMENT 'Them boi 9037',
    `verified_by`       CHAR(36)        NULL COMMENT 'Them boi 9037',
    `dicom_count`       INT             NOT NULL DEFAULT 0 COMMENT 'Them boi 9037',
    `source_file_id`    CHAR(36)        NULL COMMENT 'Them boi 9188',
    `ocr_raw_text`      TEXT            NULL COMMENT 'Them boi 9188',
    `result_pdf_path`   VARCHAR(500)    NULL,
    `image_paths`       JSON            NULL,
    `performed_at`      DATETIME        NULL,
    `performed_by`      VARCHAR(255)    NULL,
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)        NULL,
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)        NULL,
    `deleted_at`        DATETIME        NULL,
    `deleted_by`        CHAR(36)        NULL,

    PRIMARY KEY (`id`),
    INDEX `idx_rad_results_order`   (`tenant_id`, `order_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ket qua chan doan hinh anh'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0016_create_appointments.sql
        // + cot them boi 9038_sch_appointments_add_guid_refs.sql (patient_ref,
        //   doctor_ref), 9071_sch_appointments_portal_cols.sql (appointment_code,
        //   partner_reference, uuid, source_partner_ref),
        //   9084_add_branch_id_columns.sql (branch_id),
        //   9160_notification_channels.sql (reminder_sent_at) -> gop inline.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_sch_appointments` (
    `id`                 INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`          INT          NULL,
    `branch_id`          INT          NULL COMMENT 'Them boi 9084_add_branch_id_columns.sql',
    `patient_id`         INT          NULL,
    `patient_ref`        CHAR(36)     NULL COMMENT 'Them boi 9038',
    `patient_name_temp`  VARCHAR(255) NULL,
    `patient_phone`      VARCHAR(20)  NULL,
    `doctor_id`          INT          NULL,
    `doctor_ref`         CHAR(36)     NULL COMMENT 'Them boi 9038',
    `department_id`      INT          NULL,
    `service_package_id` INT          NULL,
    `appointment_at`     DATETIME     NOT NULL,
    `duration_minutes`   INT          NOT NULL DEFAULT 30,
    `status`             ENUM('PENDING','CONFIRMED','CHECKED_IN','CANCELLED','NO_SHOW')
                                      NOT NULL DEFAULT 'PENDING',
    `source`             ENUM('WALK_IN','PHONE','WEB','API','APP')
                                      NOT NULL DEFAULT 'WALK_IN',
    `source_partner_id`  INT          NULL,
    `source_partner_ref` CHAR(36)     NULL COMMENT 'Them boi 9071',
    `appointment_code`   VARCHAR(30)  NULL COMMENT 'Them boi 9071',
    `partner_reference`  VARCHAR(100) NULL COMMENT 'Them boi 9071',
    `uuid`               CHAR(36)     NULL COMMENT 'Them boi 9071',
    `reminder_sent_at`   DATETIME     NULL COMMENT 'Them boi 9160',
    `note`               TEXT         NULL,
    `created_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`         INT          NULL,
    `updated_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`         INT          NULL,
    `deleted_at`         DATETIME     NULL,

    INDEX `idx_appt_tenant_time`    (`tenant_id`, `appointment_at`),
    INDEX `idx_appt_doctor_time`    (`doctor_id`, `appointment_at`),
    INDEX `idx_appt_patient`        (`patient_id`),
    INDEX `idx_appt_status`         (`status`, `appointment_at`),
    INDEX `idx_appt_source_partner` (`source_partner_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich hen kham benh (walk-in, dien thoai, web, API doi tac)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9073_create_doctor_schedules.sql (bang 1)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_sch_doctor_schedules` (
    `id`             INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`      INT          NOT NULL,
    `doctor_ref`     CHAR(36)     NOT NULL COMMENT 'FK -> diab_his_sec_users.id',
    `day_of_week`    TINYINT      NOT NULL COMMENT '1=Thu 2 ... 7=Chu nhat (ISO: 1=Mon..7=Sun)',
    `start_time`     TIME         NOT NULL,
    `end_time`       TIME         NOT NULL,
    `slot_minutes`   INT          NOT NULL DEFAULT 15,
    `max_per_slot`   INT          NOT NULL DEFAULT 1,
    `effective_from` DATE         NULL,
    `effective_to`   DATE         NULL,
    `enabled`        TINYINT(1)   NOT NULL DEFAULT 1,
    `created_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)     NULL,
    `updated_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)     NULL,
    `deleted_at`     DATETIME     NULL,
    INDEX `idx_sch_doctor_dow` (`tenant_id`, `doctor_ref`, `day_of_week`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich lam viec bac si theo thu trong tuan'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9073_create_doctor_schedules.sql (bang 2)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_sch_schedule_blocks` (
    `id`          INT        NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`   INT        NOT NULL,
    `doctor_ref`  CHAR(36)   NOT NULL,
    `block_date`  DATE       NOT NULL,
    `start_time`  TIME       NULL COMMENT 'NULL = ca ngay',
    `end_time`    TIME       NULL,
    `reason`      VARCHAR(255) NULL,
    `created_at`  DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`  CHAR(36)   NULL,
    `deleted_at`  DATETIME   NULL,
    INDEX `idx_sch_block_doctor_date` (`tenant_id`, `doctor_ref`, `block_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Block nghi/khoa gio bac si (ngay le, hop, nghi phep)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0055_encryption_key_rotation.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_sec_encryption_keys` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`             INT NULL COMMENT 'NULL = global key, INT = tenant-specific key',
    `key_version`           INT NOT NULL DEFAULT 1,
    `key_purpose`           ENUM('PII','BHYT','OAUTH_TOKEN','VAPID','OTHER') NOT NULL,
    `key_material_encrypted` VARBINARY(512) NOT NULL COMMENT 'Encrypted with master key (KEK)',
    `algorithm`             VARCHAR(20) NOT NULL DEFAULT 'AES-256-GCM',
    `is_active`             TINYINT(1) NOT NULL DEFAULT 1,
    `rotated_at`            DATETIME NULL,
    `created_at`            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_enc_keys_lookup` (`tenant_id`, `key_purpose`, `is_active`),
    INDEX `idx_enc_keys_version` (`tenant_id`, `key_purpose`, `key_version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Quan ly encryption keys theo phien ban - Sprint 12'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9170_telehealth_allowed_icd10.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_tel_allowed_icd10` (
    `id`          CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`   INT           NOT NULL,
    `icd10_code`  VARCHAR(10)   NOT NULL,
    `icd10_name`  VARCHAR(255)  NOT NULL,
    `is_active`   TINYINT(1)    NOT NULL DEFAULT 1,
    `note`        VARCHAR(500)  NULL,
    `created_at`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`  CHAR(36)      NULL,
    `updated_at`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`  CHAR(36)      NULL,
    `deleted_at`  DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_tel_icd10_tenant_code` (`tenant_id`, `icd10_code`),
    KEY `idx_tel_icd10_tenant_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='FR-804: Danh muc ICD-10 duoc phep tu van tu xa'",

        // ------------------------------------------------------------------
        // cli_lab_outbound / cli_lab_inbound
        // Nguon DDL: db/migrations/0033_lab_partners_seed_dict.sql (CREATE TABLE).
        // Luu y: db/migrations/9061_fix_legacy_views_icd10_billable.sql chuyen 2
        //   ten nay thanh VIEW tro sang diab_his_int_lab_orders_outbound /
        //   diab_his_int_lab_results_inbound. Nhung 2 bang int_* KHONG co entity EF
        //   nen tao VIEW se hong -> dung ban CREATE TABLE goc cua 0033 (doc lap).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `cli_lab_outbound` (
    `id`                CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT         NOT NULL,
    `lab_order_id`      CHAR(36)    NOT NULL COMMENT 'FK -> diab_his_cli_lab_orders.id',
    `lab_partner_id`    CHAR(36)    NOT NULL COMMENT 'FK -> cli_lab_partners.id',
    `external_order_id` VARCHAR(100) NULL,
    `payload_json`      JSON        NULL,
    `status`            ENUM('PENDING','SENT','ACKED','FAILED')
                                    NOT NULL    DEFAULT 'PENDING',
    `retry_count`       INT         NOT NULL    DEFAULT 0,
    `error_message`     TEXT        NULL,
    `sent_at`           DATETIME    NULL,
    `acked_at`          DATETIME    NULL,
    `created_at`        DATETIME    NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)    NULL,
    `updated_at`        DATETIME    NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_outbound_status`     (`tenant_id`, `status`, `created_at`),
    INDEX `idx_outbound_partner`    (`tenant_id`, `lab_partner_id`),
    INDEX `idx_outbound_order`      (`tenant_id`, `lab_order_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Log lich su gui chi dinh XN ra doi tac ngoai'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0033_lab_partners_seed_dict.sql
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `cli_lab_inbound` (
    `id`                        CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT             NOT NULL,
    `lab_partner_id`            CHAR(36)        NOT NULL COMMENT 'FK -> cli_lab_partners.id',
    `external_result_id`        VARCHAR(100)    NOT NULL COMMENT 'ID ket qua phia doi tac (idempotent key)',
    `outbound_id`               CHAR(36)        NULL COMMENT 'FK -> cli_lab_outbound.id (neu khop duoc)',
    `payload_json`              JSON            NULL,
    `raw_hl7_message`           MEDIUMTEXT      NULL,
    `headers`                   JSON            NULL,
    `status`                    ENUM('RECEIVED','PROCESSED','FAILED')
                                                NOT NULL    DEFAULT 'RECEIVED',
    `received_at`               DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `processed_at`              DATETIME        NULL,
    `processed_result_count`    INT             NOT NULL    DEFAULT 0,
    `error_message`             TEXT            NULL,
    `created_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)        NULL,
    `updated_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_inbound_idempotent` (`lab_partner_id`, `external_result_id`),
    INDEX `idx_inbound_status`      (`tenant_id`, `status`, `received_at`),
    INDEX `idx_inbound_partner`     (`tenant_id`, `lab_partner_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Log ket qua XN nhan tu doi tac qua webhook (inbound)'",

        // ==================================================================
        // ===== BO SUNG DOT 3 =====
        // Phan 1: BANG con thieu.  Phan 2: COT con thieu (ALTER TABLE tran —
        // MySQL 8 khong ho tro ADD COLUMN IF NOT EXISTS; cau nao trung se loi
        // va bi bo qua boi try/catch tung cau ben goi).
        // ==================================================================

        // ------------------------------------------------------------------
        // cli_lab_partners — ten legacy khong prefix.
        // Nguon DDL: db/migrations/0033_lab_partners_seed_dict.sql (CREATE TABLE).
        // Luu y: 9061_fix_legacy_views_icd10_billable.sql chuyen ten nay thanh
        //   VIEW tro sang diab_his_int_lab_partners — nhung bang int_* KHONG co
        //   entity EF nen tao VIEW se hong -> dung ban CREATE TABLE goc cua 0033
        //   (cung cach da lam voi cli_lab_outbound / cli_lab_inbound).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `cli_lab_partners` (
    `id`                        CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT             NOT NULL,
    `code`                      VARCHAR(50)     NOT NULL        COMMENT 'Ma dinh danh ngan (MEDLATEC, DIAG...)',
    `name`                      VARCHAR(255)    NOT NULL,
    `endpoint_url`              VARCHAR(500)    NOT NULL,
    `auth_type`                 ENUM('NONE','API_KEY','BEARER')
                                                NOT NULL        DEFAULT 'API_KEY',
    `api_key_encrypted`         VARBINARY(512)  NULL            COMMENT 'AES-256-GCM encrypted',
    `bearer_token_encrypted`    VARBINARY(1024) NULL            COMMENT 'AES-256-GCM encrypted',
    `api_key_masked`            VARCHAR(32)     NULL            COMMENT 'sk_***XXXX hien thi UI',
    `transport`                 ENUM('REST','HL7_MLLP')
                                                NOT NULL        DEFAULT 'REST',
    `supported_tests`           JSON            NULL,
    `status`                    ENUM('ACTIVE','INACTIVE')
                                                NOT NULL        DEFAULT 'INACTIVE',
    `contact_email`             VARCHAR(255)    NULL,
    `contact_phone`             VARCHAR(30)     NULL,
    `created_at`                DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)        NULL,
    `updated_at`                DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`                CHAR(36)        NULL,
    `deleted_at`                DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_partner_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_partner_tenant_status`   (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Doi tac xet nghiem ben ngoai (Medlatec, Diag...)'",

        // ------------------------------------------------------------------
        // fil_files — ten legacy khong prefix, la BANG THAT (khong phai view).
        // Nguon: db/migrations/9062_fix_cls_uploads_guid.sql (muc 1) — ban chuan
        //   GUID, chay sau 0022_create_reception_queue.sql.
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `fil_files` (
    `id`               CHAR(36)     NOT NULL,
    `tenant_id`        INT          NOT NULL,
    `bucket`           VARCHAR(100) NOT NULL,
    `object_key`       VARCHAR(500) NOT NULL,
    `file_name`        VARCHAR(255) NOT NULL,
    `mime_type`        VARCHAR(100) NULL,
    `file_size_bytes`  BIGINT       NULL,
    `category`         VARCHAR(50)  NULL,
    `uploaded_by`      CHAR(36)     NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_fil_files_tenant` (`tenant_id`),
    KEY `idx_fil_files_tenant_bucket` (`tenant_id`, `bucket`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Metadata file luu tren object storage (MinIO/local)'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/0031_create_lab_rad_orders.sql (Rad procedure catalog)
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS diab_his_dict_rad_procedures (
    code          VARCHAR(50)  NOT NULL,
    name          VARCHAR(300) NOT NULL,
    modality      VARCHAR(20)  NULL,
    default_price DECIMAL(12,2) NULL,
    bhyt_price    DECIMAL(12,2) NULL,
    is_active     TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (code),
    FULLTEXT INDEX ft_rad_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Radiology procedure catalog'",

        // ------------------------------------------------------------------
        // Nguon: db/migrations/9187_legacy_scan_import.sql (bang 2).
        // LegacyImportHandlers.cs doc `FROM diab_his_leg_import_item i ...
        //   i.deleted_at` -> bang khong co entity EF nen phai tao tay
        //   (bang batch da co san o dot 2).
        // ------------------------------------------------------------------
        @"CREATE TABLE IF NOT EXISTS `diab_his_leg_import_item` (
    `id`                  CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT          NOT NULL,
    `batch_id`            CHAR(36)     NOT NULL,
    `original_filename`   VARCHAR(255) NULL,
    `image_object_key`    VARCHAR(500) NULL COMMENT 'bucket/key anh tren legacy-scans',
    `ocr_text`            LONGTEXT     NULL,
    `ocr_confidence`      DECIMAL(5,2) NULL,
    `matched_patient_id`  CHAR(36)     NULL,
    `match_method`        VARCHAR(20)  NULL COMMENT 'filename_auto|manual',
    `status`              VARCHAR(20)  NOT NULL DEFAULT 'pending_match',
    `saved_cls_upload_id` CHAR(36)     NULL,
    `item_error`          VARCHAR(1000) NULL,
    `confirmed_by`        CHAR(36)     NULL,
    `confirmed_at`        DATETIME     NULL,
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`          DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_leg_import_item_tenant_batch` (`tenant_id`, `batch_id`),
    KEY `idx_leg_import_item_tenant_status` (`tenant_id`, `status`),
    KEY `idx_leg_import_item_tenant_patient` (`tenant_id`, `matched_patient_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='1 anh trong batch nhap lieu ho so giay cu'",

        // ==================================================================
        // ===== PHAN 2: ALTER TABLE — cot thieu tren bang do EF tao =====
        // ==================================================================

        // d.name_vi / d.name_en -> diab_his_pha_drugs
        //   (DrugHandlers / PrescriptionHandlers / WarehouseHandlers:
        //    `SELECT name_vi FROM pha_drug_master`, `LEFT JOIN diab_his_pha_drugs d`)
        // Kieu lay tu db/migrations/9010_alter_pha_drugs_add_cols.sql
        @"ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `name_vi` VARCHAR(255) NULL",
        @"ALTER TABLE `diab_his_pha_drugs` ADD COLUMN `name_en` VARCHAR(255) NULL",

        // branch_id — kieu lay tu db/migrations/9080_helpers_branch.sql
        //   (procedure add_branch_col: 'INT NULL'), danh sach bang lay tu
        //   db/migrations/9084_add_branch_id_columns.sql.
        // Kho duoc / ton kho / xuat nhap / canh bao ton:
        //   pha_warehouses (bang nay do chinh file supplement tao o dot 2 —
        //   ban 9026 goc chua co branch_id nen phai ALTER them),
        //   pha_stocks la VIEW legacy -> bang goc diab_his_pha_stock.
        @"ALTER TABLE `pha_warehouses` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_stock` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_stock_movements` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_purchase_orders` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_grn` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_stocktakes` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_prescriptions` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_dispenses` ADD COLUMN `branch_id` INT NULL",
        // Billing / thu ngan
        @"ALTER TABLE `diab_his_bil_billing` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_bil_payments` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_bil_einvoices` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_bil_cashier_shifts` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_bil_counters` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_bil_cash_out` ADD COLUMN `branch_id` INT NULL",
        // Kham / CLS
        @"ALTER TABLE `diab_his_enc_encounters` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_lab_orders` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rad_orders` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_lab_results` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_cls_uploads` ADD COLUMN `branch_id` INT NULL",
        // Audit + report cache (r.branch_id trong cac query /api/v1/reports/...)
        @"ALTER TABLE `diab_his_sec_audit_logs` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rep_daily_revenue_cache` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rep_doctor_kpi_cache` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rep_top_drugs_cache` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rep_inventory_value_cache` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_rep_diabetes_cohort_cache` ADD COLUMN `branch_id` INT NULL",
        // Bang gia theo chi nhanh (ServicePriceOverrideHandlers /
        //   DrugPriceOverrideHandlers doc `p.*` roi map r.branch_id)
        @"ALTER TABLE `diab_his_bil_service_branch_prices` ADD COLUMN `branch_id` INT NULL",
        @"ALTER TABLE `diab_his_pha_drug_branch_prices` ADD COLUMN `branch_id` INT NULL",

        // po.warehouse_id -> diab_his_pha_purchase_orders
        //   (WarehouseHandlers: `FROM diab_his_pha_purchase_orders po`)
        // Kieu lay tu db/migrations/0037_create_purchase_orders.sql: INT NOT NULL.
        // SUY LUAN: them `DEFAULT 0` vi ALTER ADD COLUMN NOT NULL tren bang da co
        //   du lieu can gia tri mac dinh (migration goc tao moi nen khong can).
        @"ALTER TABLE `diab_his_pha_purchase_orders` ADD COLUMN `warehouse_id` INT NOT NULL DEFAULT 0",

        // is_system -> diab_his_cli_diabetes_templates (GET /api/v1/diabetes-templates)
        // Kieu lay tu db/migrations/0029_create_diabetes_history.sql
        @"ALTER TABLE `diab_his_cli_diabetes_templates` ADD COLUMN `is_system` TINYINT(1) NOT NULL DEFAULT 0",

        // i.deleted_at -> diab_his_pat_insurances
        //   (PortalHandlers: `FROM diab_his_pat_insurances i ... i.deleted_at`)
        // Kieu lay tu db/migrations/9002_create_patient.sql
        @"ALTER TABLE `diab_his_pat_insurances` ADD COLUMN `deleted_at` DATETIME NULL",

        // scopes -> diab_his_api_partners (EF map `scopes_json`)
        // Kieu lay tu db/migrations/9014_fix_dtqg_apipartners_schema.sql (muc 3)
        //   va 9027_recreate_api_partners.sql: JSON NULL.
        @"ALTER TABLE `diab_his_api_partners` ADD COLUMN `scopes` JSON NULL",

        // p.phone -> diab_his_pat_patients (CashierClosingHandlers /debts:
        //   `JOIN pat_patients p ... p.phone`)
        // Kieu lay tu db/migrations/9002_create_patient.sql: VARCHAR(30) NULL.
        @"ALTER TABLE `diab_his_pat_patients` ADD COLUMN `phone` VARCHAR(30) NULL",

        // QUAN TRONG: view pat_patients duoc tao o dot 1 bang `SELECT *`, ma MySQL
        //   khai trien `*` ngay luc tao view -> cot `phone` vua them KHONG xuat hien
        //   trong view cu. Phai tao lai view SAU cac ALTER tren.
        @"CREATE OR REPLACE VIEW pat_patients AS SELECT * FROM diab_his_pat_patients",
    };
}
