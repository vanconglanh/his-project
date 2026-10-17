# Script đăng ký client SSO trên IdentityServer

Dùng cho tính năng SSO DiaB Portal → HIS (+ TimeSheet). Thiết kế: `docs/api/sso-design-20261017.md`.

## Hạ tầng đã xác minh (2026-10-17, qua SSH VNG)

| Hạng mục | Giá trị thật |
|---|---|
| IS staging container | `stg-uat-is` → domain `is.staging.diab.com.vn` |
| IS dev container | `dev-is` → domain `is.dev.diab.vn` |
| IS production container | `pro-diab-is` → `is.diab.com.vn` / `is.diab.vn` (KHÔNG ĐỤNG) |
| DB ConfigurationStore staging+dev | SQL Server `dev-db-mssql,1433`, catalog **`DiaB_Identity_Server`**, user `stg_app` (staging + dev DÙNG CHUNG) |
| **Issuer thật (claim `iss`)** | **`https://is.stg.savvycom.asia`** — KHÁC host `is.staging.diab.com.vn`. Backend validate `ValidIssuer` = `issuer` lấy từ discovery, KHÔNG dùng Authority. |
| Client cache | KHÔNG bật (`AddClientStoreCache` comment) → INSERT có hiệu lực NGAY, không cần restart IS |
| DataProtection key IS | **KHÔNG persist trên disk** (R8) → TUYỆT ĐỐI không restart IS |

## File

| File | Mục đích |
|---|---|
| `is-register-clients-staging.sql` | INSERT `prodiab-his-staging`, `prodiab-his-dev`, `diab-timesheet-staging` vào `DiaB_Identity_Server`. Idempotent. |
| `is-register-clients-staging-rollback.sql` | Gỡ đúng 3 client trên. |
| `is-register-clients-prod.sql` | **CHỜ USER DUYỆT.** INSERT `prodiab-his`, `diab-timesheet` vào IS production. |
| `is-register-clients-prod-rollback.sql` | Gỡ client production. |

## Secret

Secret plaintext KHÔNG nằm trong git. File SQL chỉ chứa HASH SHA256-base64 (chuẩn IS4 `SharedSecret`).
Plaintext lưu trên server HIS tương ứng tại file env quyền `600` (ví dụ `/srv/apps/stg/his/backend.sso.env`), nạp vào biến `Sso__ClientSecret`. Không in ra log, không echo.

## Quy trình chạy (staging) — GS1

```bash
# 1. Backup bảng cấu hình IS trước (BẮT BUỘC)
PW=<password stg_app>   # lấy từ /srv/apps/stg/apps/config/uat-is.json DefaultConnection
docker exec -e SQLCMDPASSWORD="$PW" dev-db-mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U stg_app -C -d DiaB_Identity_Server -Q \
  "SELECT * INTO Clients_bak_<ts> FROM Clients; SELECT * INTO ClientSecrets_bak_<ts> FROM ClientSecrets; \
   SELECT * INTO ClientGrantTypes_bak_<ts> FROM ClientGrantTypes; SELECT * INTO ClientScopes_bak_<ts> FROM ClientScopes; \
   SELECT * INTO ClientRedirectUris_bak_<ts> FROM ClientRedirectUris; \
   SELECT * INTO ClientPostLogoutRedirectUris_bak_<ts> FROM ClientPostLogoutRedirectUris;"

# 2. Ghi jwks trước để đối chiếu (GS7)
docker exec stg-uat-is wget -qO- http://localhost/.well-known/openid-configuration/jwks > jwks_before.json

# 3. Chạy script (copy file vào container rồi -i)
docker cp is-register-clients-staging.sql dev-db-mssql:/tmp/
docker exec -e SQLCMDPASSWORD="$PW" dev-db-mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U stg_app -C -d DiaB_Identity_Server -i /tmp/is-register-clients-staging.sql

# 4. Kiểm GS1: jwks KHÔNG đổi, discovery không đổi kid, portal vẫn đăng nhập (user tự reload portal)
docker exec stg-uat-is wget -qO- http://localhost/.well-known/openid-configuration/jwks > jwks_after.json
diff jwks_before.json jwks_after.json   # phải rỗng
```

Rollback: chạy `is-register-clients-staging-rollback.sql` cùng cách. Không restart IS.

## Lưu ý

- `diab-timesheet-staging`: domain (`api.timesheet.diab.vn` / `portal.timesheet.diab.vn`) cần DevOps xác nhận trước khi dùng; TimeSheet pha này CHƯA deploy SSO.
- Production (`is-register-clients-prod.sql`): xác nhận tên catalog IS production (pro-db-mssql) + domain HIS prod (Q6: his.diab.com.vn vs his.diab.vn) trước khi chạy; **chỉ chạy sau khi user duyệt**.
