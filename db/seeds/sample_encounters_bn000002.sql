-- ============================================================
-- Seed: 3 encounter mẫu cho BN000002 + chẩn đoán ICD-10
-- Idempotent: chỉ insert nếu chưa có encounter nào của BN000002
-- Cách chạy:
--   docker exec -i <mysql-container> mysql -uprodiab -p$DB_PASSWORD prodiab_his \
--     < db/seeds/sample_encounters_bn000002.sql
-- ============================================================
SET NAMES utf8mb4;

SET @patient_code := 'BN000002';

SELECT id, tenant_id
  INTO @pat_id, @tid
  FROM pat_patients
 WHERE code = @patient_code
   AND deleted_at IS NULL
 LIMIT 1;

SELECT id INTO @doc_id
  FROM sec_users
 WHERE tenant_id = @tid
   AND deleted_at IS NULL
 ORDER BY created_at ASC
 LIMIT 1;

SELECT COUNT(*) INTO @existing
  FROM cli_visits
 WHERE patient_id = @pat_id
   AND tenant_id  = @tid
   AND deleted_at IS NULL;

-- Encounter 1: lượt khám cũ nhất, đã DONE
SET @e1 := UUID();
INSERT INTO cli_visits
    (id, tenant_id, patient_id, doctor_id, room_id,
     encounter_type, status, reason_for_visit, chief_complaint,
     started_at, finished_at,
     created_at, created_by, updated_at, updated_by)
SELECT @e1, @tid, @pat_id, @doc_id, NULL,
       'FIRST_VISIT', 'DONE',
       'Khám sức khỏe định kỳ',
       'Mệt mỏi, khát nước nhiều, tiểu đêm',
       DATE_SUB(NOW(), INTERVAL 90 DAY),
       DATE_SUB(NOW(), INTERVAL 90 DAY) + INTERVAL 30 MINUTE,
       DATE_SUB(NOW(), INTERVAL 90 DAY), @doc_id,
       DATE_SUB(NOW(), INTERVAL 90 DAY), @doc_id
 WHERE @pat_id IS NOT NULL AND @existing = 0;

INSERT INTO diab_his_cli_encounter_diagnoses
    (id, tenant_id, encounter_id, icd10_code, name, type, created_at)
SELECT UUID(), @tid, @e1, 'E11.9', 'Đái tháo đường type 2 không biến chứng', 'PRIMARY', NOW()
 WHERE @pat_id IS NOT NULL AND @existing = 0;

INSERT INTO diab_his_cli_encounter_diagnoses
    (id, tenant_id, encounter_id, icd10_code, name, type, created_at)
SELECT UUID(), @tid, @e1, 'I10', 'Tăng huyết áp vô căn', 'SECONDARY', NOW()
 WHERE @pat_id IS NOT NULL AND @existing = 0;

-- Encounter 2: tái khám, DONE
SET @e2 := UUID();
INSERT INTO cli_visits
    (id, tenant_id, patient_id, doctor_id, room_id,
     encounter_type, status, reason_for_visit, chief_complaint,
     started_at, finished_at,
     created_at, created_by, updated_at, updated_by)
SELECT @e2, @tid, @pat_id, @doc_id, NULL,
       'FOLLOW_UP', 'DONE',
       'Tái khám sau 1 tháng dùng thuốc',
       'Đường huyết ổn định hơn, vẫn còn mệt',
       DATE_SUB(NOW(), INTERVAL 30 DAY),
       DATE_SUB(NOW(), INTERVAL 30 DAY) + INTERVAL 20 MINUTE,
       DATE_SUB(NOW(), INTERVAL 30 DAY), @doc_id,
       DATE_SUB(NOW(), INTERVAL 30 DAY), @doc_id
 WHERE @pat_id IS NOT NULL AND @existing = 0;

INSERT INTO diab_his_cli_encounter_diagnoses
    (id, tenant_id, encounter_id, icd10_code, name, type, created_at)
SELECT UUID(), @tid, @e2, 'E11.9', 'Đái tháo đường type 2 không biến chứng', 'PRIMARY', NOW()
 WHERE @pat_id IS NOT NULL AND @existing = 0;

-- Encounter 3: lượt khám gần nhất, IN_PROGRESS
SET @e3 := UUID();
INSERT INTO cli_visits
    (id, tenant_id, patient_id, doctor_id, room_id,
     encounter_type, status, reason_for_visit, chief_complaint,
     started_at, finished_at,
     created_at, created_by, updated_at, updated_by)
SELECT @e3, @tid, @pat_id, @doc_id, NULL,
       'FOLLOW_UP', 'IN_PROGRESS',
       'Tái khám tháng',
       'Đau đầu nhẹ, hoa mắt khi đứng dậy',
       DATE_SUB(NOW(), INTERVAL 2 HOUR), NULL,
       DATE_SUB(NOW(), INTERVAL 2 HOUR), @doc_id,
       DATE_SUB(NOW(), INTERVAL 2 HOUR), @doc_id
 WHERE @pat_id IS NOT NULL AND @existing = 0;

INSERT INTO diab_his_cli_encounter_diagnoses
    (id, tenant_id, encounter_id, icd10_code, name, type, created_at)
SELECT UUID(), @tid, @e3, 'I10', 'Tăng huyết áp vô căn', 'PRIMARY', NOW()
 WHERE @pat_id IS NOT NULL AND @existing = 0;

SELECT
    CASE
        WHEN @pat_id IS NULL THEN CONCAT('SKIPPED: khong tim thay benh nhan ', @patient_code)
        WHEN @existing > 0   THEN CONCAT('SKIPPED: BN da co ', @existing, ' encounter, khong seed lai')
        ELSE 'OK: da seed 3 encounter mau cho BN000002'
    END AS result;
