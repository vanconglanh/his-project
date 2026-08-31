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
    };
}
