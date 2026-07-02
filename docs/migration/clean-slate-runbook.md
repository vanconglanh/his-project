# Clean Slate Runbook — Phase 1 Migration (Direction B)

- **Phiên bản**: 1.0
- **Ngày**: 2026-05-29
- **Phụ trách thực thi**: Thảo (BE) + Chương (DevOps)
- **Tham chiếu**: ADR 0007, ERD v2, plan `C:\Users\ADMIN\.claude\plans\hi-n-t-i-c-report-jaunty-cherny.md`
- **Mục tiêu Phase 1**: trong 1 ngày, đưa database `prodiab_his` về trạng thái sạch — chỉ còn bảng prefix `diab_his_*` theo ERD v2, có sẵn master data + demo data để Phase 2/3 chạy.

---

## 0. Chuẩn bị (15 phút)

- [ ] Lưu trữ dump hiện tại (snapshot phòng hờ): `docker exec prodiab-mysql mysqldump -u root -p prodiab_his > backup_pre_clean_slate_$(date +%Y%m%d).sql`.
- [ ] Thông báo team freeze branch `dev` trong 1 ngày.
- [ ] Verify file `db/migrations/0000_helpers.sql` đã có stored proc `add_col_if_missing` (dùng cho idempotency MySQL 8).
- [ ] Build helper hash BCrypt cost 12 cho seed admin (`/tmp/hashgen/Program.cs` — đã verify session trước).

---

## 1. Thứ tự chạy 9 migration

| # | File | Phụ thuộc vào | Mô tả |
|---|---|---|---|
| 1 | `9000_drop_legacy.sql` | (none) | `SET FOREIGN_KEY_CHECKS=0`, drop mọi bảng KHÔNG có prefix `diab_his_*` + drop hangfire legacy. |
| 2 | `9001_create_sec_all.sql` | 9000 | Tạo 7 bảng module `sec` (users, roles, permissions, user_roles, role_permissions, sessions, audit_logs). |
| 3 | `9002_create_patient.sql` | 9001 (FK created_by) + sys_tenants đã tồn tại từ migration `0001` | Tạo 5 bảng `pat_*`. |
| 4 | `9003_create_encounter.sql` | 9002 + 9006 (cần `sys_clinics`, `sys_rooms`) | Tạo 4 bảng `enc_*`. **Phải chạy SAU 9006**. |
| 5 | `9004_create_labrad.sql` | 9003 | Tạo 5 bảng `lab_*` + `rad_*` + `cls_uploads`. |
| 6 | `9005_create_pharmacy.sql` | 9001 + 9003 | Tạo 8 bảng `pha_*`. |
| 7 | `9006_create_clinic.sql` | 9001 | Tạo 3 bảng `sys_clinics`, `sys_branches`, `sys_rooms`. **Lưu ý: vì 9003/9005 cần FK tới clinic/room, đảo thứ tự thực thi → 9001 → 9006 → 9002 → 9003 → 9004 → 9005**. Giữ tên file theo group, runner đọc dependency từ header comment `-- DEPENDS: 9001`. |
| 8 | `9007_seed_master_data.sql` | 9001–9006 | Seed: 60 permissions, 6 roles chuẩn (Admin/BacSi/LeTan/DuocSi/KeToan/KyThuatVien), ~100 mã ICD-10 nội tổng quát + tiểu đường, ATC drug units, 50 service catalog, role-permission mapping. |
| 9 | `9008_seed_demo.sql` | 9007 | Seed: 1 tenant `dIaB`, 1 clinic, 2 branches, 5 rooms, 1 admin user (`admin@prodiab.local` / hash của `admin123`), 10 patients, 20 encounters, 30 billings, 50 prescriptions, 100 stock movements. |

**Thứ tự thực thi cuối cùng**: `9000 → 9001 → 9006 → 9002 → 9003 → 9004 → 9005 → 9007 → 9008`.

> Runner mặc định trong repo chạy theo lexical order (`9000`, `9001`, `9002`, …). Để tránh đảo lộn convention, **đổi tên file** ngay khi commit:
> - `9001_create_sec_all.sql`
> - `9002_create_sys_clinic.sql`  ← (chứa nội dung "9006" ở plan gốc)
> - `9003_create_patient.sql`
> - `9004_create_encounter.sql`
> - `9005_create_labrad.sql`
> - `9006_create_pharmacy.sql`
> - `9007_seed_master_data.sql`
> - `9008_seed_demo.sql`
>
> Plan gốc dùng số thứ tự logic theo module; runbook này dùng số thứ tự thực thi vật lý. Backend (Thảo) tự do đổi nhưng phải nhất quán header `-- DEPENDS:`.

---

## 2. Verify checklist sau mỗi file

### Sau `9000_drop_legacy.sql`
```sql
SELECT COUNT(*) FROM information_schema.tables
WHERE table_schema='prodiab_his' AND table_name NOT LIKE 'diab_his_%';
-- Kỳ vọng: 0 (hoặc chỉ còn migration history nếu repo dùng `__EFMigrationsHistory`)
```
- [ ] Adminer `localhost:8080` → DB `prodiab_his` → list tables: chỉ thấy bảng `diab_his_sys_tenants` (đã tồn tại từ migration `0001`) + bảng tracking nếu có.

### Sau `9001_create_sec_all.sql`
- [ ] Count = 7 bảng prefix `diab_his_sec_*`.
- [ ] Verify schema 1 bảng: `DESCRIBE diab_his_sec_users;` — phải có đủ 6 cột audit chuẩn theo ADR 0007.
- [ ] Verify index: `SHOW INDEX FROM diab_his_sec_users WHERE Key_name='idx_diab_his_sec_users_tenant';` → có 1 row.

### Sau `9002_create_sys_clinic.sql`
- [ ] Count `diab_his_sys_*` = 4 (tenants + clinics + branches + rooms).
- [ ] FK: `SELECT * FROM information_schema.referential_constraints WHERE constraint_schema='prodiab_his' AND constraint_name LIKE 'fk_diab_his_sys_%';` → 3 rows (clinic→tenant, branch→clinic, room→branch).

### Sau `9003_create_patient.sql`
- [ ] Count `diab_his_pat_*` = 5.
- [ ] Verify cột mã hóa: `id_card_enc VARBINARY(512)` trong `diab_his_pat_patients`, `card_number_enc VARBINARY(512)` trong `diab_his_pat_insurances`.

### Sau `9004_create_encounter.sql`
- [ ] Count `diab_his_enc_*` = 4.
- [ ] Verify FK encounter → patient + encounter → clinic + encounter → user(doctor).

### Sau `9005_create_labrad.sql`
- [ ] Count: `lab_*`=2, `rad_*`=2, `cls_uploads`=1 → tổng 5.
- [ ] Verify `diab_his_cls_uploads` có 2 FK nullable tới lab_orders và rad_orders.

### Sau `9006_create_pharmacy.sql`
- [ ] Count `diab_his_pha_*` = 8.
- [ ] `diab_his_pha_stock` có index `idx_diab_his_pha_stock_drug_lot` trên `(drug_id, lot_no)`.

### Sau `9007_seed_master_data.sql`
```sql
SELECT COUNT(*) FROM diab_his_sec_permissions;            -- ≥ 60
SELECT COUNT(*) FROM diab_his_sec_roles WHERE is_system=1; -- = 6
SELECT COUNT(*) FROM diab_his_sec_role_permissions;        -- ≥ 200
SELECT COUNT(*) FROM diab_his_bil_services;                -- ≥ 50
```
- [ ] Verify 6 role code: `admin`, `doctor`, `receptionist`, `pharmacist`, `accountant`, `technician`.
- [ ] Verify role `admin` map tới TẤT CẢ permissions.

### Sau `9008_seed_demo.sql`
```sql
SELECT COUNT(*) FROM diab_his_sys_tenants WHERE code='dIaB';      -- = 1
SELECT COUNT(*) FROM diab_his_sec_users WHERE email='admin@prodiab.local'; -- = 1
SELECT COUNT(*) FROM diab_his_pat_patients;     -- = 10
SELECT COUNT(*) FROM diab_his_enc_encounters;   -- = 20
SELECT COUNT(*) FROM diab_his_bil_billings;     -- = 30
```
- [ ] Test login: `POST /api/v1/auth/login` body `{"email":"admin@prodiab.local","password":"admin123"}` → 200 OK + JWT.
- [ ] JWT payload chứa `tenant_id`, `clinic_id`, `user_id`, `roles=["admin"]`, permissions ≥ 60.

---

## 3. Verify tổng thể sau Phase 1

```sql
-- Tổng số bảng diab_his_*
SELECT COUNT(*) FROM information_schema.tables
WHERE table_schema='prodiab_his' AND table_name LIKE 'diab_his_%';
-- Kỳ vọng: ≥ 53 (theo ERD v2) + hangfire ~6 bảng nếu re-init
```

- [ ] `docker compose ps` → tất cả service `healthy`.
- [ ] `dotnet build backend/ProDiabHis.sln` → 0 error, warning về EF entity chưa có config sẽ xử lý Phase 2.
- [ ] Hangfire dashboard `/hangfire` accessible (đã re-init schema).
- [ ] Audit log empty: `SELECT COUNT(*) FROM diab_his_sec_audit_logs;` = 0.

---

## 4. Rollback procedure

Nếu bất kỳ verify nào FAIL không khắc phục được trong 30 phút:

### Mức nhẹ — Rollback 1 file
1. Identify file FAIL.
2. Drop tay các bảng đã tạo bởi file đó: `DROP TABLE IF EXISTS diab_his_<group>_<entity>;` (tra trong file SQL).
3. Sửa file, re-run.

### Mức nặng — Reset toàn bộ Phase 1
1. **Tắt backend + frontend trước** để tránh request đang chạy.
   ```powershell
   docker compose -f ops/docker-compose.yml stop backend frontend
   ```
2. **Drop volume MySQL** (xóa sạch dữ liệu):
   ```powershell
   docker compose -f ops/docker-compose.yml down -v
   ```
3. **Restart compose từ đầu**:
   ```powershell
   docker compose -f ops/docker-compose.yml up -d mysql redis minio
   ```
4. Chờ MySQL healthy (~30s), apply lại từ migration `0000_helpers.sql` → `0001_create_tenants.sql` → `9000_drop_legacy.sql` → … → `9008_seed_demo.sql`.
5. Restore backup nếu cần dữ liệu cũ: `docker exec -i prodiab-mysql mysql -u root -p prodiab_his < backup_pre_clean_slate_YYYYMMDD.sql`.

### Mức cực nặng — Revert ADR
Nếu sau 2 ngày Phase 1 vẫn không pass verify:
- Revert commit ADR 0007, quay lại Direction A hoặc C theo discussion lại với PO Đăng.
- Khôi phục backup pre-clean-slate (bước 0).

---

## 5. Bàn giao sang Phase 2

Sau khi tất cả checklist mục 2-3 PASS:
- [ ] Commit migration files với message: `feat(db): clean slate schema theo ADR 0007 — 9000-9008`.
- [ ] Update `docs/migration/CHANGELOG.md` ghi version `v2.0.0 — Direction B clean slate`.
- [ ] Tag DB schema version: `INSERT INTO __schema_version VALUES ('v2.0.0', NOW(), 'direction-b-clean-slate');` (tạo bảng nếu chưa có).
- [ ] Ping Thảo bắt đầu Phase 2 (9 EF Configuration cho entity chưa có config).
- [ ] Ping Phượng review test plan E2E (Phase 6) dựa trên 10 patients + 20 encounters demo data.

---

## 6. Liên hệ

- **Block kỹ thuật MySQL**: Chương (DevOps).
- **Block schema/FK**: Lành (architect — file này).
- **Block business rule trong seed**: Đăng (PO).
- **Approval go**: Đăng + Chương.
