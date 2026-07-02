#!/usr/bin/env node
// Crawl tất cả route trong sidebar + sample detail route, báo 404
// Usage: node scripts/check-404.mjs [baseUrl]

const BASE = process.argv[2] || "http://localhost:3000";
const API = process.env.API_BASE || "http://localhost:5000";
const EMAIL = process.env.ADMIN_EMAIL || "admin@prodiab.local";
const PASSWORD = process.env.ADMIN_PASSWORD || "Admin@123";

const ROUTES = [
  // Sidebar
  "/",
  "/patients",
  "/encounters",
  "/nurse",
  "/reception",
  "/prescriptions",
  "/drugs",
  "/pharmacy",
  "/labrad",
  "/labrad/results",
  "/labrad/partners",
  "/cashier",
  "/cashier/debts",
  "/billings",
  "/bhyt",
  "/services",
  "/reports",
  "/icd10",
  "/notifications",
  // Admin
  "/admin",
  "/admin/users",
  "/admin/roles",
  "/admin/audit",
  "/admin/tenants",
  "/admin/api-partners",
  "/admin/dtqg",
  "/admin/einvoice",
  "/admin/suppliers",
  "/admin/emr-templates",
  "/admin/notifications-config",
  // Account
  "/account/security",
  "/account/notifications",
  // Patient editor routes
  "/patients/new",
  // Auth
  "/login",
  "/forgot-password",
  "/reset-password",
  "/accept-invite",
  // Portal
  "/portal",
  "/portal/login",
  "/portal/me",
  "/portal/encounters",
  "/portal/prescriptions",
  "/portal/lab-results",
  "/portal/appointments",
];

console.log(`Checking ${ROUTES.length} routes against ${BASE}...\n`);

let pass = 0,
  fail = 0;
const failures = [];

for (const r of ROUTES) {
  try {
    const res = await fetch(`${BASE}${r}`, {
      redirect: "manual",
      signal: AbortSignal.timeout(15000),
    });
    const ok = res.status >= 200 && res.status < 400;
    const icon = ok ? "✅" : "❌";
    console.log(`  ${icon} ${res.status}  ${r}`);
    if (ok) pass++;
    else {
      fail++;
      failures.push({ route: r, status: res.status });
    }
  } catch (e) {
    fail++;
    failures.push({ route: r, status: "ERR", error: e.message });
    console.log(`  ❌ ERR  ${r}  (${e.message})`);
  }
}

console.log(`\n──────────────────────────────`);
console.log(`TOTAL: ${pass} PASS / ${fail} FAIL of ${ROUTES.length}`);
if (failures.length) {
  console.log(`\nFailed routes:`);
  failures.forEach((f) => console.log(`  - ${f.status}  ${f.route}`));
  process.exit(1);
}
