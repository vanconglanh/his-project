-- ============================================================================
-- 9191_fix_drug_name_unify.sql
--
-- BUG-03: o chon thuoc khi ke don hien SAI ten / rong.
--   Bang diab_his_pha_drugs co 2 cot ten song song:
--     - `name`    (bo chuan 9005) : DUNG, day du 30/30 -> NGUON THAT.
--     - `name_vi` (bo cu 9010)    : rong 28/30, 2 dong TH001/TH002 con du lieu
--                                   test rac ("Paracetamol ... (HIEN moi CN)",
--                                   "Amoxicillin ... (se AN o CN2)").
--   Duong GHI ghi vao `name`, duong DOC lai doc `name_vi` -> lech pha:
--     - 28 thuoc hien ten RONG (name_vi NULL)
--     - Metformin (TH001) hien nham ten "Paracetamol ..." -> nguy hiem an toan nguoi benh.
--
--   Code da sua: TOAN BO duong doc/tim kiem + duong ghi deu dung cot `name`.
--   Migration nay DONG BO 1 CHIEU `name` -> `name_vi` cho MOI thuoc de 2 cot khong
--   con lech (don ca du lieu rac TH001/TH002), phong cho bat ky cho nao con doc name_vi.
--
--   An toan: `name` la nguon that va da day du -> ghi de name_vi khong lam mat du lieu
--   hien thi dung nao. Idempotent: chay nhieu lan cho cung ket qua.
-- ============================================================================
SET NAMES utf8mb4;

UPDATE diab_his_pha_drugs
SET name_vi = name, updated_at = NOW()
WHERE name IS NOT NULL AND name <> ''
  AND (name_vi IS NULL OR name_vi <> name);
