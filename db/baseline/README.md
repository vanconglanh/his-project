# Baseline schema — Pro-Diab HIS

> # ⚠️ CANH BAO — TUYET DOI KHONG APPLY LEN DB DANG CO DU LIEU
>
> **`0000_baseline_schema.sql` CHI dung de dung mot DATABASE MOI HOAN TOAN RONG.**
>
> **CAM** chay len production, staging, hay bat ky DB nao da co du lieu benh nhan.
> Truoc khi chay, bat buoc kiem tra DB dich rong:
> ```sql
> SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE();
> -- phai = 0. Neu > 0 thi DUNG LAI.
> ```

---

## Trang thai hien tai: **CHUA SINH DUOC**

File `0000_baseline_schema.sql` **chua ton tai**. Ly do va phuong an: xem
[`docs/adr/0008-baseline-schema-strategy.md`](../../docs/adr/0008-baseline-schema-strategy.md).

Tom tat: baseline phai duoc sinh bang `mysqldump --no-data` tu production (150 bang) roi chuan
hoa, **khong** hop nhat thu cong tu 150 file migration (30 file trong so do von da loi, nen
trang thai schema that khong suy ra duoc tu repo). Dang cho dau vao o ADR-0008 muc 5.

---

## Cach dung (sau khi file duoc sinh)

```bash
mysql -u root -p -e "CREATE DATABASE diab_his_new CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;"
mysql -u root -p diab_his_new < db/baseline/0000_baseline_schema.sql
mysql -u root -p diab_his_new < db/baseline/0001_baseline_seed.sql   # danh muc, khong co PII
# sau do chi apply migration tu 9200 tro len
```

## Moc cat (cut line)

| Dai file | Trang thai | Hanh dong khi dung DB moi |
|---|---|---|
| `db/diab_his_*.sql` (64 file dump he thong cu) | **LEGACY — lich su** | KHONG nap |
| `db/migrations/*.sql` so thu tu **≤ 9120** | **LEGACY — da nam trong baseline** | KHONG apply lai |
| `db/migrations/*.sql` so thu tu **≥ 9200** | **ACTIVE** | Apply theo thu tu ten file |

**Khong xoa file legacy nao** — giu lam ho so lich su. Chung chi bi loai khoi chain apply.

## Checklist verify bat buoc truoc khi merge baseline

| # | Buoc | Trang thai |
|---|---|---|
| a | Nap baseline → apply migration ≥ 9200 → 0 loi | ⬜ chua chay |
| b | Chay lai lan 2 → van 0 loi (idempotent) | ⬜ chua chay |
| c | `dotnet test ProDiabHis.IntegrationTests` tro vao DB tu baseline → PASS | ⬜ chua chay |
| d | So danh sach bang baseline vs 150 bang production → liet ke thieu/thua | ⬜ chua chay |

Query cho buoc (d):
```sql
SELECT table_name FROM information_schema.tables
WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE'
ORDER BY table_name;
```
So sanh voi danh sach 150 bang production (ADR-0008 muc 5.3).
**Ky vong: lech = 0 bang.** Neu lech, KHONG duoc merge.
