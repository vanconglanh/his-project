# Triển khai Portal bệnh nhân (portal-client)

App bệnh nhân là **container riêng**, phục vụ mọi phòng khám qua **wildcard subdomain**
`*.diab.com.vn`. Backend tự resolve phòng khám theo `Host` header → `diab_his_sys_tenants.subdomain`
(kiến trúc 1 DB dùng chung + lọc `tenant_id`). Không cần deploy riêng mỗi phòng khám.

> Lưu ý: nếu hệ thống chuyển sang **DB-per-clinic** (mảng "per-clinic generator" đang làm),
> cần thống nhất lại: khi đó tenant + connection resolve theo Host ở tầng hạ tầng, và portal
> chỉ cần bỏ bước `ResolvePortalTenantQuery` (middleware hạ tầng lo). Xem `PortalTenantResolveHandlers.cs`.

## 1. Build image

```bash
docker build \
  --build-arg NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn \
  -t prodiab-portal ./portal-client
```

`NEXT_PUBLIC_API_BASE_URL` bake lúc build → trỏ về domain công khai (nginx route `/api` về backend).

## 2. Service trong docker-compose (thêm cạnh frontend/backend)

```yaml
  portal:
    build:
      context: ./portal-client
      args:
        NEXT_PUBLIC_API_BASE_URL: https://his.diab.com.vn
    environment:
      NODE_ENV: production
    expose: ["3000"]
    depends_on: [backend]
    restart: unless-stopped
```

## 3. Nginx — định tuyến subdomain phòng khám → portal

Wildcard DNS `*.diab.com.vn` + wildcard TLS. Trong stack nginx (`ops/nginx`):

```nginx
upstream prodiab_portal { server portal:3000; }

# Subdomain phòng khám (phongkham-a.diab.com.vn ...) -> portal benh nhan
server {
    listen 80;
    server_name ~^(?<clinic>[a-z0-9-]+)\.diab\.com\.vn$;   # tru his.diab.com.vn (khai server rieng ben duoi)

    client_max_body_size 25m;

    location /api/ {
        proxy_pass http://prodiab_backend;
        proxy_set_header Host              $host;   # QUAN TRONG: giu Host de backend resolve tenant theo subdomain
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $http_x_forwarded_proto;
    }
    location / {
        proxy_pass http://prodiab_portal;
        proxy_set_header Host              $host;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $http_x_forwarded_proto;
    }
}
```

Giữ nguyên `server` hiện có cho `his.diab.com.vn` (app nội bộ) — khai `server_name his.diab.com.vn;`
tường minh để nó thắng regex ở trên. Backend đọc `Host` → tra subdomain → tenant_id.

## 4. Dev (localhost)

`phongkham-a.localhost:3000` (Chrome/Edge/FF hỗ trợ `*.localhost`). Nếu host là `localhost` thường,
đặt `NEXT_PUBLIC_DEV_SUBDOMAIN=phongkham-a` để FE gửi header `X-Portal-Subdomain` cho backend resolve.

## 5. Việc còn lại trước khi go-live
- Bổ sung asset thật `public/icons/icon-192.png`, `icon-512.png` (PWA installable).
- Đăng ký subdomain cho từng phòng khám vào `diab_his_sys_tenants.subdomain`.
- Web Push đã hiện thực thật (RFC 8291 aes128gcm + RFC 8292 VAPID trong `WebPushCrypto`, verify test
  vector chính thức). Cấu hình `WebPush:Subject` = `mailto:...` liên hệ của phòng khám (BYT/push service yêu cầu).
