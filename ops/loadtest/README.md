# Load Test — Pro-Diab HIS (k6)

Thu mục này chứa các kịch bản load test bằng [k6](https://k6.io/) cho Pro-Diab HIS.

## Yêu cầu

- k6 >= 0.50.0 — cài từ https://k6.io/docs/getting-started/installation/
- Staging environment đang chạy và có user test đã seed sẵn

## Biến môi trường

| Biến | Mô tả | Mặc định |
|------|-------|----------|
| `BASE_URL` | Base URL của môi trường test | `http://localhost:5000` |
| `TEST_USER` | Email user test | `loadtest@prodiab.local` |
| `TEST_PASS` | Password user test | `LoadTest@2026!` |
| `TENANT_ID` | UUID tenant test | `00000000-0000-0000-0000-000000000001` |

## Các kịch bản

### 1. Smoke Test (`k6-smoke.js`)

Kiểm tra nhanh xem các endpoint cơ bản còn hoạt động không. Dùng trước khi deploy hoặc sau deploy.

- 10 VUs, 1 phút
- Endpoints: `/healthz`, `/api/v1/health`, `/api/v1/patients`
- Threshold: error < 1%, p95 < 2s

```bash
k6 run ops/loadtest/k6-smoke.js -e BASE_URL=https://staging.prodiab.example.com
```

### 2. Load Test (`k6-load.js`)

Mô phỏng tải thực tế của phòng khám. Ramp up dần, sustain ở peak, ramp down.

- 0 → 100 VUs trong 10 phút → sustain 5 phút → ramp down 5 phút
- Tổng thời gian: ~20 phút
- Endpoints: login, list patients, create encounter, sign prescription, dispense
- Threshold: error < 1%, p95 < 1s, p99 < 3s

```bash
k6 run ops/loadtest/k6-load.js \
  -e BASE_URL=https://staging.prodiab.example.com \
  -e TEST_USER=loadtest@prodiab.local \
  -e TEST_PASS=LoadTest@2026!
```

Xuất kết quả ra file JSON để phân tích:

```bash
k6 run ops/loadtest/k6-load.js \
  -e BASE_URL=https://staging.prodiab.example.com \
  --out json=results/load-$(date +%Y%m%d_%H%M%S).json
```

### 3. Spike Test (`k6-spike.js`)

Kiểm tra hệ thống khi có traffic đột biến (ví dụ: mở cửa phòng khám buổi sáng, nhiều user đăng nhập cùng lúc).

- 0 → 500 VUs trong 30 giây → sustain 1 phút → ramp down
- Threshold: 5xx error < 5%, p95 < 5s
- Theo dõi thêm metric `post_spike_duration_ms` để đo thời gian phục hồi

```bash
k6 run ops/loadtest/k6-spike.js \
  -e BASE_URL=https://staging.prodiab.example.com
```

## Xem kết quả trực tiếp trên Grafana

k6 có thể stream metrics vào Grafana Cloud hoặc InfluxDB:

```bash
# Với k6 Cloud
k6 cloud ops/loadtest/k6-load.js -e BASE_URL=https://staging.prodiab.example.com

# Với InfluxDB local
k6 run ops/loadtest/k6-load.js \
  -e BASE_URL=https://staging.prodiab.example.com \
  --out influxdb=http://localhost:8086/k6
```

## Seed user test

Trước khi chạy load test, cần seed user test vào staging:

```bash
# Chạy trên staging server
docker compose exec backend \
  dotnet ProDiab.CLI.dll seed-test-users \
  --tenant-id 00000000-0000-0000-0000-000000000001 \
  --count 1
```

Hoặc qua API Admin:

```bash
curl -X POST https://staging.prodiab.example.com/api/v1/admin/seed-test-data \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"00000000-0000-0000-0000-000000000001","scenario":"load-test"}'
```

## Giải thích kết quả

```
scenarios: (100.00%) 1 scenario, 100 max VUs
default: 0 looping VUs for 20m0s

checks.........................: 99.12%   ✓ 45231  ✗ 398
data_received..................: 234 MB   195 kB/s
data_sent......................: 45 MB    37 kB/s
http_req_duration..............: avg=234ms  min=12ms   med=198ms  max=8.2s   p(90)=445ms p(95)=612ms
  { expected_response:true }...: avg=228ms  min=12ms   med=195ms  max=4.1s   p(90)=430ms p(95)=590ms
http_req_failed................: 0.88%    ✓ 44833  ✗ 398
http_reqs......................: 45231    37.69/s

THRESHOLDS
http_req_failed............: rate<0.01    ✓ PASS
http_req_duration (p95)....: p(95)<1000  ✓ PASS 612ms
```

**Các chỉ số cần quan tâm:**

| Metric | Mô tả | Target |
|--------|-------|--------|
| `http_req_failed` | Tỷ lệ request thất bại | < 1% |
| `http_req_duration p(95)` | Latency p95 | < 1000ms |
| `http_req_duration p(99)` | Latency p99 | < 3000ms |
| `error_rate` | Custom error rate | < 1% |
| `login_duration_ms p(95)` | Login latency p95 | < 2000ms |
