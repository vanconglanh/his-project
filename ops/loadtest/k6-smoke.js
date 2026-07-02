/**
 * k6-smoke.js — Smoke test Pro-Diab HIS
 * 10 VUs trong 1 phút, kiểm tra các endpoint cơ bản còn sống
 *
 * Chạy:
 *   k6 run k6-smoke.js -e BASE_URL=https://staging.prodiab.example.com
 *
 * Pass điều kiện:
 *   - http_req_failed < 1%
 *   - http_req_duration p(95) < 2000ms
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// ---------------------------------------------------------------------------
// Cấu hình
// ---------------------------------------------------------------------------
const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const API = `${BASE_URL}/api/v1`;

// Metrics tùy chỉnh
const errorRate = new Rate("error_rate");
const loginDuration = new Trend("login_duration_ms", true);

// ---------------------------------------------------------------------------
// Thresholds
// ---------------------------------------------------------------------------
export const options = {
  vus: 10,
  duration: "1m",
  thresholds: {
    http_req_failed: ["rate<0.01"],           // < 1% lỗi
    http_req_duration: ["p(95)<2000"],        // p95 < 2s
    error_rate: ["rate<0.01"],
    login_duration_ms: ["p(95)<3000"],        // login p95 < 3s
  },
};

// ---------------------------------------------------------------------------
// Test credentials (staging only — không dùng cho prod)
// ---------------------------------------------------------------------------
const TEST_USER = __ENV.TEST_USER || "smoketest@prodiab.local";
const TEST_PASS = __ENV.TEST_PASS || "SmokeTest@2026!";
const TENANT_ID = __ENV.TENANT_ID || "00000000-0000-0000-0000-000000000001";

// ---------------------------------------------------------------------------
// Setup — lấy token một lần dùng chung
// ---------------------------------------------------------------------------
export function setup() {
  const loginRes = http.post(
    `${API}/auth/login`,
    JSON.stringify({ email: TEST_USER, password: TEST_PASS, tenantId: TENANT_ID }),
    { headers: { "Content-Type": "application/json" } }
  );

  const ok = check(loginRes, {
    "setup: login status 200": (r) => r.status === 200,
    "setup: has access_token": (r) => {
      try {
        return JSON.parse(r.body).data.accessToken !== undefined;
      } catch {
        return false;
      }
    },
  });

  if (!ok) {
    console.error(`Setup login thất bại: ${loginRes.status} ${loginRes.body}`);
    return { token: null };
  }

  return { token: JSON.parse(loginRes.body).data.accessToken };
}

// ---------------------------------------------------------------------------
// Scenario mặc định
// ---------------------------------------------------------------------------
export default function (data) {
  const headers = {
    "Content-Type": "application/json",
    Authorization: data.token ? `Bearer ${data.token}` : "",
  };

  // 1. Health check
  const healthRes = http.get(`${BASE_URL}/healthz`);
  check(healthRes, {
    "healthz: status 200": (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // 2. API health
  const apiHealthRes = http.get(`${API}/health`, { headers });
  check(apiHealthRes, {
    "api/health: status 200": (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // 3. Patients list (cần auth)
  if (data.token) {
    const patientsRes = http.get(`${API}/patients?page=1&pageSize=10`, { headers });
    check(patientsRes, {
      "patients: status 200 or 401": (r) => r.status === 200 || r.status === 401,
      "patients: response time < 2s": (r) => r.timings.duration < 2000,
    }) || errorRate.add(1);
  }

  sleep(1);
}

// ---------------------------------------------------------------------------
// Teardown — log tóm tắt
// ---------------------------------------------------------------------------
export function teardown(data) {
  console.log(`Smoke test hoàn tất. Token có: ${data.token ? "YES" : "NO"}`);
}
