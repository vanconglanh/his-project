# Deploy per-clinic — Generator

Mỗi phòng khám = **1 stack Docker + 1 DB riêng, độc lập**. Quy trình: khách trả lời bộ câu hỏi →
generator sinh bộ deploy → `docker compose up`.

## Quy trình

```
1. Điền câu trả lời         →  deploy/answers/<clinic>.json  (theo answers.schema.json)
2. Sinh bộ deploy           →  prodiab-clinic-gen --answers deploy/answers/<clinic>.json
                               → deploy/deployments/<code>/{.env, docker-compose.yml, nginx.conf, migrate.sh, seed.sql}
3. Deploy                   →  cd deploy/deployments/<code> && docker compose up -d --build
4. Nginx host ngoài proxy   →  <domain> → 172.17.0.1:<nginx_port>  (SSL ở nginx host ngoài)
```

## Bộ câu hỏi & schema
- [onboarding-questions.md](onboarding-questions.md) — bộ câu hỏi đầy đủ (nhóm A–M + E6), đánh dấu bắt buộc/tùy chọn.
- [answers.schema.json](answers.schema.json) — JSON Schema validate câu trả lời.
- [answers.example.json](answers.example.json) — mẫu điền sẵn.

## Generator (`prodiab-clinic-gen`)
Nguồn: `backend/src/ProDiabHis.ClinicGen`. Chạy:
```
dotnet run --project backend/src/ProDiabHis.ClinicGen -- --answers deploy/answers/<clinic>.json
```
Tham số: `--answers <path>` · `--out <dir>` (mặc định `deploy/deployments`) · `--repo-root <path>`.

Generator làm gì:
- **Tự sinh secret** (DB/JWT/Encryption/Minio/Redis) — giữ ổn định khi chạy lại (đọc `.env` cũ, không xoay secret).
- **Tự cấp `nginx_port`** (8090, 8091... tránh trùng) nếu answers không chỉ định.
- Render `.env` / `docker-compose.yml` / `nginx.conf` từ `deploy/template/`.
- Sinh `seed.sql` (tenant + user admin/nhân sự + role + dịch vụ + feature flags) — idempotent (UUID tất định + INSERT IGNORE).
- Cập nhật `deploy/deployments/registry.json` (danh sách clinic — cho super-admin).

## Stack sinh ra (mỗi clinic)
`mysql` + `migrator` (one-shot: apply 9xxx trừ demo + seed.sql) + `redis` + `minio` + `backend` + `frontend` + `nginx`.
Backend chờ `migrator` xong (`service_completed_successfully`) mới start. Tên container/network/DB đều prefix `prodiab-<code>`.

## ⚠️ Lưu ý
- `deploy/deployments/` chứa **SECRET** → đã gitignore, KHÔNG commit.
- Frontend `NEXT_PUBLIC_API_BASE_URL` bake lúc build → mỗi domain build lại image (image tag `prodiab-<code>-frontend`).
- Phần **profile bác sĩ (CCHN/chữ ký số), giờ làm việc, finance (HĐĐT/bank), kho dược khởi tạo** cần schema mới
  (bảng `his_provider_profile`, `his_provider_schedule`, `his_appointment_setting`, cột finance) — **giai đoạn sau**;
  hiện seed chỉ phủ phần schema đang có (đủ để đăng nhập + khám + kê đơn cơ bản).
