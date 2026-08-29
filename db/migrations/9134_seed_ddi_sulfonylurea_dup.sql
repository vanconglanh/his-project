-- ============================================================
-- Migration: 9134_seed_ddi_sulfonylurea_dup
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Muc dich: bo sung cap tuong tac/trung lap nhom sulfonylurea cho CDSS DDI.
--   Bang diab_his_cdss_ddi_pairs (tao boi 9045) da co ~20 cap tim mach-chuyen hoa,
--   nhung CHUA co cap trung lap 2 sulfonylurea (vd Glibenclamide + Gliclazide) —
--   cung nhom, phoi hop lam tang manh nguy co ha duong huyet (duplicate therapy).
--   Them cap nay de tinh nang canh bao CDSS test duoc dung vi du thuong gap.
-- Idempotent: YES (INSERT ... WHERE NOT EXISTS theo (ingredient_a, ingredient_b) khi tenant_id NULL).
-- Chuan hoa: ingredient thuong, khong dau, tieng Anh; ingredient_a < ingredient_b (alphabet).
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO diab_his_cdss_ddi_pairs
    (id, tenant_id, ingredient_a, ingredient_b, atc_a, atc_b,
     severity, mechanism, management, evidence_level, source)
SELECT UUID(), NULL, 'glibenclamide', 'gliclazide', 'A10BB01', 'A10BB09', 'MAJOR',
    N'Phoi hop 2 sulfonylurea cung nhom (trung lap dieu tri) lam tang manh tac dung ha duong huyet, nguy co ha duong huyet nang keo dai.',
    N'Khong phoi hop 2 sulfonylurea. Chon 1 hoat chat duy nhat trong nhom; neu can tang hieu qua, phoi hop nhom khac co che (metformin, DPP-4i, SGLT2i).',
    'ESTABLISHED', 'Duoc thu Quoc gia VN; ADA Standards of Care'
WHERE NOT EXISTS (
    SELECT 1 FROM diab_his_cdss_ddi_pairs
    WHERE tenant_id IS NULL AND ingredient_a = 'glibenclamide' AND ingredient_b = 'gliclazide'
);
