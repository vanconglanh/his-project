/**
 * k6-load.js — Load test Pro-Diab HIS
 * Ramp 0→100 VUs trong 10 phút, sustain 5 phút, ramp down 5 phút
 *
 * Chạy:
 *   k6 run k6-load.js -e BASE_URL=https://staging.prodiab.example.com
 *   k6 run k6-load.js -e BASE_URL=https://staging.prodiab.example.com --out json=results.json
 *
 * Endpoints được test:
 *   - POST /auth/login
 *   - GET  /patients (list)
 *   - POST /encounters (create encounter)
 *   - POST /prescriptions (sign prescription)
 *   - POST /pharmacy/dispense (dispense)
 */

import http from "k6/http";
import { check, group, sleep, fail } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";
import { randomIntBetween, randomString } from "https://jslib.k6.io/k6-utils/1.4.0/index.js";

// ---------------------------------------------------------------------------
// Cấu hình
// ---------------------------------------------------------------------------
const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const API = `${BASE_URL}/api/v1`;

const TEST_USER = __ENV.TEST_USER || "loadtest@prodiab.local";
const TEST_PASS = __ENV.TEST_PASS || "LoadTest@2026!";
const TENANT_ID = __ENV.TENANT_ID || "00000000-0000-0000-0000-000000000001";

// ---------------------------------------------------------------------------
// Custom metrics
// ---------------------------------------------------------------------------
const errorRate     = new Rate("error_rate");
const loginDuration = new Trend("login_duration_ms", true);
const encounterDuration = new Trend("encounter_create_ms", true);
const prescriptionDuration = new Trend("prescription_sign_ms", true);
const dispenseDuration = new Trend("dispense_ms", true);
const failedRequests = new Counter("failed_requests");

// ---------------------------------------------------------------------------
// Scenarios & thresholds
// ---------------------------------------------------------------------------
export const options = {
  scenarios: {
    // Ramp up từ 0 → 100 VUs trong 10 phút
    ramp_up: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "2m", target: 20 },   // warm up
        { duration: "3m", target: 50 },
        { duration: "5m", target: 100 },  // đạt peak
        { duration: "5m", target: 100 },  // sustain
        { duration: "3m", target: 50 },   // ramp down
        { duration: "2m", target: 0 },
      ],
      gracefulRampDown: "30s",
    },
  },
  thresholds: {
    http_req_failed:        ["rate<0.01"],      // < 1% lỗi HTTP
    http_req_duration:      ["p(95)<1000", "p(99)<3000"],  // p95<1s, p99<3s
    error_rate:             ["rate<0.01"],
    login_duration_ms:      ["p(95)<2000"],
    encounter_create_ms:    ["p(95)<1500"],
    prescription_sign_ms:   ["p(95)<2000"],
    dispense_ms:            ["p(95)<1500"],
  },
};

// ---------------------------------------------------------------------------
// Setup — tạo token dùng chung
// ---------------------------------------------------------------------------
export function setup() {
  const t0 = Date.now();
  const res = http.post(
    `${API}/auth/login`,
    JSON.stringify({ email: TEST_USER, password: TEST_PASS, tenantId: TENANT_ID }),
    { headers: { "Content-Type": "application/json" }, timeout: "10s" }
  );
  loginDuration.add(Date.now() - t0);

  if (res.status !== 200) {
    fail(`Setup login thất bại: HTTP ${res.status} — ${res.body.substring(0, 200)}`);
  }

  let body;
  try {
    body = JSON.parse(res.body);
  } catch (e) {
    fail(`Setup: không parse được response JSON — ${res.body.substring(0, 200)}`);
  }

  return {
    token: body.data.accessToken,
    refreshToken: body.data.refreshToken,
  };
}

// ---------------------------------------------------------------------------
// Default scenario
// ---------------------------------------------------------------------------
export default function (data) {
  const headers = {
    "Content-Type": "application/json",
    Authorization: `Bearer ${data.token}`,
    "X-Tenant-Id": TENANT_ID,
  };

  // Phân phối ngẫu nhiên action theo tỷ lệ thực tế phòng khám
  const roll = Math.random();

  if (roll < 0.30) {
    scenarioListPatients(headers);
  } else if (roll < 0.55) {
    scenarioLogin();
  } else if (roll < 0.75) {
    scenarioCreateEncounter(headers);
  } else if (roll < 0.88) {
    scenarioSignPrescription(headers);
  } else {
    scenarioDispense(headers);
  }

  sleep(randomIntBetween(1, 3));
}

// ---------------------------------------------------------------------------
// Scenario: List patients
// ---------------------------------------------------------------------------
function scenarioListPatients(headers) {
  group("list_patients", () => {
    const page = randomIntBetween(1, 5);
    const res = http.get(`${API}/patients?page=${page}&pageSize=20`, {
      headers,
      timeout: "10s",
    });

    const ok = check(res, {
      "list_patients: status 200": (r) => r.status === 200,
      "list_patients: has data": (r) => {
        try { return Array.isArray(JSON.parse(r.body).data); }
        catch { return false; }
      },
      "list_patients: p95 < 1s": (r) => r.timings.duration < 1000,
    });

    if (!ok) {
      errorRate.add(1);
      failedRequests.add(1);
      console.warn(`list_patients thất bại: ${res.status}`);
    }
  });
}

// ---------------------------------------------------------------------------
// Scenario: Login
// ---------------------------------------------------------------------------
function scenarioLogin() {
  group("login", () => {
    const t0 = Date.now();
    const res = http.post(
      `${API}/auth/login`,
      JSON.stringify({ email: TEST_USER, password: TEST_PASS, tenantId: TENANT_ID }),
      { headers: { "Content-Type": "application/json" }, timeout: "10s" }
    );
    loginDuration.add(Date.now() - t0);

    const ok = check(res, {
      "login: status 200": (r) => r.status === 200,
      "login: has token": (r) => {
        try { return !!JSON.parse(r.body).data.accessToken; }
        catch { return false; }
      },
    });

    if (!ok) {
      errorRate.add(1);
      failedRequests.add(1);
    }
  });
}

// ---------------------------------------------------------------------------
// Scenario: Create encounter
// ---------------------------------------------------------------------------
function scenarioCreateEncounter(headers) {
  group("create_encounter", () => {
    const patientId = `00000000-0000-0000-0000-${String(randomIntBetween(1, 100)).padStart(12, "0")}`;

    const t0 = Date.now();
    const res = http.post(
      `${API}/encounters`,
      JSON.stringify({
        patientId,
        chiefComplaint: "Đau đầu, sốt nhẹ",
        encounterType: "outpatient",
        visitDate: new Date().toISOString(),
      }),
      { headers, timeout: "15s" }
    );
    encounterDuration.add(Date.now() - t0);

    const ok = check(res, {
      "create_encounter: status 201 or 200 or 404": (r) =>
        r.status === 201 || r.status === 200 || r.status === 404,
      "create_encounter: p95 < 1.5s": (r) => r.timings.duration < 1500,
    });

    if (!ok && res.status >= 500) {
      errorRate.add(1);
      failedRequests.add(1);
      console.warn(`create_encounter lỗi server: ${res.status} — ${res.body.substring(0, 100)}`);
    }
  });
}

// ---------------------------------------------------------------------------
// Scenario: Sign prescription
// ---------------------------------------------------------------------------
function scenarioSignPrescription(headers) {
  group("sign_prescription", () => {
    const encounterId = `00000000-0000-0000-0000-${String(randomIntBetween(1, 50)).padStart(12, "0")}`;

    const t0 = Date.now();
    const res = http.post(
      `${API}/prescriptions`,
      JSON.stringify({
        encounterId,
        diagnosis: "J06.9 - Nhiễm khuẩn hô hấp trên cấp tính",
        medications: [
          {
            drugCode: "SĐK-001",
            drugName: "Paracetamol 500mg",
            quantity: 10,
            dosage: "1 viên × 3 lần/ngày",
            unit: "viên",
          },
        ],
        notes: "Uống sau ăn",
      }),
      { headers, timeout: "15s" }
    );
    prescriptionDuration.add(Date.now() - t0);

    const ok = check(res, {
      "sign_prescription: not 5xx": (r) => r.status < 500,
      "sign_prescription: p95 < 2s": (r) => r.timings.duration < 2000,
    });

    if (!ok) {
      errorRate.add(1);
      failedRequests.add(1);
    }
  });
}

// ---------------------------------------------------------------------------
// Scenario: Dispense
// ---------------------------------------------------------------------------
function scenarioDispense(headers) {
  group("dispense", () => {
    const prescriptionId = `00000000-0000-0000-0000-${String(randomIntBetween(1, 50)).padStart(12, "0")}`;

    const t0 = Date.now();
    const res = http.post(
      `${API}/pharmacy/dispense`,
      JSON.stringify({
        prescriptionId,
        dispensedBy: "pharmacist-001",
        notes: "Đã cấp phát đầy đủ",
      }),
      { headers, timeout: "15s" }
    );
    dispenseDuration.add(Date.now() - t0);

    const ok = check(res, {
      "dispense: not 5xx": (r) => r.status < 500,
      "dispense: p95 < 1.5s": (r) => r.timings.duration < 1500,
    });

    if (!ok) {
      errorRate.add(1);
      failedRequests.add(1);
    }
  });
}
