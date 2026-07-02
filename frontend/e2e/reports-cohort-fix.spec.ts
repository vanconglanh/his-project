import { test, expect, type Page, type ConsoleMessage } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const SHOTS_DIR = path.resolve(__dirname, "screenshots", "reports-cohort-fix");
fs.mkdirSync(SHOTS_DIR, { recursive: true });

const CRITICAL_RE = /Cannot read properties of undefined.*map/i;

type Tracker = {
  consoleErrors: string[];
  pageErrors: string[];
  requests: string[];
};

function attachTrackers(page: Page): Tracker {
  const t: Tracker = { consoleErrors: [], pageErrors: [], requests: [] };
  page.on("console", (msg: ConsoleMessage) => {
    if (msg.type() === "error") t.consoleErrors.push(msg.text());
  });
  page.on("pageerror", (err) => {
    t.pageErrors.push(err.message);
  });
  page.on("request", (req) => {
    t.requests.push(req.url());
  });
  return t;
}

// Stub auth (BE down -> 404 if not mocked). Mirror auth.spec.ts mock shape.
async function stubAuth(page: Page) {
  const mockUser = {
    id: 1,
    email: "admin@prodiab.local",
    fullName: "Admin Prodiab",
    role: "Admin",
    tenantId: 1,
    clinicId: 1,
    clinicName: "Phòng khám Demo",
    permissions: ["report.read", "patient.read", "dashboard.read"],
  };
  await page.route("**/api/v1/auth/login", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        data: {
          accessToken: "mock-access-token",
          refreshToken: "mock-refresh-token",
          expiresIn: 3600,
          user: mockUser,
        },
      }),
    });
  });
  // Stub refresh token — tránh interceptor redirect về /login khi API 401
  await page.route("**/api/v1/auth/refresh", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        data: { accessToken: "mock-access-token", refreshToken: "mock-refresh-token" },
      }),
    });
  });
  // Stub logout — tránh lỗi khi component gọi logout
  await page.route("**/api/v1/auth/logout", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: "{}" });
  });
  // Stub me/profile endpoint nếu có
  await page.route("**/api/v1/auth/me", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: mockUser }),
    });
  });
}

// Stub tất cả reports API để tránh hanging requests khi BE down
async function stubAllReportsApis(page: Page) {
  const emptyList = JSON.stringify({ data: [] });
  const emptyObj = JSON.stringify({ data: {} });
  // Revenue
  await page.route("**/api/v1/reports/revenue*", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: [] }),
    });
  });
  await page.route("**/api/v1/reports/revenue-by-method*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  await page.route("**/api/v1/reports/top-doctors*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  // Clinical
  await page.route("**/api/v1/reports/top-diagnoses*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  // Pharmacy
  await page.route("**/api/v1/reports/top-pharmacy-drugs*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  await page.route("**/api/v1/reports/pharmacy*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  // Dashboard summary
  await page.route("**/api/v1/reports/summary*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyObj });
  });
  // Dashboard endpoints
  await page.route("**/api/v1/dashboard/overview", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        data: { total_patients: 0, today_encounters: 0, today_revenue: 0, pending_encounters: 0 },
      }),
    });
  });
  await page.route("**/api/v1/dashboard/charts/**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: { labels: [], datasets: [] } }),
    });
  });
  await page.route("**/api/v1/dashboard/alerts*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  // Catch-all dashboard
  await page.route("**/api/v1/dashboard**", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyObj });
  });
  // Notifications
  await page.route("**/api/v1/notifications*", async (route) => {
    await route.fulfill({ status: 200, contentType: "application/json", body: emptyList });
  });
  // Tenants/me
  await page.route("**/api/v1/tenants/me*", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: { id: 1, name: "Phòng khám Demo" } }),
    });
  });
  // Catch-all: tất cả API v1 chưa stub → 200 empty để tránh 401/connection refused → redirect
  await page.route("**/api/v1/**", async (route) => {
    // Chỉ handle GET, để POST/PUT/DELETE pass-through (hoặc cũng 200)
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: null }),
    });
  });
}

// Stub cohort endpoint với mock value deterministic.
// R4 dựa vào BE down -> FE fallback mock; R5 BE up trả empty DB nên không fallback.
// Stub ở đây để test value 348/87 ổn định bất kể BE state.
async function stubCohort(page: Page) {
  await page.route("**/api/v1/reports/diabetes/cohort*", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        data: {
          as_of: new Date().toISOString().slice(0, 10),
          total_patients: 412,
          by_type: { t1: 45, t2: 348, gdm: 19 },
          hba1c_distribution: { lt_7: 120, between_7_8: 145, between_8_9: 98, gt_9: 49 },
          complications: { retinopathy: 87, neuropathy: 134, nephropathy: 62, cad: 44, pad: 31 },
        },
      }),
    });
  });
}

async function login(page: Page) {
  // Catch-all phải register TRƯỚC để specific routes (register sau) có priority cao hơn
  await stubAllReportsApis(page);
  await stubAuth(page);
  await stubCohort(page);

  // Inject auth state vào localStorage TRƯỚC khi navigate — bỏ qua UI login flow
  // tránh race condition với Next.js dev "Compiling..." overlay block form submit
  await page.goto("/login", { waitUntil: "load", timeout: 60_000 });
  await page.evaluate(() => {
    const authState = {
      state: {
        user: {
          id: 1,
          email: "admin@prodiab.local",
          fullName: "Admin Prodiab",
          role: "Admin",
          tenantId: 1,
          clinicId: 1,
          clinicName: "Phòng khám Demo",
          permissions: ["report.read", "patient.read", "dashboard.read"],
        },
        accessToken: "mock-access-token",
        refreshToken: "mock-refresh-token",
        isAuthenticated: true,
        permissions: ["report.read", "patient.read", "dashboard.read"],
        roles: ["Admin"],
      },
      version: 0,
    };
    localStorage.setItem("auth-store", JSON.stringify(authState));
  });
  // Navigate trực tiếp đến trang đích (không cần qua login form)
  await page.goto("/", { waitUntil: "load", timeout: 60_000 });
  // Đợi layout render — xác nhận auth state được nhận
  await page.waitForSelector("aside, main", { timeout: 20_000 });
  await page.waitForTimeout(1000);
}

async function shot(page: Page, file: string) {
  await page.screenshot({ path: path.join(SHOTS_DIR, file), fullPage: true });
}

function assertNoCritical(t: Tracker, where: string) {
  const all = [...t.consoleErrors, ...t.pageErrors];
  const hit = all.filter((m) => CRITICAL_RE.test(m));
  if (hit.length) {
    throw new Error(`[${where}] critical error matched ${CRITICAL_RE}: ${JSON.stringify(hit)}`);
  }
}

test.describe.configure({ mode: "serial", timeout: 300_000 });

test.describe("reports cohort fix - visual", () => {
  test("TC01 - Financial tab khong crash + screenshot", async ({ page }) => {
    const t = attachTrackers(page);
    await login(page);
    await page.goto("/reports", { waitUntil: "load", timeout: 60_000 });
    // Đợi tab list render xong
    await page.waitForSelector("[role='tab']", { timeout: 20_000 });
    const tab = page.getByRole("tab", { name: /Tài chính/i });
    if (await tab.count()) await tab.click({ force: true, timeout: 15_000 });
    await page.waitForTimeout(4000);
    await shot(page, "tc01-financial-tab.png");
    console.log(`[TC01] consoleErrors=${t.consoleErrors.length} pageErrors=${t.pageErrors.length}`);
    if (t.consoleErrors.length) console.log("[TC01] sample:", t.consoleErrors.slice(0, 3));
    assertNoCritical(t, "TC01");
  });

  test("TC02 - Clinical tab cohort card render", async ({ page }) => {
    const t = attachTrackers(page);
    await login(page);
    await page.goto("/reports", { waitUntil: "load", timeout: 60_000 });
    // Đợi tab list render xong — React hydrate + TanStack Query placeholderData active
    await page.waitForSelector("[role='tab']", { timeout: 20_000 });
    const tab = page.getByRole("tab", { name: /Lâm sàng/i });
    if (await tab.count()) await tab.click({ force: true, timeout: 15_000 });
    await page.waitForTimeout(5000);
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await page.waitForTimeout(1000);
    await shot(page, "tc02-clinical-tab.png");
    const body = await page.locator("body").innerText();
    const has348 = body.includes("348");
    const has87 = body.includes("87");
    console.log(
      `[TC02] has T2=348? ${has348}; has retinopathy=87? ${has87}; consoleErrors=${t.consoleErrors.length}`
    );
    assertNoCritical(t, "TC02");
    expect(
      has348 || has87,
      "Expect at least one cohort mock value (348 or 87) visible"
    ).toBeTruthy();
  });

  test("TC03 - Dashboard cohort widget khong crash", async ({ page }) => {
    const t = attachTrackers(page);
    await login(page);
    await page.goto("/", { waitUntil: "load", timeout: 60_000 });
    await page.waitForSelector("main", { timeout: 20_000 });
    await page.waitForTimeout(4500);
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await page.waitForTimeout(1000);
    await shot(page, "tc03-dashboard.png");
    console.log(`[TC03] consoleErrors=${t.consoleErrors.length} pageErrors=${t.pageErrors.length}`);
    if (t.pageErrors.length) console.log("[TC03] pageErrors:", t.pageErrors.slice(0, 3));
    assertNoCritical(t, "TC03");
  });

  test("TC04 - Network request cohort?dm_type=ALL", async ({ page }) => {
    const t = attachTrackers(page);
    await login(page);
    await page.goto("/reports", { waitUntil: "load", timeout: 60_000 });
    await page.waitForSelector("[role='tab']", { timeout: 20_000 });
    const tab = page.getByRole("tab", { name: /Lâm sàng/i });
    if (await tab.count()) await tab.click({ force: true, timeout: 15_000 });
    await page.waitForTimeout(4000);
    await shot(page, "tc04-network-cohort.png");
    const cohortReqs = t.requests.filter((u) => /\/api\/v1\/reports\/diabetes\/cohort/.test(u));
    console.log(`[TC04] cohort requests captured: ${cohortReqs.length}`);
    cohortReqs.forEach((u) => console.log(`  -> ${u}`));
    const hasAll = cohortReqs.some((u) => /dm_type=ALL/i.test(u));
    expect(cohortReqs.length, "expect at least 1 cohort request").toBeGreaterThan(0);
    expect(hasAll, "expect request URL contain dm_type=ALL").toBeTruthy();
    assertNoCritical(t, "TC04");
  });
});
