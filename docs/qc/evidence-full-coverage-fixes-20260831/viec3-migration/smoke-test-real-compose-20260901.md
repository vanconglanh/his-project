# Smoke test cuoi — dung DB SACH tu chuoi migration tren docker-compose THAT (2026-09-01)

## Quy trinh
1. `docker compose down -v` — xoa sach volume MySQL dev (user da xac nhan).
2. `docker compose up -d` (ops/docker-compose.yml) — MySQL moi + migrator container THAT
   (`ops/scripts/apply-migrations.sh`, khong phai container throwaway).
3. Migrator: **exited 0**, "Tat ca migrations da duoc apply. Hoan tat." — 211 file, 0 loi, 183 bang.
4. `up -d --build backend frontend` — rebuild backend tu source hien tai (co fix BUG-001 + Viec 1).

## Ket qua
- Login THAT: `bacsi.test@prodiab.test` -> HTTP 200, access token hop le (2260 ky tu).
  `admin@prodiab.local` -> 200 + yeu cau MFA setup (dung logic FR-1011, khong phai bug).
- **BUG-001 tren app that: GET /api/fhir/r4/metadata (KHONG token) -> 200.**
- Sweep endpoint chinh (token bac si): patients/encounters/billings/prescriptions/services/
  drugs/lab-results/appointments/dashboard/rooms/recall/reception-queue = **200**, KHONG con 500.

## LOI DRIFT PHAT HIEN + SUA (chi lo khi build DB sach tu migration)
- `GET /api/v1/prescriptions` -> **500** "Unknown column 'i.line_total'".
- Nguyen nhan: bang `diab_his_pha_prescription_items` bi 0035 (lop 00xx) tao TRUOC voi schema
  thieu cot -> `CREATE TABLE IF NOT EXISTS` o 9005 thanh no-op -> thieu 6 cot ma entity EF +
  code can (drug_name, drug_strength, unit, unit_price, line_total, bhyt_applicable).
  Integration test khong bat vi dung EnsureCreated (tao du cot tu entity).
- Fix: migration moi `9192_fix_prescription_items_missing_cols.sql` (add_col_if_missing, idempotent).
- Sau fix: prescriptions -> 200.

## Ha tang khac fix trong dot nay
- `.gitattributes`: them `*.sql text eol=lf` (truoc chi co `*.sh`). CRLF tren Windows lam
  hong DELIMITER stored proc + script migrator trong container Linux. Normalize LF working copy.
