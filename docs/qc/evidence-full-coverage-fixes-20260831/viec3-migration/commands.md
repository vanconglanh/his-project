# Việc 3 — Migration chain: lệnh dùng & bằng chứng

## Môi trường
- Docker Desktop, image `mysql:8.0.36`, container throwaway riêng: `prodiab_mig_test`
- charset/collation server: `utf8mb4` / `utf8mb4_0900_ai_ci`
- Base dump: `db/diab_his_*.sql` (64 file, READ-ONLY) — nạp sau khi lọc bỏ dòng `SET @@GLOBAL.GTID_PURGED`
- Migrations: `db/migrations/*.sql` (141 file) — apply theo thứ tự tên file (`ls | sort`)

## Lệnh dựng container sạch
```bash
docker rm -f prodiab_mig_test
docker run -d --name prodiab_mig_test -e MYSQL_ROOT_PASSWORD=root_dev \
  mysql:8.0.36 --character-set-server=utf8mb4 --collation-server=utf8mb4_0900_ai_ci
# chờ sẵn sàng
until docker exec prodiab_mig_test mysqladmin ping -uroot -proot_dev 2>/dev/null | grep -q alive; do sleep 2; done
```

## Chạy toàn bộ chain (harness `run_mig.sh` trong thư mục này)
```bash
bash run_mig.sh <thu_muc_log>
```
Harness thực hiện:
1. `DROP DATABASE IF EXISTS prodiab_his; CREATE DATABASE ... utf8mb4_0900_ai_ci;`
2. Nạp lần lượt `diab_his_*.sql` — mỗi file `grep -v 'SET @@GLOBAL.GTID_PURGED' | mysql ...` (xử lý bẫy GTID)
3. Apply lần lượt `migrations/*.sql`, ghi lỗi từng file (nguyên văn message + tên file) vào `summary.log`

## Bẫy GTID_PURGED
Base dump chứa `SET @@GLOBAL.GTID_PURGED=...` — trên server gtid_mode=OFF sẽ lỗi. Đã lọc bỏ dòng này trước khi nạp (KHÔNG sửa file dump gốc).

## Kết quả
| Lần chạy | Thư mục log | Kết quả |
|---|---|---|
| Trước khi sửa | `before/summary.log` | 30 FAIL / 141 |
| Sau khi sửa (reload DB) | `after/summary.log` | 0 FAIL / 141 |
| Fresh volume (container mới hoàn toàn) | `final-fresh-volume/summary.log` | **OK=141 FAIL=0** |

## Sanity check schema cuối
```
BASE TABLE = 126, VIEW = 15
diab_his_sec_permissions      = 180
diab_his_sec_role_permissions = 296
diab_his_dict_icd10           = 15532
diab_his_dict_drug_units      = 12
diab_his_sch_appointments     = 10 (demo)
```
