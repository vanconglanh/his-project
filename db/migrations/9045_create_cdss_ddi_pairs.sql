-- ============================================================
-- Migration: 9045_create_cdss_ddi_pairs
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Danh muc cap tuong tac thuoc-thuoc (drug-drug interaction) cho CDSS.
--   diab_his_cdss_ddi_pairs: moi dong = 1 cap hoat chat (ingredient_a < ingredient_b
--   theo alphabet de tra 2 chieu). CDSS engine sinh moi cap hoat chat trong don
--   roi tra bang nay (cache Redis) de canh bao.
--   tenant_id NULL = cap CHUAN dung chung moi tenant; tenant co the them cap rieng.
-- LUU Y LAM SANG: day la DANH MUC NOI BO tu curate tu nguon cong khai kiem chung
--   duoc (Duoc thu Quoc gia VN, to HDSD thuoc/FDA label, ADA Standards of Care).
--   CHUA thay the CSDL DDI thuong mai (Lexicomp/Micromedex). Moi cap ghi ro `source`
--   + `evidence_level`; can review lam sang truoc khi bat interruptive tren production.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT ... WHERE NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cdss_ddi_pairs (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NULL COMMENT 'NULL = cap chuan dung chung; INT = cap rieng cua tenant',
    ingredient_a   VARCHAR(120) NOT NULL COMMENT 'Hoat chat A (chuan hoa: thuong, khong dau, tieng Anh)',
    ingredient_b   VARCHAR(120) NOT NULL COMMENT 'Hoat chat B (ingredient_a < ingredient_b theo alphabet)',
    atc_a          VARCHAR(10)  NULL,
    atc_b          VARCHAR(10)  NULL,
    severity       VARCHAR(16)  NOT NULL COMMENT 'CONTRAINDICATED|MAJOR|MODERATE|MINOR',
    mechanism      TEXT         NULL COMMENT 'Co che tuong tac (tieng Viet co dau)',
    management     TEXT         NULL COMMENT 'Khuyen cao xu tri (tieng Viet co dau)',
    evidence_level VARCHAR(20)  NULL COMMENT 'ESTABLISHED|PROBABLE|THEORETICAL',
    source         VARCHAR(120) NULL COMMENT 'Nguon tham chieu (Duoc thu QG, FDA label, ADA...)',
    is_active      TINYINT(1)   NOT NULL DEFAULT 1,
    created_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by     CHAR(36)     NULL,
    updated_at     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by     CHAR(36)     NULL,
    deleted_at     DATETIME(3)  NULL,
    PRIMARY KEY (id),
    INDEX idx_ddi_pair_lookup (ingredient_a, ingredient_b),
    INDEX idx_ddi_pair_atc (atc_a, atc_b),
    INDEX idx_ddi_pair_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='CDSS: danh muc cap tuong tac thuoc-thuoc (noi bo tu curate)';

-- ------------------------------------------------------------
-- SEED cap tuong tac CHUAN (tenant_id NULL). Tap trung nhom tim mach - chuyen hoa
-- (tieu duong) pho bien tai phong kham. Chi seed cac cap DA DUOC LAM SANG GHI NHAN
-- rong rai (established). Idempotent theo (ingredient_a, ingredient_b) khi tenant_id NULL.
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS seed_ddi_pair;
DELIMITER $$
CREATE PROCEDURE seed_ddi_pair(
    p_a VARCHAR(120), p_b VARCHAR(120),
    p_atc_a VARCHAR(10), p_atc_b VARCHAR(10),
    p_sev VARCHAR(16), p_mech TEXT, p_mgmt TEXT,
    p_evi VARCHAR(20), p_src VARCHAR(120))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM diab_his_cdss_ddi_pairs
        WHERE tenant_id IS NULL AND ingredient_a = p_a AND ingredient_b = p_b
    ) THEN
        INSERT INTO diab_his_cdss_ddi_pairs
            (id, tenant_id, ingredient_a, ingredient_b, atc_a, atc_b,
             severity, mechanism, management, evidence_level, source)
        VALUES
            (UUID(), NULL, p_a, p_b, p_atc_a, p_atc_b,
             p_sev, p_mech, p_mgmt, p_evi, p_src);
    END IF;
END$$
DELIMITER ;

-- Statin - macrolide/fibrate (nguy co tieu co van - rhabdomyolysis)
CALL seed_ddi_pair('clarithromycin','simvastatin','J01FA09','C10AA01','CONTRAINDICATED',
    N'Clarithromycin uc che CYP3A4 lam tang manh nong do simvastatin, nguy co tieu co van va suy than cap.',
    N'Chong chi dinh phoi hop. Ngung simvastatin trong thoi gian dung clarithromycin hoac doi khang sinh khac.',
    'ESTABLISHED','FDA label simvastatin');
CALL seed_ddi_pair('gemfibrozil','simvastatin','C10AB04','C10AA01','CONTRAINDICATED',
    N'Gemfibrozil lam tang nong do statin, tang manh nguy co benh co/tieu co van.',
    N'Chong chi dinh phoi hop gemfibrozil voi simvastatin. Neu can fibrat, can nhac fenofibrat.',
    'ESTABLISHED','FDA label simvastatin');
CALL seed_ddi_pair('allopurinol','azathioprine','M04AA01','L04AX01','CONTRAINDICATED',
    N'Allopurinol uc che xanthine oxidase lam tang doc tinh azathioprine (suy tuy nghiem trong).',
    N'Tranh phoi hop. Neu bat buoc, giam lieu azathioprine con 25-33% va theo doi cong thuc mau.',
    'ESTABLISHED','Duoc thu Quoc gia VN');

-- Metformin - thuoc can quang i-ot (nguy co nhiem toan lactic khi suy than)
CALL seed_ddi_pair('iodinated_contrast','metformin','V08','A10BA02','MAJOR',
    N'Thuoc can quang i-ot co the gay suy than cap, lam tich luy metformin va tang nguy co nhiem toan lactic.',
    N'Tam ngung metformin truoc/sau tiem thuoc can quang (theo eGFR), danh gia lai chuc nang than truoc khi dung lai.',
    'ESTABLISHED','ADA Standards of Care; Duoc thu QG');

-- Sulfonylurea - khang sinh/khang nam (tang tac dung ha duong huyet)
CALL seed_ddi_pair('glimepiride','sulfamethoxazole','A10BB12','J01EC01','MAJOR',
    N'Sulfamethoxazole tang tac dung ha duong huyet cua sulfonylurea (uc che chuyen hoa, day khoi lien ket protein).',
    N'Theo doi duong huyet chat che, canh bao ha duong huyet; can nhac chinh lieu sulfonylurea.',
    'ESTABLISHED','Duoc thu Quoc gia VN');
CALL seed_ddi_pair('fluconazole','glimepiride','J02AC01','A10BB12','MODERATE',
    N'Fluconazole uc che CYP2C9 lam tang nong do sulfonylurea, nguy co ha duong huyet.',
    N'Theo doi duong huyet; can nhac giam lieu sulfonylurea khi phoi hop.',
    'PROBABLE','FDA label fluconazole');

-- Warfarin - thuoc lam tang chay mau / tang INR
CALL seed_ddi_pair('aspirin','warfarin','B01AC06','B01AA03','MAJOR',
    N'Phoi hop khang tieu cau + khang dong lam tang nguy co xuat huyet.',
    N'Chi phoi hop khi co chi dinh ro rang; theo doi INR va dau hieu chay mau.',
    'ESTABLISHED','Duoc thu Quoc gia VN');
CALL seed_ddi_pair('ibuprofen','warfarin','M01AE01','B01AA03','MAJOR',
    N'NSAID tang nguy co xuat huyet tieu hoa va co the day warfarin khoi lien ket protein.',
    N'Tranh NSAID o benh nhan dung warfarin; uu tien paracetamol de giam dau.',
    'ESTABLISHED','FDA label warfarin');
CALL seed_ddi_pair('fluconazole','warfarin','J02AC01','B01AA03','MAJOR',
    N'Fluconazole uc che CYP2C9 lam tang manh tac dung warfarin, tang INR.',
    N'Theo doi INR sat, can nhac giam lieu warfarin.',
    'ESTABLISHED','FDA label warfarin');
CALL seed_ddi_pair('amiodarone','warfarin','C01BD01','B01AA03','MAJOR',
    N'Amiodarone uc che chuyen hoa warfarin lam tang INR keo dai.',
    N'Giam lieu warfarin (~30-50%), theo doi INR trong nhieu tuan.',
    'ESTABLISHED','FDA label amiodarone');

-- Tang kali mau (ACEi/ARB + loi tieu giu kali / kali)
CALL seed_ddi_pair('enalapril','spironolactone','C09AA02','C03DA01','MAJOR',
    N'Phoi hop uc che men chuyen + loi tieu giu kali lam tang nguy co tang kali mau.',
    N'Theo doi kali mau va chuc nang than dinh ky; than trong o benh nhan suy than.',
    'ESTABLISHED','Duoc thu Quoc gia VN');
CALL seed_ddi_pair('enalapril','losartan','C09AA02','C09CA01','MAJOR',
    N'Phoi hop ACEi + ARB lam tang nguy co tang kali mau, tut huyet ap va suy than.',
    N'Khong khuyen cao phoi hop thuong quy; neu dung phai theo doi kali va creatinine.',
    'ESTABLISHED','ADA/KDIGO');
CALL seed_ddi_pair('enalapril','potassium_chloride','C09AA02','A12BA01','MAJOR',
    N'ACEi giu kali; bo sung kali dong thoi lam tang nguy co tang kali mau.',
    N'Tranh bo sung kali thuong quy khi dung ACEi; theo doi kali mau.',
    'ESTABLISHED','Duoc thu Quoc gia VN');
CALL seed_ddi_pair('potassium_chloride','spironolactone','A12BA01','C03DA01','MAJOR',
    N'Loi tieu giu kali + bo sung kali lam tang manh nguy co tang kali mau.',
    N'Tranh phoi hop; theo doi kali mau chat che neu bat buoc.',
    'ESTABLISHED','FDA label spironolactone');

-- ACEi/ARB - NSAID (giam tac dung ha ap, nguy co suy than - "triple whammy")
CALL seed_ddi_pair('enalapril','ibuprofen','C09AA02','M01AE01','MODERATE',
    N'NSAID giam tac dung ha ap cua ACEi va tang nguy co suy than (nhat la khi phoi hop loi tieu).',
    N'Han che NSAID; theo doi huyet ap va chuc nang than.',
    'ESTABLISHED','Duoc thu Quoc gia VN');

-- Digoxin - thuoc tang nong do digoxin
CALL seed_ddi_pair('amiodarone','digoxin','C01BD01','C01AA05','MAJOR',
    N'Amiodarone lam tang nong do digoxin, nguy co ngo doc digoxin.',
    N'Giam lieu digoxin ~50%, theo doi nong do digoxin va dien tim.',
    'ESTABLISHED','FDA label digoxin');
CALL seed_ddi_pair('digoxin','verapamil','C01AA05','C08DA01','MODERATE',
    N'Verapamil lam tang nong do digoxin.',
    N'Theo doi nong do digoxin va nhip tim; can nhac giam lieu digoxin.',
    'ESTABLISHED','Duoc thu Quoc gia VN');

-- Nhip cham / block dan truyen (chen beta + chen kenh calci nhom non-DHP)
CALL seed_ddi_pair('propranolol','verapamil','C07AA05','C08DA01','MAJOR',
    N'Phoi hop chen beta + verapamil lam tang nguy co nhip cham nang, block nhi that va suy tim.',
    N'Tranh phoi hop tinh mach; than trong khi dung duong uong, theo doi nhip tim.',
    'ESTABLISHED','FDA label verapamil');

-- Clopidogrel - PPI (giam hoat hoa clopidogrel)
CALL seed_ddi_pair('clopidogrel','omeprazole','B01AC04','A02BC01','MODERATE',
    N'Omeprazole uc che CYP2C19 lam giam hoat hoa clopidogrel, co the giam hieu qua khang tieu cau.',
    N'Uu tien pantoprazole neu can PPI; hoac dung PPI cach xa thoi diem clopidogrel.',
    'PROBABLE','FDA label clopidogrel');

-- Methotrexate - NSAID (tang doc tinh methotrexate)
CALL seed_ddi_pair('ibuprofen','methotrexate','M01AE01','L04AX03','MAJOR',
    N'NSAID giam thai methotrexate qua than, tang doc tinh (suy tuy, doc than).',
    N'Tranh NSAID lieu cao khi dung methotrexate; theo doi cong thuc mau va chuc nang than.',
    'ESTABLISHED','Duoc thu Quoc gia VN');

DROP PROCEDURE IF EXISTS seed_ddi_pair;
