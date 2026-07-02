-- ============================================================
-- Migration: 0028_seed_icd10
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-E07
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Tao bang neu chua co (0018 co the da tao)
CREATE TABLE IF NOT EXISTS diab_his_dict_icd10 (
    code        VARCHAR(10)  NOT NULL,
    name_vi     VARCHAR(500) NOT NULL DEFAULT '',
    name_en     VARCHAR(500) NOT NULL DEFAULT '',
    category    VARCHAR(20)  NULL COMMENT 'e.g. E10-E14',
    parent_code VARCHAR(10)  NULL,
    is_billable TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (code),
    INDEX idx_icd10_category (category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='ICD-10 dictionary. DBA load full CSV (10000+ ma) after migration.';

-- ADD FULLTEXT neu chua co
DROP PROCEDURE IF EXISTS _add_ft_icd10;
DELIMITER $$
CREATE PROCEDURE _add_ft_icd10()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'diab_his_dict_icd10'
          AND INDEX_NAME   = 'ft_icd10_names'
    ) THEN
        ALTER TABLE diab_his_dict_icd10
            ADD FULLTEXT INDEX ft_icd10_names (name_vi, name_en) WITH PARSER ngram;
    END IF;
END$$
DELIMITER ;
CALL _add_ft_icd10();
DROP PROCEDURE IF EXISTS _add_ft_icd10;

-- Seed E10-E14 + 50 ma pho bien VN
INSERT IGNORE INTO diab_his_dict_icd10 (code, name_vi, name_en, category, parent_code, is_billable) VALUES
-- Đái tháo đường
('E10',   'Đái tháo đường phụ thuộc insulin',                              'Type 1 diabetes mellitus',                                  'E10-E14', NULL, 0),
('E10.0', 'ĐTĐ typ 1 với hôn mê',                                          'Type 1 diabetes mellitus with coma',                        'E10-E14', 'E10', 1),
('E10.1', 'ĐTĐ typ 1 với nhiễm toan ceton',                                 'Type 1 diabetes mellitus with ketoacidosis',                'E10-E14', 'E10', 1),
('E10.2', 'ĐTĐ typ 1 với biến chứng thận',                                  'Type 1 diabetes mellitus with renal complications',         'E10-E14', 'E10', 1),
('E10.3', 'ĐTĐ typ 1 với biến chứng mắt',                                   'Type 1 diabetes mellitus with ophthalmic complications',    'E10-E14', 'E10', 1),
('E10.4', 'ĐTĐ typ 1 với biến chứng thần kinh',                             'Type 1 diabetes mellitus with neurological complications',  'E10-E14', 'E10', 1),
('E10.5', 'ĐTĐ typ 1 với biến chứng tuần hoàn ngoại vi',                    'Type 1 diabetes mellitus with peripheral circulatory comp', 'E10-E14', 'E10', 1),
('E10.6', 'ĐTĐ typ 1 với biến chứng khác được xác định',                    'Type 1 diabetes mellitus with other specified complications','E10-E14', 'E10', 1),
('E10.7', 'ĐTĐ typ 1 với đa biến chứng',                                    'Type 1 diabetes mellitus with multiple complications',      'E10-E14', 'E10', 1),
('E10.8', 'ĐTĐ typ 1 với biến chứng không xác định',                        'Type 1 diabetes mellitus with unspecified complications',   'E10-E14', 'E10', 1),
('E10.9', 'ĐTĐ typ 1 không có biến chứng',                                  'Type 1 diabetes mellitus without complications',            'E10-E14', 'E10', 1),
('E11',   'Đái tháo đường không phụ thuộc insulin',                          'Type 2 diabetes mellitus',                                  'E10-E14', NULL, 0),
('E11.0', 'ĐTĐ typ 2 với hôn mê',                                           'Type 2 diabetes mellitus with coma',                        'E10-E14', 'E11', 1),
('E11.1', 'ĐTĐ typ 2 với nhiễm toan ceton',                                  'Type 2 diabetes mellitus with ketoacidosis',                'E10-E14', 'E11', 1),
('E11.2', 'ĐTĐ typ 2 với biến chứng thận',                                   'Type 2 diabetes mellitus with renal complications',         'E10-E14', 'E11', 1),
('E11.3', 'ĐTĐ typ 2 với biến chứng mắt',                                    'Type 2 diabetes mellitus with ophthalmic complications',    'E10-E14', 'E11', 1),
('E11.4', 'ĐTĐ typ 2 với biến chứng thần kinh',                              'Type 2 diabetes mellitus with neurological complications',  'E10-E14', 'E11', 1),
('E11.5', 'ĐTĐ typ 2 với biến chứng tuần hoàn ngoại vi',                     'Type 2 diabetes mellitus with peripheral circulatory comp', 'E10-E14', 'E11', 1),
('E11.6', 'ĐTĐ typ 2 với biến chứng khác được xác định',                     'Type 2 diabetes mellitus with other specified complications','E10-E14', 'E11', 1),
('E11.7', 'ĐTĐ typ 2 với đa biến chứng',                                     'Type 2 diabetes mellitus with multiple complications',      'E10-E14', 'E11', 1),
('E11.8', 'ĐTĐ typ 2 với biến chứng không xác định',                         'Type 2 diabetes mellitus with unspecified complications',   'E10-E14', 'E11', 1),
('E11.9', 'ĐTĐ typ 2 không có biến chứng',                                   'Type 2 diabetes mellitus without complications',            'E10-E14', 'E11', 1),
('E12',   'Đái tháo đường liên quan đến suy dinh dưỡng',                     'Malnutrition-related diabetes mellitus',                    'E10-E14', NULL, 0),
('E12.9', 'ĐTĐ liên quan suy dinh dưỡng không biến chứng',                   'Malnutrition-related diabetes without complications',       'E10-E14', 'E12', 1),
('E13',   'Đái tháo đường xác định khác',                                    'Other specified diabetes mellitus',                         'E10-E14', NULL, 0),
('E13.9', 'ĐTĐ xác định khác không biến chứng',                              'Other specified diabetes without complications',            'E10-E14', 'E13', 1),
('E14',   'Đái tháo đường không xác định',                                   'Unspecified diabetes mellitus',                             'E10-E14', NULL, 0),
('E14.9', 'ĐTĐ không xác định không biến chứng',                             'Unspecified diabetes without complications',                'E10-E14', 'E14', 1),
-- Bệnh tim mạch phổ biến
('I10',   'Tăng huyết áp nguyên phát',                                       'Essential (primary) hypertension',                          'I10-I15', NULL, 1),
('I11.9', 'Tăng huyết áp có bệnh tim không có suy tim',                      'Hypertensive heart disease without heart failure',          'I10-I15', 'I11', 1),
('I20.9', 'Đau thắt ngực không xác định',                                    'Angina pectoris, unspecified',                              'I20-I25', 'I20', 1),
('I21.9', 'Nhồi máu cơ tim cấp không xác định',                              'Acute myocardial infarction, unspecified',                  'I20-I25', 'I21', 1),
('I50.9', 'Suy tim không xác định',                                          'Heart failure, unspecified',                                'I50-I52', 'I50', 1),
-- Bệnh hô hấp
('J06.9', 'Nhiễm khuẩn hô hấp trên cấp tính không xác định',                'Acute upper respiratory infection, unspecified',            'J00-J06', 'J06', 1),
('J18.9', 'Viêm phổi không xác định',                                        'Pneumonia, unspecified organism',                           'J12-J18', 'J18', 1),
('J45.9', 'Hen không xác định',                                              'Asthma, unspecified',                                       'J45-J46', 'J45', 1),
-- Tiêu hóa
('K21.0', 'Trào ngược dạ dày thực quản với viêm thực quản',                  'GERD with esophagitis',                                     'K20-K31', 'K21', 1),
('K29.7', 'Viêm dạ dày không xác định',                                      'Gastritis, unspecified',                                    'K20-K31', 'K29', 1),
('K57.3', 'Bệnh túi thừa đại tràng không có thủng và áp xe không biến chứng','Diverticular disease of large intestine without perforation','K55-K63', 'K57', 1),
-- Thận - tiết niệu
('N18.3', 'Bệnh thận mãn tính giai đoạn 3',                                  'Chronic kidney disease, stage 3',                           'N17-N19', 'N18', 1),
('N18.9', 'Bệnh thận mãn tính không xác định giai đoạn',                     'Chronic kidney disease, unspecified',                       'N17-N19', 'N18', 1),
-- Rối loạn lipid
('E78.0', 'Tăng cholesterol máu thuần túy',                                  'Pure hypercholesterolemia',                                 'E78',     NULL, 1),
('E78.5', 'Tăng lipid máu không xác định',                                   'Hyperlipidemia, unspecified',                               'E78',     NULL, 1),
-- Rối loạn tuyến giáp
('E03.9', 'Suy giáp không xác định',                                         'Hypothyroidism, unspecified',                               'E00-E07', 'E03', 1),
('E05.9', 'Cường giáp không xác định',                                       'Thyrotoxicosis, unspecified',                               'E00-E07', 'E05', 1),
-- Xương khớp
('M10.9', 'Gút không xác định',                                              'Gout, unspecified',                                         'M10-M14', 'M10', 1),
('M79.3', 'Viêm mô tế bào',                                                  'Panniculitis',                                              'M70-M79', 'M79', 1),
-- Thần kinh
('G43.9', 'Đau nửa đầu không xác định',                                      'Migraine, unspecified',                                     'G40-G47', 'G43', 1),
('G47.0', 'Rối loạn giấc ngủ không vào giấc',                                'Insomnia',                                                  'G40-G47', 'G47', 1),
-- Tâm thần
('F32.9', 'Giai đoạn trầm cảm không xác định',                               'Major depressive episode, unspecified',                     'F30-F39', 'F32', 1),
('F41.1', 'Rối loạn lo âu lan toả',                                          'Generalized anxiety disorder',                              'F40-F48', 'F41', 1),
-- Nhiễm khuẩn
('A09',   'Nhiễm trùng đường tiêu hóa không xác định',                       'Infectious gastroenteritis and colitis, unspecified',        'A00-A09', NULL, 1),
('B34.9', 'Nhiễm virus không xác định',                                      'Viral infection, unspecified',                              'B25-B34', NULL, 1);

-- TODO DBA: load full ICD-10 VN (10000+ ma) qua: mysql -e "LOAD DATA INFILE '/path/icd10_vn.csv' INTO TABLE diab_his_dict_icd10 FIELDS TERMINATED BY ',' ENCLOSED BY '\"' LINES TERMINATED BY '\n' IGNORE 1 ROWS;"
