-- ============================================================
-- Migration: 9035_seed_code_master
-- Mo ta: Seed 27 nhom ma enum code cung (dong bo frontend/lib/constants/code-labels.ts)
--   vao CODE_MASTER + CODE_DETAIL_MASTER. KHONG gom nhom co master table rieng (ROLE...).
-- Idempotent: YES (INSERT IGNORE master; detail ON DUPLICATE KEY UPDATE name)
-- ============================================================
SET NAMES utf8mb4;

-- ---------- CODE_MASTER ----------
INSERT IGNORE INTO diab_his_sys_code_master (id, name, sort_order) VALUES
 ('GENDER','Gioi tinh',10),
 ('BLOOD_TYPE','Nhom mau',20),
 ('NATIONALITY','Quoc tich',30),
 ('MARITAL_STATUS','Tinh trang hon nhan',40),
 ('PATIENT_TYPE','Doi tuong benh nhan',50),
 ('VISIT_TYPE','Loai kham (tiep don)',60),
 ('RELATIONSHIP','Quan he lien he',70),
 ('ENCOUNTER_TYPE','Loai luot kham',80),
 ('ENCOUNTER_STATUS','Trang thai luot kham',90),
 ('DIABETES_TYPE','Loai dai thao duong',100),
 ('MODALITY','Phuong thuc CDHA',110),
 ('CLS_PRIORITY','Do uu tien CLS',120),
 ('LAB_RESULT_FLAG','Co ket qua xet nghiem',130),
 ('DRUG_FORM','Dang bao che',140),
 ('SERVICE_CATEGORY','Nhom dich vu',150),
 ('VAT_RATE','Thue VAT',160),
 ('PRESCRIPTION_STATUS','Trang thai don thuoc',170),
 ('DRUG_STATUS','Trang thai thuoc',180),
 ('STOCK_ADJUST_REASON','Ly do dieu chinh kho',190),
 ('BILLING_STATUS','Trang thai hoa don',200),
 ('INSURANCE_TYPE','Loai bao hiem',210),
 ('EINVOICE_PROVIDER','Nha cung cap HDDT',220),
 ('APPOINTMENT_STATUS','Trang thai lich hen',230),
 ('APPOINTMENT_SOURCE','Nguon lich hen',240),
 ('USER_STATUS','Trang thai nguoi dung',250),
 ('TENANT_STATUS','Trang thai phong kham',260),
 ('COMMON_STATUS','Trang thai chung',270);

-- ---------- CODE_DETAIL_MASTER ----------
INSERT INTO diab_his_sys_code_detail (code_master_id, code, name, sort_order) VALUES
 ('GENDER','MALE','Nam',1),('GENDER','FEMALE','Nữ',2),('GENDER','OTHER','Khác',3),
 ('BLOOD_TYPE','A_POS','A+',1),('BLOOD_TYPE','A_NEG','A-',2),('BLOOD_TYPE','B_POS','B+',3),('BLOOD_TYPE','B_NEG','B-',4),('BLOOD_TYPE','AB_POS','AB+',5),('BLOOD_TYPE','AB_NEG','AB-',6),('BLOOD_TYPE','O_POS','O+',7),('BLOOD_TYPE','O_NEG','O-',8),('BLOOD_TYPE','UNKNOWN','Chưa xác định',9),
 ('NATIONALITY','VN','Việt Nam',1),('NATIONALITY','US','Hoa Kỳ',2),('NATIONALITY','CN','Trung Quốc',3),('NATIONALITY','JP','Nhật Bản',4),('NATIONALITY','KR','Hàn Quốc',5),('NATIONALITY','OTHER','Khác',6),
 ('MARITAL_STATUS','SINGLE','Độc thân',1),('MARITAL_STATUS','MARRIED','Đã kết hôn',2),('MARITAL_STATUS','DIVORCED','Ly hôn',3),('MARITAL_STATUS','WIDOWED','Goá',4),('MARITAL_STATUS','OTHER','Khác',5),
 ('PATIENT_TYPE','SERVICE','Dịch vụ',1),('PATIENT_TYPE','BHYT','Bảo hiểm y tế',2),('PATIENT_TYPE','FREE','Miễn phí',3),('PATIENT_TYPE','CONTRACT','Hợp đồng',4),
 ('VISIT_TYPE','FIRST_VISIT','Khám lần đầu',1),('VISIT_TYPE','FOLLOW_UP','Tái khám',2),('VISIT_TYPE','EMERGENCY','Cấp cứu',3),('VISIT_TYPE','SPECIALIST','Khám chuyên khoa',4),
 ('RELATIONSHIP','FATHER','Cha',1),('RELATIONSHIP','MOTHER','Mẹ',2),('RELATIONSHIP','SPOUSE','Vợ/Chồng',3),('RELATIONSHIP','CHILD','Con',4),('RELATIONSHIP','SIBLING','Anh/Chị/Em',5),('RELATIONSHIP','OTHER','Khác',6),
 ('ENCOUNTER_TYPE','FIRST_VISIT','Khám mới',1),('ENCOUNTER_TYPE','FOLLOW_UP','Tái khám',2),('ENCOUNTER_TYPE','EMERGENCY','Cấp cứu',3),('ENCOUNTER_TYPE','CONSULTATION','Hội chẩn',4),
 ('ENCOUNTER_STATUS','WAITING','Chờ khám',1),('ENCOUNTER_STATUS','IN_PROGRESS','Đang khám',2),('ENCOUNTER_STATUS','DONE','Đã khám xong',3),('ENCOUNTER_STATUS','CANCELLED','Đã huỷ',4),
 ('DIABETES_TYPE','TYPE_1','Đái tháo đường type 1',1),('DIABETES_TYPE','TYPE_2','Đái tháo đường type 2',2),('DIABETES_TYPE','GESTATIONAL','ĐTĐ thai kỳ',3),('DIABETES_TYPE','MODY','MODY',4),('DIABETES_TYPE','OTHER','Khác',5),
 ('MODALITY','XRAY','X-quang',1),('MODALITY','US','Siêu âm',2),('MODALITY','CT','CT Scan',3),('MODALITY','MRI','MRI',4),('MODALITY','MAMMO','Nhũ ảnh',5),('MODALITY','ECG','Điện tim',6),('MODALITY','ENDO','Nội soi',7),
 ('CLS_PRIORITY','NORMAL','Thường',1),('CLS_PRIORITY','URGENT','Khẩn',2),('CLS_PRIORITY','STAT','Cấp cứu',3),
 ('LAB_RESULT_FLAG','NORMAL','Bình thường',1),('LAB_RESULT_FLAG','HIGH','Cao',2),('LAB_RESULT_FLAG','LOW','Thấp',3),('LAB_RESULT_FLAG','CRITICAL','Nguy hiểm',4),('LAB_RESULT_FLAG','ABNORMAL','Bất thường',5),
 ('DRUG_FORM','TABLET','Viên nén',1),('DRUG_FORM','CAPSULE','Viên nang',2),('DRUG_FORM','SYRUP','Syrup',3),('DRUG_FORM','INJ','Tiêm',4),('DRUG_FORM','CREAM','Kem',5),('DRUG_FORM','OINTMENT','Mỡ',6),('DRUG_FORM','DROP','Nhỏ giọt',7),('DRUG_FORM','INHALER','Hít',8),('DRUG_FORM','POWDER','Bột',9),('DRUG_FORM','SUPPOSITORY','Đặt',10),('DRUG_FORM','OTHER','Khác',11),
 ('SERVICE_CATEGORY','CONSULTATION','Khám bệnh',1),('SERVICE_CATEGORY','PROCEDURE','Thủ thuật',2),('SERVICE_CATEGORY','LAB','Xét nghiệm',3),('SERVICE_CATEGORY','RAD','CĐHA',4),('SERVICE_CATEGORY','PHARMACY','Dược',5),('SERVICE_CATEGORY','OTHER','Khác',6),
 ('VAT_RATE','0','0%',1),('VAT_RATE','5','5%',2),('VAT_RATE','8','8%',3),('VAT_RATE','10','10%',4),
 ('PRESCRIPTION_STATUS','DRAFT','Nháp',1),('PRESCRIPTION_STATUS','SIGNED','Đã ký',2),('PRESCRIPTION_STATUS','CANCELLED','Đã huỷ',3),('PRESCRIPTION_STATUS','DISPENSED','Đã phát',4),
 ('DRUG_STATUS','ACTIVE','Đang dùng',1),('DRUG_STATUS','INACTIVE','Ngừng dùng',2),
 ('STOCK_ADJUST_REASON','STOCKTAKE','Kiểm kê',1),('STOCK_ADJUST_REASON','DAMAGED','Hư hỏng',2),('STOCK_ADJUST_REASON','EXPIRED','Hết hạn',3),('STOCK_ADJUST_REASON','LOST','Thất thoát',4),('STOCK_ADJUST_REASON','OTHER','Khác',5),
 ('BILLING_STATUS','UNPAID','Chưa thu',1),('BILLING_STATUS','PARTIAL','Thu một phần',2),('BILLING_STATUS','PAID','Đã thu',3),('BILLING_STATUS','REFUNDED','Đã hoàn',4),('BILLING_STATUS','VOID','Đã huỷ',5),
 ('INSURANCE_TYPE','BHYT','Bảo hiểm y tế (BHYT)',1),('INSURANCE_TYPE','PRIVATE','Bảo hiểm tư nhân',2),('INSURANCE_TYPE','OTHER','Khác',3),
 ('EINVOICE_PROVIDER','MISA','MISA',1),('EINVOICE_PROVIDER','VNPT','VNPT',2),('EINVOICE_PROVIDER','EFY','EFY',3),
 ('APPOINTMENT_STATUS','PENDING','Chờ xác nhận',1),('APPOINTMENT_STATUS','CONFIRMED','Đã xác nhận',2),('APPOINTMENT_STATUS','CHECKED_IN','Đã check-in',3),('APPOINTMENT_STATUS','CANCELLED','Đã huỷ',4),('APPOINTMENT_STATUS','NO_SHOW','Không đến',5),
 ('APPOINTMENT_SOURCE','WALK_IN','Vãng lai',1),('APPOINTMENT_SOURCE','PHONE','Điện thoại',2),('APPOINTMENT_SOURCE','WEB','Website',3),('APPOINTMENT_SOURCE','API','API',4),('APPOINTMENT_SOURCE','APP','Ứng dụng',5),
 ('USER_STATUS','ACTIVE','Đang hoạt động',1),('USER_STATUS','PENDING','Chờ kích hoạt',2),('USER_STATUS','LOCKED','Đã khoá',3),('USER_STATUS','DISABLED','Vô hiệu hoá',4),
 ('TENANT_STATUS','ACTIVE','Đang hoạt động',1),('TENANT_STATUS','SUSPENDED','Tạm ngưng',2),('TENANT_STATUS','TERMINATED','Đã chấm dứt',3),
 ('COMMON_STATUS','ACTIVE','Đang hoạt động',1),('COMMON_STATUS','INACTIVE','Ngừng hoạt động',2)
ON DUPLICATE KEY UPDATE name = VALUES(name), sort_order = VALUES(sort_order);
