/**
 * k6-spike.js — Spike test Pro-Diab HIS
 * Tăng đột ngột từ 0 → 500 VUs trong 30 giây, giữ 1 phút, thoát
 * Mục tiêu: kiểm tra hệ thống không crash khi có traffic đột biến
 *
 * Chạy:
 *   k6 run k6-spike.js -e BASE_URL=https://staging.prodiab.example.com
 *
 * Pass điều kiện:
 *   - Hệ thống không trả 5xx > 5%
 *   - Sau spike, p95 phục hồi về < 2s trong 2 phút
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";

// ---------------------------------------------------------------------------
// Cấu hình
// ---------------------------------------------------------------------------
const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const API = `${BASE_URL}/api/v1`;

const TEST_USER = __ENV.TEST_USER || "spiketest@prodiab.local";
const TEST_PASS = __ENV.TEST_PASS || "SpikeTest@2026!";
const TENANT_ID = __ENV.TENANT_ID || "00000000-0000-0000-0000-000000000001";

// ---------------------------------------------------------------------------
// Custom metrics
// ---------------------------------------------------------------------------
const errorRate5xx   = new Rate("error_rate_5xx");
const spikeRejected  = new Counter("spike_rejected_429");
const recoverDuration = new Trend("post_spike_duration_ms", true);

// ---------------------------------------------------------------------------
// Scenarios: spike 0→500 trong 30s, sustain 1m, ramp down 30s
// ---------------------------------------------------------------------------
export const options = {
  scenarios: {
    spike: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "5s",  target: 10 },    // baseline nhỏ
        { duration: "30s", target: 500 },   // SPIKE — tăng đột ngột
        { duration: "1m",  target: 500 },   // sustain spike
        { duration: "30s", target: 10 },    // ramp down
        { duration: "2m",  target: 10 },    // recovery check
        { duration: "10s", target: 0 },
      ],
      gracefulRampDown: "15s",
    },
  },
  thresholds: {
    // Cho phép error rate cao hơn trong spike nhưng không quá 5%
    http_req_failed:   ["rate<0.05"],
    error_rate_5xx:    ["rate<0.05"],
    // p95 cho toàn bộ test (bao gồm spike) — nới lỏng hơn load test
    http_req_duration: ["p(95)<5000"],
  },
};

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
export function setup() {
  const res = http.post(
    `${API}/auth/login`,
    JSON.stringify({ email: TEST_USER, password: TEST_PASS, tenantId: TENANT_ID }),
    { headers: { "Content-Type": "application/json" }, timeout: "15s" }
  );

  if (res.status !== 200) {
    console.error(`Setup login thất bại: ${res.status}`);
    return { token: null };
  }

  try {
    return { token: JSON.parse(res.body).data.accessToken };
  } catch {
    return { token: null };
  }
}

// ---------------------------------------------------------------------------
// Default scenario — tập trung vào endpoint nhẹ để đo throughput
// ---------------------------------------------------------------------------
export default function (data) {
  const headers = {
    "Content-Type": "application/json",
    Authorization: data.token ? `Bearer ${data.token}` : "",
    "X-Tenant-Id": TENANT_ID,
  };

  // 1. Health check — endpoint nhẹ nhất
  const healthRes = http.get(`${BASE_URL}/healthz`, { timeout: "5s" });
  const healthOk = check(healthRes, {
    "healthz: status 200": (r) => r.status === 200,
    "healthz: not 5xx": (r) => r.status < 500,
  });

  if (healthRes.status >= 500) errorRate5xx.add(1);
  if (healthRes.status === 429) spikeRejected.add(1);

  // 2. API endpoint có auth
  if (data.token) {
    const t0 = Date.now();
    const listRes = http.get(`${API}/patients?page=1&pageSize=5`, {
      headers,
      timeout: "10s",
    });
    recoverDuration.add(Date.now() - t0);

    check(listRes, {
      "patients: not 5xx during spike": (r) => r.status < 500,
      "patients: handled (200 or 429)": (r) => r.status === 200 || r.status === 429,
    });

    if (listRes.status >= 500) errorRate5xx.add(1);
    if (listRes.status === 429) {
      spikeRejected.add(1);
      // Rate limited → backoff ngắn
      sleep(0.5);
      return;
    }
  }

  // Sleep ngắn trong spike để tạo áp lực tối đa
  sleep(0.1);
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------
export function teardown() {
  console.log("Spike test hoàn tất. Kiểm tra Grafana dashboard để xem thời gian phục hồi.");
}
