-- ============================================================
-- Migration: 9065_fix_prod_dict_mojibake
-- Muc dich: sua mojibake tieng Viet (double-encoded UTF-8 qua cp1252) o 3 bang
--   tu dien tren PROD do seed 03/07 chay thieu --default-character-set=utf8mb4:
--   diab_his_dict_icd10.name_vi (28), diab_his_dict_drug_units.name (8),
--   diab_his_dict_doc_types.name (7) + 1 dong permission api_partner.read.
--   Re-seed gia tri dung (khop theo natural key 'code'). Local/ref_* la nguon chuan.
-- Idempotent: YES (UPDATE ve gia tri dung; chay lai vo hai).
-- LUU Y: PHAI ap bang mysql --default-character-set=utf8mb4.
-- ============================================================
SET NAMES utf8mb4;

-- 1. diab_his_dict_icd10.name_vi
UPDATE diab_his_dict_icd10 SET name_vi='Nhiễm trùng đường tiêu hóa không xác định' WHERE code='A09';
UPDATE diab_his_dict_icd10 SET name_vi='Nhiễm virus không xác định' WHERE code='B34.9';
UPDATE diab_his_dict_icd10 SET name_vi='Suy giáp không xác định' WHERE code='E03.9';
UPDATE diab_his_dict_icd10 SET name_vi='Cường giáp không xác định' WHERE code='E05.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đái tháo đường typ 1' WHERE code='E10';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với hôn mê' WHERE code='E10.0';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với nhiễm toan ceton' WHERE code='E10.1';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với biến chứng thận' WHERE code='E10.2';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với biến chứng mắt' WHERE code='E10.3';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với biến chứng thần kinh' WHERE code='E10.4';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với biến chứng tuần hoàn ngoại vi' WHERE code='E10.5';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với các biến chứng khác' WHERE code='E10.6';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với nhiều biến chứng' WHERE code='E10.7';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 với biến chứng không đặc hiệu' WHERE code='E10.8';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 1 không có biến chứng' WHERE code='E10.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đái tháo đường typ 2' WHERE code='E11';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với hôn mê' WHERE code='E11.0';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với nhiễm toan ceton' WHERE code='E11.1';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với biến chứng thận' WHERE code='E11.2';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với biến chứng mắt' WHERE code='E11.3';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với biến chứng thần kinh' WHERE code='E11.4';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với biến chứng tuần hoàn ngoại vi' WHERE code='E11.5';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với các biến chứng khác' WHERE code='E11.6';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với nhiều biến chứng' WHERE code='E11.7';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 với biến chứng không đặc hiệu' WHERE code='E11.8';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ typ 2 không có biến chứng' WHERE code='E11.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đái tháo đường liên quan đến suy dinh dưỡng' WHERE code='E12';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ liên quan suy dinh dưỡng không biến chứng' WHERE code='E12.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đái tháo đường đặc hiệu khác' WHERE code='E13';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ đặc hiệu khác không biến chứng' WHERE code='E13.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đái tháo đường không đặc hiệu' WHERE code='E14';
UPDATE diab_his_dict_icd10 SET name_vi='ĐTĐ không đặc hiệu, không biến chứng' WHERE code='E14.9';
UPDATE diab_his_dict_icd10 SET name_vi='Tăng cholesterol máu thuần túy' WHERE code='E78.0';
UPDATE diab_his_dict_icd10 SET name_vi='Tăng lipid máu không xác định' WHERE code='E78.5';
UPDATE diab_his_dict_icd10 SET name_vi='Giai đoạn trầm cảm không xác định' WHERE code='F32.9';
UPDATE diab_his_dict_icd10 SET name_vi='Rối loạn lo âu lan toả' WHERE code='F41.1';
UPDATE diab_his_dict_icd10 SET name_vi='Đau nửa đầu không xác định' WHERE code='G43.9';
UPDATE diab_his_dict_icd10 SET name_vi='Rối loạn giấc ngủ không vào giấc' WHERE code='G47.0';
UPDATE diab_his_dict_icd10 SET name_vi='Tăng huyết áp nguyên phát' WHERE code='I10';
UPDATE diab_his_dict_icd10 SET name_vi='Tăng huyết áp có bệnh tim không có suy tim' WHERE code='I11.9';
UPDATE diab_his_dict_icd10 SET name_vi='Đau thắt ngực không xác định' WHERE code='I20.9';
UPDATE diab_his_dict_icd10 SET name_vi='Nhồi máu cơ tim cấp không xác định' WHERE code='I21.9';
UPDATE diab_his_dict_icd10 SET name_vi='Suy tim không xác định' WHERE code='I50.9';
UPDATE diab_his_dict_icd10 SET name_vi='Nhiễm khuẩn hô hấp trên cấp tính không xác định' WHERE code='J06.9';
UPDATE diab_his_dict_icd10 SET name_vi='Viêm phổi không xác định' WHERE code='J18.9';
UPDATE diab_his_dict_icd10 SET name_vi='Hen không xác định' WHERE code='J45.9';
UPDATE diab_his_dict_icd10 SET name_vi='Trào ngược dạ dày thực quản với viêm thực quản' WHERE code='K21.0';
UPDATE diab_his_dict_icd10 SET name_vi='Viêm dạ dày không xác định' WHERE code='K29.7';
UPDATE diab_his_dict_icd10 SET name_vi='Bệnh túi thừa đại tràng không có thủng và áp xe không biến chứng' WHERE code='K57.3';
UPDATE diab_his_dict_icd10 SET name_vi='Gút không xác định' WHERE code='M10.9';
UPDATE diab_his_dict_icd10 SET name_vi='Viêm mô tế bào' WHERE code='M79.3';
UPDATE diab_his_dict_icd10 SET name_vi='Bệnh thận mãn tính giai đoạn 3' WHERE code='N18.3';
UPDATE diab_his_dict_icd10 SET name_vi='Bệnh thận mãn tính không xác định giai đoạn' WHERE code='N18.9';

-- 2. diab_his_dict_drug_units.name
UPDATE diab_his_dict_drug_units SET name='Chai' WHERE code='CHAI';
UPDATE diab_his_dict_drug_units SET name='Gram' WHERE code='G';
UPDATE diab_his_dict_drug_units SET name='Gói' WHERE code='GOI';
UPDATE diab_his_dict_drug_units SET name='Hộp' WHERE code='HOP';
UPDATE diab_his_dict_drug_units SET name='Kg' WHERE code='KG';
UPDATE diab_his_dict_drug_units SET name='Lọ' WHERE code='LO';
UPDATE diab_his_dict_drug_units SET name='mL' WHERE code='ML';
UPDATE diab_his_dict_drug_units SET name='Ống' WHERE code='ONG';
UPDATE diab_his_dict_drug_units SET name='Túi' WHERE code='TUIP';
UPDATE diab_his_dict_drug_units SET name='Tuýp' WHERE code='TUP';
UPDATE diab_his_dict_drug_units SET name='Vỉ' WHERE code='VI';
UPDATE diab_his_dict_drug_units SET name='Viên' WHERE code='VIEN';

-- 3. diab_his_dict_doc_types.name
UPDATE diab_his_dict_doc_types SET name='Xét nghiệm máu' WHERE code='LAB_BLOOD';
UPDATE diab_his_dict_doc_types SET name='Xét nghiệm khác' WHERE code='LAB_OTHER';
UPDATE diab_his_dict_doc_types SET name='Xét nghiệm nước tiểu' WHERE code='LAB_URINE';
UPDATE diab_his_dict_doc_types SET name='Tài liệu khác' WHERE code='OTHER';
UPDATE diab_his_dict_doc_types SET name='CT scan' WHERE code='RAD_CT';
UPDATE diab_his_dict_doc_types SET name='Điện tim (ECG)' WHERE code='RAD_ECG';
UPDATE diab_his_dict_doc_types SET name='Siêu âm tim (Echo)' WHERE code='RAD_ECHO';
UPDATE diab_his_dict_doc_types SET name='MRI' WHERE code='RAD_MRI';
UPDATE diab_his_dict_doc_types SET name='Siêu âm' WHERE code='RAD_ULTRASOUND';
UPDATE diab_his_dict_doc_types SET name='X-quang' WHERE code='RAD_XRAY';

-- 4. permission api_partner.read description
UPDATE diab_his_sec_permissions SET description='Xem danh sách và thống kê đối tác API' WHERE code='api_partner.read';
