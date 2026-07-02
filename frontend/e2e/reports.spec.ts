import { test, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const ADMIN_EMAIL = "admin@prodiab.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "Admin@123";
const SHOTS_DIR = path.resolve(__dirname, "..", "test-results", "reports-shots");
const REPORT_FILE = path.resolve(__dirname, "..", "test-results", "reports-report.json");
const DL_DIR = path.resolve(__dirname, "..", "test-results", "reports-downloads");

type Status = "PASS" | "FAIL" | "SKIP";
interface StepResult { step: string; name: string; status: Status; screenshots: string[]; note?: string; error?: string; }
const reportsResults: StepResult[] = [];

fs.mkdirSync(SHOTS_DIR, { recursive: true });
fs.mkdirSync(DL_DIR, { recursive: true });

async function shot(page: Page, file: string): Promise<string> {
  const p = path.join(SHOTS_DIR, file);
  await page.screenshot({ path: p, fullPage: true }).catch(() => {});
  return file;
}

async function runStep(step: string, name: string, fn: () => Promise<{ screenshots: string[]; note?: string }>) {
  try {
    const r = await fn();
    reportsResults.push({ step, name, status: "PASS", screenshots: r.screenshots, note: r.note });
    console.log(`[reports] ${step} ${name} -> PASS${r.note ? " :: " + r.note : ""}`);
  } catch (e: unknown) {
    const err = (e as Error).message ?? String(e);
    const isSkip = err.startsWith("SKIP:");
    reportsResults.push({ step, name, status: isSkip ? "SKIP" : "FAIL", screenshots: [], error: err });
    console.log(`[reports] ${step} ${name} -> ${isSkip ? "SKIP" : "FAIL"} :: ${err}`);
  }
}

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 40_000 });
  await page.locator("#email").fill(ADMIN_EMAIL);
  await page.locator("#password").fill(ADMIN_PASSWORD);
  await page.getByRole("button", { name: /Đăng nhập/i }).click();
  await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30_000 });
  await page.waitForTimeout(600);
}

function dateRange() {
  const to = new Date();
  const from = new Date(to.getTime() - 29 * 86400_000);
  const f = (d: Date) => d.toISOString().slice(0, 10);
  return { from: f(from), to: f(to) };
}

async function previewAndDownload(page: Page, kind: "financial" | "clinical" | "pharmacy", stepDl: string) {
  const { from, to } = dateRange();
  await page.goto(`/reports/print/${kind}?from=${from}&to=${to}`, { waitUntil: "domcontentloaded", timeout: 30_000 });
  await page.waitForTimeout(4000);
  const s1 = await shot(page, `${stepDl}-preview-${kind}.png`);

  const dlBtn = page.getByRole("button", { name: /Tải PDF/i }).first();
  let size = 0;
  let dlPath = "";
  const [dl] = await Promise.all([
    page.waitForEvent("download", { timeout: 30_000 }),
    dlBtn.click(),
  ]);
  dlPath = path.join(DL_DIR, `${kind}.pdf`);
  await dl.saveAs(dlPath);
  size = fs.statSync(dlPath).size;
  const s2 = await shot(page, `${stepDl}-after-download-${kind}.png`);
  if (size < 50 * 1024) throw new Error(`PDF too small: ${size} bytes (< 50KB)`);
  return { screenshots: [s1, s2], note: `pdf=${size}B path=${dlPath}` };
}

test.describe("reports flow", () => {
  test.describe.configure({ mode: "serial", timeout: 600_000 });

  test("reports e2e", async ({ page }) => {
    await runStep("STEP-01", "Login + vao /reports", async () => {
      await login(page);
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(1500);
      return { screenshots: [await shot(page, "step-01-reports-home.png")] };
    });

    await runStep("STEP-02", "Tab Tai chinh + chart", async () => {
      const tab = page.getByRole("tab", { name: /Tài chính/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      return { screenshots: [await shot(page, "step-02-financial-tab.png")] };
    });

    await runStep("STEP-03+04", "Financial preview + Tai PDF", async () => {
      return await previewAndDownload(page, "financial", "step-03");
    });

    await runStep("STEP-05", "Tab Lam sang", async () => {
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(800);
      const tab = page.getByRole("tab", { name: /Lâm sàng/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      return { screenshots: [await shot(page, "step-05-clinical-tab.png")] };
    });

    await runStep("STEP-06", "Clinical preview + Tai PDF", async () => {
      return await previewAndDownload(page, "clinical", "step-06");
    });

    await runStep("STEP-07", "Tab Duoc pham", async () => {
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(800);
      const tab = page.getByRole("tab", { name: /Kho dược|Dược/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      return { screenshots: [await shot(page, "step-07-pharmacy-tab.png")] };
    });

    await runStep("STEP-08", "Pharmacy preview + Tai PDF", async () => {
      return await previewAndDownload(page, "pharmacy", "step-08");
    });

    await runStep("STEP-09", "Doctor KPI widget", async () => {
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(800);
      const tab = page.getByRole("tab", { name: /Tài chính/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(800);
      return { screenshots: [await shot(page, "step-09-doctor-kpi.png")] };
    });

    await runStep("STEP-10", "Diabetes cohort widget", async () => {
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(800);
      const tab = page.getByRole("tab", { name: /Lâm sàng/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(800);
      return { screenshots: [await shot(page, "step-10-diabetes-cohort.png")] };
    });

    await runStep("STEP-11", "Top drugs widget", async () => {
      await page.goto("/reports", { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(800);
      const tab = page.getByRole("tab", { name: /Kho dược|Dược/i });
      if (await tab.count()) await tab.click();
      await page.waitForTimeout(2500);
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(800);
      return { screenshots: [await shot(page, "step-11-top-drugs.png")] };
    });

    fs.writeFileSync(REPORT_FILE, JSON.stringify({
      ts: new Date().toISOString(),
      total: reportsResults.length,
      pass: reportsResults.filter(r => r.status === "PASS").length,
      fail: reportsResults.filter(r => r.status === "FAIL").length,
      skip: reportsResults.filter(r => r.status === "SKIP").length,
      results: reportsResults,
    }, null, 2));
  });
});
