# Runbook — Dựng môi trường test E2E bằng Docker Desktop (máy mới)

> Mục tiêu: dựng lại **full stack** trên máy có Docker Desktop để chạy **E2E CRUD + clinic-sim** (đặc biệt xác nhận **fix cấp phát/thu tiền — bug #6, migration 9025**).
> Cập nhật: 03/07/2026. Nhánh `main` đã có commit `a51d477` (rework Dispensing + `9025`).

---

## 0. Trạng thái đã verify trước khi chuyển máy (03/07)

| Hạng mục | Kết quả |
|---|---|
| Backend build | 0 error · 56 unit test pharmacy PASS |
| Migration `9025` áp DB live | 6 cột INT→CHAR(36)/VARCHAR(36) OK, idempotent |
| **Dispense HTTP E2E thật** | `POST /pharmacy/dispense/{id}` → **201 DISPENSED**, FEFO chọn lô, **trừ tồn 1000→990**, ghi dispense_records/items/movements, re-dispense bị chặn |
| Bộ E2E chung | **27 PASS** (Patient/Encounter/Reception/Reports/patient-journey…) |
| clinic-sim đa bệnh nhân | **Flaky** trên dev-server (Next.js hydration mismatch + selector 15s timeout ở bước tiếp đón) — bệnh nhân chưa tới bước cấp phát |

→ **Việc cần làm trên máy mới:** chạy **clinic-sim full 50 BN + 10 ngoại lệ trên production build** để có kết quả ổn định, và **re-run bộ 56 CRUD** để cập nhật lại con số 76.8% (snapshot R7 cũ).

---

## 1. Yêu cầu máy mới
- Docker Desktop (compose v2)
- .NET SDK 8 (nếu chỉ có .NET 10 → dùng `DOTNET_ROLL_FORWARD=Major`)
- Node.js 18+ · `npm`
- `git`

## 2. Lấy code
```bash
git clone <repo> pro-diab-his && cd pro-diab-his
git checkout main    # đã có a51d477: dispense fix + migration 9025
```

## 3. Dựng hạ tầng (Docker Desktop)
```bash
docker compose -f ops/docker-compose.yml up -d mysql redis minio
# MySQL: container prodiab-mysql · user root / pass root_dev · DB prodiab_his · :3306
```

## 4. Áp schema + migrations + seed (QUAN TRỌNG)
Chạy migrator container (tự áp dump → migrations → seeds):
```bash
docker compose -f ops/docker-compose.yml up migrator
# hoặc thủ công: bash ops/scripts/apply-migrations.sh  (DB_USER=root DB_PASS=root_dev)
```
Seed tenant test **DIAB-TEST** (tenant_id=2):
```bash
docker exec -i prodiab-mysql mysql -uroot -proot_dev prodiab_his < db/seeds/diab_test_tenant.sql
```

**Kiểm chứng bắt buộc:**
```sql
-- (a) migration 9025 đã áp? 6 cột phải là char(36)/varchar(36), KHÔNG còn int
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA='prodiab_his' AND (
 (TABLE_NAME='diab_his_pha_dispense_records'  AND COLUMN_NAME IN ('prescription_id','warehouse_id')) OR
 (TABLE_NAME='diab_his_pha_dispense_items'    AND COLUMN_NAME='drug_id') OR
 (TABLE_NAME='diab_his_pha_stock_movements'   AND COLUMN_NAME IN ('stock_id','warehouse_id','reference_id')));

-- (b) seed tenant 2: kỳ vọng users=7, rooms=4, drugs=8, patients=15
SELECT (SELECT COUNT(*) FROM diab_his_sec_users WHERE tenant_id=2) users,
       (SELECT COUNT(*) FROM diab_his_pha_drugs WHERE tenant_id=2) drugs,
       (SELECT COUNT(*) FROM diab_his_pat_patients WHERE tenant_id=2) patients;
```

## 5. Backend `:5000`
```bash
cd backend/src/ProDiabHis.Api
# Nếu chỉ có .NET 10: export DOTNET_ROLL_FORWARD=Major
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 dotnet run -c Debug
```
Verify: `curl -X POST http://localhost:5000/api/v1/auth/login -H "Content-Type: application/json" -d '{"email":"admin@prodiab.local","password":"admin123"}'` → **200 + accessToken**.

## 6. Frontend
```bash
cd frontend && npm install
# DEV (nhanh, nhưng sim có thể flaky do hydration):
npm run dev                 # :3000
# KHUYẾN NGHỊ cho sim ổn định — PRODUCTION build:
npm run build && npm run start   # :3000
```
`.env.local` phải có `NEXT_PUBLIC_API_BASE_URL=http://localhost:5000`.

> ⚠️ **Cạm bẫy port :3000:** trên máy cũ, `:3000` bị một app khác (Mantis/"Smartwood") chiếm → HIS phải chạy `:3100` và E2E phải set `BASE_URL=http://localhost:3100`. **Máy mới sạch** thì `:3000` OK, không cần override. Luôn kiểm tra `curl -s localhost:3000 | grep -o '<title>[^<]*'` phải ra `Pro-Diab HIS`.

## 7. Chạy E2E
```bash
cd frontend
# Bộ CRUD/E2E chung (default BASE_URL=:3000; thêm BASE_URL nếu FE ở port khác)
ADMIN_PASSWORD=admin123 npm run test:e2e

# Clinic-sim — SMOKE 10 BN trước:
SIM_USE_ADMIN=1 ADMIN_PASSWORD=admin123 SIM_PATIENTS=10 npm run test:sim
# PASS → full 50 BN + 10 ngoại lệ (bỏ SIM_PATIENTS):
SIM_USE_ADMIN=1 ADMIN_PASSWORD=admin123 npm run test:sim
```

## 8. Credentials (mật khẩu tất cả = `admin123`)
| Tenant | Tài khoản |
|---|---|
| 1 (demo) | `admin@prodiab.local`, `bacsi1@prodiab.local` |
| 2 (DIAB-TEST) | `admin.test@diabtest.local`, `letan.test@`, `bacsi.test@`, `bacsi2.test@`, `duocsi.test@`, `ketoan.test@`, `ktv.test@` `diabtest.local` |

- `SIM_USE_ADMIN=1` → mọi bước dùng `admin.test` (bỏ qua RBAC seed mismatch tạm thời).
- Trên Docker, `crud-actions.spec.ts › unlockAdmin` gọi `docker exec prodiab-mysql -uroot -proot_dev` → **sẽ hoạt động** (máy cũ không có docker nên nó skip).

## 9. Kịch bản cần soi kỹ (xác nhận fix bug #6)
- **Ngoại lệ 1 — Hết thuốc:** phát Gliclazide (lô `T-GL01` tồn=3) → kỳ vọng `422 PHARMACY_STOCK_INSUFFICIENT`.
- **Ngoại lệ 3 — FEFO/cận HSD:** Insulin Glargine 2 lô → FEFO chọn lô hết hạn sớm trước.
- Luồng chuẩn: kê đơn → ký ĐTQG → **Dược cấp phát (FEFO auto)** → trừ tồn theo lô → **Thu ngân**.

## 10. Fail đã biết — KHÔNG chặn (đừng nhầm là regression)
- 4 × `auth.spec` (<1s): UI-label/mock assertion drift.
- 1 × `reports-cohort-fix` visual (screenshot).
- clinic-sim flaky ở tiếp đón khi chạy **dev-server** → dùng **production build** (mục 6).

## 11. Ghi chú khác
- Repo SDK `pro-diab-sdk`: commit đồng bộ tài liệu v0.2.1 (`db8e6ec`) đang **ở local, CHƯA push** — quyết định push riêng nếu cần.
- File này là source of truth cho việc dựng test; cập nhật khi quy trình đổi.
