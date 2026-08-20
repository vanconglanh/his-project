# ADR-0008 — Chien luoc sinh baseline schema (`db/baseline/0000_baseline_schema.sql`)

- **Trang thai:** Accepted (quyet dinh phuong an) / **Chua thi hanh** (thieu dau vao, xem muc 5)
- **Ngay:** 2026-08-21
- **Tac gia:** Lành (architect)
- **Nhanh:** `develop`
- **Lien quan:** `db/migrations/README.md` muc 5, `db/migrations/APPLY_ORDER.md`,
  `docs/architecture/canonical-table-names.md` (REV-2)

---

## 1. Boi canh

Dung DB moi tu so 0 hien **khong chay duoc**: nap 64 file dump goc roi apply 150 file
`db/migrations/*.sql` tren MySQL 8 sach → **30/150 file loi** (da kiem chung that, DevOps
2026-08-20). Nguyen nhan goc: hai the he migration chong nhau quanh moc `9000_drop_legacy.sql`,
cung tao bang cho cung nghiep vu duoi hai ten khac nhau.

Production **dang chay on dinh voi 150 bang**, 225 benh nhan, 178 luot kham. Tuc la:
**schema dung ton tai that tren production, chi la khong tai tao lai duoc tu repo.**

## 2. Van de can quyet

Sinh `db/baseline/0000_baseline_schema.sql` bang cach nao de DB moi dung len **giong
production 100%**?

## 3. Cac phuong an

### A. Hop nhat thu cong tu 150 file migration
Doc 150 file, gom `CREATE TABLE` + `ALTER` cuoi cung cua tung bang, viet lai thanh 1 file.

- (+) Khong can truy cap production.
- (−) **Khong the dam bao dung.** Phai tai dung thu cong ket qua cua ~500 lenh ALTER trong do
  30 file von da loi giua chung — nghia la trang thai that cua nhieu bang **khong suy ra duoc
  tu repo**, no phu thuoc vao lenh nao da chay thanh cong tren production truoc day.
- (−) Buoc verify (d) — so danh sach bang voi 150 bang production — gan nhu chac chan lech, va
  moi lan lech lai phai doan tiep.
- (−) Rui ro cao nhat: file **trong nhu chuan** nhung sai cot ⇒ DB moi chay duoc, test pass,
  nhung lech production ⇒ loi chi lo ra khi go-live tenant moi.

### B. `mysqldump --no-data` tu production, roi chuan hoa (**CHON**)
Xuat DDL that cua ca 150 bang tu production (read-only, khong khoa bang), commit lam baseline.

- (+) **Dung 150/150 bang theo dinh nghia** — buoc verify (d) dat *by construction*, khong con
  cho de doan.
- (+) Lay duoc chinh xac kieu cot, index, FK, collation, AUTO_INCREMENT, comment.
- (+) Thao tac read-only, khong dung vao du lieu.
- (−) Bao gom ca bang deprecated (`cli_lab_orders`, `cli_allergies`, `ref_icd10`…) → xu ly o muc 4.
- (−) Can 1 lan truy cap production co kiem soat.

### C. EF `GenerateCreateScript()`
- (−) Chi phu **58/150 bang**. Loai ngay; chi dung de doi chieu.

## 4. Quyet dinh

**Chon B.** Baseline la **anh chup DDL production da chuan hoa**, khong phai san pham suy dien
tu chain migration.

### 4.1 Lenh sinh (read-only, agent cha chay qua SSH)

```bash
mysqldump --no-data --skip-add-drop-table --skip-comments \
          --single-transaction --set-gtid-purged=OFF \
          --routines=FALSE --triggers=FALSE --events=FALSE \
          -h <host> -u <ro_user> -p diab_his \
  > /tmp/baseline_raw.sql
# doi chieu bat buoc: phai ra dung 150
grep -c '^CREATE TABLE' /tmp/baseline_raw.sql
```

Ghi chu: `--set-gtid-purged=OFF` la bat buoc — bay GTID_PURGED da duoc ghi nhan trong
`APPLY_ORDER.md`. `--no-data` dam bao **khong xuat mot dong du lieu benh nhan nao**.

### 4.2 Chuan hoa sau khi xuat (architect lam trong repo)

1. Bo `AUTO_INCREMENT=<n>` con lai tren tung `CREATE TABLE` (reset ve 1 cho DB moi).
2. Doi `CREATE TABLE` → `CREATE TABLE IF NOT EXISTS` (yeu cau idempotent, buoc verify (b)).
3. Sap xep lai thu tu: bang khong co FK truoc, hoac don gian hon — boc toan file bang
   `SET FOREIGN_KEY_CHECKS=0;` … `SET FOREIGN_KEY_CHECKS=1;`.
4. `SET NAMES utf8mb4;` dau file.
5. **Giu nguyen ca 150 bang, KE CA bang deprecated.** Ly do: baseline phai bang voi production
   de buoc (d) so khop tuyet doi va de migration sau moc van apply duoc. Bang deprecated chi
   duoc **danh dau bang `COMMENT='DEPRECATED (ADR-0008) - canonical: <ten>'`**, dung theo dung
   ten chuan da chot o `canonical-table-names.md` REV-2. Viec DROP that su la mot migration
   rieng, sau khi code da chuyen het (khong nam trong ADR nay).
6. Khong kem seed du lieu. Seed danh muc (ICD-10 15 532 ma, permission…) tach thanh
   `db/baseline/0001_baseline_seed.sql`, sinh bang `mysqldump --no-create-info` chi cho cac
   bang `diab_his_dict_*` / `diab_his_sec_permissions` / `diab_his_sys_code_*`.

### 4.3 Moc cat (cut line)

Baseline chup **sau** migration `9120_merge_dispense_records.sql` (file cao nhat da commit).
Quy uoc:

- Moi file `db/migrations/*.sql` co so thu tu **≤ 9120** = **LEGACY**, da nam trong baseline,
  **khong apply lai** khi dung DB moi. Khong xoa file (yeu cau cua task), chi liet ke trong
  `db/baseline/README.md`.
- Migration moi tu nay danh so tu **9200** tro len, phai idempotent, va la chain duy nhat
  chay sau baseline.

### 4.4 Ranh gioi an toan

Baseline **chi dung dung DB MOI**. Canh bao in dam trong `db/baseline/README.md`.
Vi baseline chua `CREATE TABLE IF NOT EXISTS` (khong co DROP), apply nham len DB co du lieu se
la no-op chu khong pha du lieu — nhung van cam vi no lam sai lech nhan thuc ve trang thai schema.

## 5. Dau vao con thieu — ADR chua thi hanh duoc

Can agent cha chay lenh o muc 4.1 va cung cap:

1. `/tmp/baseline_raw.sql` (DDL 150 bang, khong co du lieu).
2. Ket qua `grep -c '^CREATE TABLE'` (ky vong: 150).
3. Danh sach 150 ten bang: `SELECT table_name FROM information_schema.tables
   WHERE table_schema='diab_his' AND table_type='BASE TABLE' ORDER BY table_name;`
   — dung lam **danh sach doi chieu cua buoc verify (d)**.
4. Danh sach VIEW (neu co): cung query voi `table_type='VIEW'` + `SHOW CREATE VIEW`.
   Muc 0 cua `canonical-table-names.md` cho biet `9009/9022/9061` co tao VIEW tuong thich —
   can biet VIEW nao con song de dua vao baseline.

## 6. He qua

- Baseline tro thanh **nguon su that duy nhat** cho schema; `db/diab_his_*.sql` (dump he thong
  cu) va dai migration ≤ 9120 tro thanh tai lieu lich su.
- 30 file migration loi **khong con can sua** — chung da nam ngoai chain apply. Day la loi ich
  lon nhat cua phuong an B so voi A.
- Doi lai: repo mat kha nang "tai dung lich su schema tu so 0". Chap nhan duoc, vi kha nang do
  **von da mat roi** (30/150 loi), ADR nay chi ghi nhan su that do.
