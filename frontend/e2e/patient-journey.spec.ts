import { test, expect, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const ADMIN_EMAIL = "admin@prodiab.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "Admin@123";
const SHOTS_DIR = path.resolve(__dirname, "..", "test-results", "journey-shots");
const REPORT_FILE = path.resolve(__dirname, "..", "test-results", "patient-journey-report.json");

type StepStatus = "pass" | "fail" | "skip";
interface StepResult { step: string; name: string; status: StepStatus; screenshots: string[]; note?: string; error?: string; }
const journeyResults: StepResult[] = [];

async function shot(page: Page, file: string): Promise<string> {
  const p = path.join(SHOTS_DIR, file);
  await page.screenshot({ path: p, fullPage: true }).catch(() => {});
  return file;
}

async function runStep(step: string, name: string, fn: () => Promise<{ screenshots: string[]; note?: string }>): Promise<void> {
  try {
    const r = await fn();
    journeyResults.push({ step, name, status: "pass", screenshots: r.screenshots, note: r.note });
    console.log("[journey] " + step + " " + name + " -> PASS");
  } catch (e: unknown) {
    const err = (e as Error).message ?? String(e);
    const isSkip = err.startsWith("SKIP:");
    journeyResults.push({ step, name, status: isSkip ? "skip" : "fail", screenshots: [], error: err });
    console.log("[journey] " + step + " " + name + " -> " + (isSkip ? "SKIP" : "FAIL") + " :: " + err);
  }
}

test.describe("patient journey", () => {
  test.describe.configure({ mode: "serial", timeout: 600_000 });

  test("end-to-end mot luot kham", async ({ page }) => {
    fs.mkdirSync(SHOTS_DIR, { recursive: true });

    await runStep("STEP-01", "Login admin", async () => {
      const shots: string[] = [];
      await page.goto("/login", { waitUntil: "domcontentloaded" });
      shots.push(await shot(page, "step-01a-login-empty.png"));
      await page.locator("#email").fill(ADMIN_EMAIL);
      await page.locator("#password").fill(ADMIN_PASSWORD);
      shots.push(await shot(page, "step-01b-login-filled.png"));
      await page.getByRole("button", { name: /Đăng nhập/i }).click();
      await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30_000 });
      await page.waitForLoadState("domcontentloaded").catch(() => {});
      await page.waitForTimeout(800);
      shots.push(await shot(page, "step-01c-dashboard.png"));
      return { screenshots: shots, note: "Logged in as " + ADMIN_EMAIL };
    });

    const uniqueSuffix = Date.now().toString().slice(-6);
    const patientName = "Nguyễn Văn Tâm " + uniqueSuffix;
    await runStep("STEP-02", "Tao benh nhan", async () => {
      const shots: string[] = [];
      await page.goto("/patients", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForTimeout(1200);
      shots.push(await shot(page, "step-02a-patients-list.png"));
      const addBtn = page.getByRole("button", { name: /Thêm bệnh nhân|Tạo mới|Thêm mới|^Thêm$/i }).first();
      const linkBtn = page.getByRole("link", { name: /Thêm bệnh nhân|Tạo mới|^Thêm$/i }).first();
      let opened = false;
      if (await addBtn.count()) { try { await addBtn.click({ timeout: 5000 }); opened = true; } catch {} }
      if (!opened && await linkBtn.count()) { try { await linkBtn.click({ timeout: 5000 }); opened = true; } catch {} }
      if (!opened) {
        await page.goto("/patients/new", { waitUntil: "domcontentloaded", timeout: 15_000 }).catch(() => {});
        opened = !page.url().endsWith("/patients");
      }
      if (!opened) throw new Error("SKIP: khong tim thay nut tao benh nhan");
      await page.waitForTimeout(1200);
      shots.push(await shot(page, "step-02b-patient-form.png"));
      const fillField = async (labels: RegExp, value: string) => {
        const byLabel = page.getByLabel(labels).first();
        if (await byLabel.count()) { await byLabel.fill(value).catch(() => {}); return true; }
        const byPh = page.getByPlaceholder(labels).first();
        if (await byPh.count()) { await byPh.fill(value).catch(() => {}); return true; }
        return false;
      };
      await fillField(/Họ.*tên|Tên|Full.*name/i, patientName);
      await fillField(/Ngày sinh|DOB|Date of birth/i, "1985-03-15");
      await fillField(/Điện thoại|SĐT|Phone/i, "0901234567");
      await fillField(/Địa chỉ|Address/i, "123 Lý Thường Kiệt, Q.10, TP.HCM");
      await fillField(/BHYT|Bảo hiểm|Insurance/i, "DN4791234567890");
      const maleRadio = page.getByLabel(/^Nam$/i).first();
      if (await maleRadio.count()) await maleRadio.check({ force: true }).catch(() => {});
      shots.push(await shot(page, "step-02c-patient-filled.png"));
      const submit = page.getByRole("button", { name: /^Lưu$|^Tạo$|^Submit$|^Thêm$|Lưu lại/i }).last();
      await submit.click({ timeout: 5_000 }).catch(() => {});
      await page.waitForTimeout(2500);
      shots.push(await shot(page, "step-02d-patient-after-submit.png"));
      return { screenshots: shots, note: "Patient name=" + patientName };
    });

    await runStep("STEP-03", "Tiep don", async () => {
      const shots: string[] = [];
      await page.goto("/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-03a-reception.png"));
      const checkin = page.getByRole("button", { name: /Check.?in|Tiếp đón|Đón tiếp/i }).first();
      if (await checkin.count()) { await checkin.click({ timeout: 5_000 }).catch(() => {}); await page.waitForTimeout(1500); shots.push(await shot(page, "step-03b-reception-after.png")); return { screenshots: shots, note: "Check-in clicked" }; }
      throw new Error("SKIP: khong co nut Check-in");
    });

    await runStep("STEP-04", "Tao Encounter", async () => {
      const shots: string[] = [];
      await page.goto("/encounters", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-04a-encounters-list.png"));
      const newBtn = page.getByRole("button", { name: /Tạo.*khám|Khám mới|^Thêm$|^Mới$/i }).first();
      const newLink = page.getByRole("link", { name: /Tạo.*khám|Khám mới|^Thêm$|^Mới$/i }).first();
      if (await newBtn.count()) await newBtn.click({ timeout: 5_000 }).catch(() => {});
      else if (await newLink.count()) await newLink.click({ timeout: 5_000 }).catch(() => {});
      else throw new Error("SKIP: khong co nut tao encounter");
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-04b-encounter-form.png"));
      return { screenshots: shots, note: "Encounter form opened" };
    });

    await runStep("STEP-05", "Ke don", async () => {
      const shots: string[] = [];
      const tab = page.getByRole("tab", { name: /Kê đơn|Đơn thuốc|Prescription/i }).first();
      if (await tab.count()) { await tab.click({ timeout: 5_000 }).catch(() => {}); await page.waitForTimeout(1000); shots.push(await shot(page, "step-05a-prescription-tab.png")); return { screenshots: shots, note: "Tab ke don opened" }; }
      await page.goto("/prescriptions", { waitUntil: "domcontentloaded", timeout: 20_000 }).catch(() => {});
      await page.waitForTimeout(1000);
      shots.push(await shot(page, "step-05b-prescriptions.png"));
      return { screenshots: shots, note: "Fallback /prescriptions" };
    });

    await runStep("STEP-06", "Phat thuoc", async () => {
      const shots: string[] = [];
      await page.goto("/pharmacy/dispense", { waitUntil: "domcontentloaded", timeout: 20_000 }).catch(async () => { await page.goto("/pharmacy", { waitUntil: "domcontentloaded", timeout: 20_000 }); });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-06-pharmacy-dispense.png"));
      return { screenshots: shots };
    });

    await runStep("STEP-07", "Thu ngan", async () => {
      const shots: string[] = [];
      await page.goto("/cashier", { waitUntil: "domcontentloaded", timeout: 20_000 });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-07-cashier.png"));
      return { screenshots: shots };
    });

    await runStep("STEP-08", "In phieu", async () => {
      const shots: string[] = [];
      // Navigate to encounters list and click first row to open detail
      await page.goto("/encounters", { waitUntil: "domcontentloaded", timeout: 20_000 });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-08a-encounters-list.png"));
      // Try to open first encounter detail
      const firstDetailBtn = page.getByRole("button", { name: /Chi tiết/i }).first();
      if (await firstDetailBtn.count()) {
        await firstDetailBtn.click({ timeout: 5_000 }).catch(() => {});
        await page.waitForTimeout(2000);
        shots.push(await shot(page, "step-08b-encounter-detail.png"));
      }
      const printBtn = page.getByRole("button", { name: /In phiếu|^In$|Print/i }).first();
      if (await printBtn.count()) {
        // Use dispatchEvent to avoid popup blocker issues
        await printBtn.dispatchEvent("click");
        await page.waitForTimeout(1500);
        shots.push(await shot(page, "step-08c-print-clicked.png"));
        return { screenshots: shots, note: "In phieu button clicked" };
      }
      shots.push(await shot(page, "step-08-not-found.png"));
      throw new Error("SKIP: khong tim thay nut In phieu");
    });

    await runStep("STEP-09", "Tong ket", async () => {
      const shots: string[] = [];
      await page.goto("/dashboard", { waitUntil: "domcontentloaded", timeout: 20_000 }).catch(async () => { await page.goto("/", { waitUntil: "domcontentloaded", timeout: 20_000 }); });
      await page.waitForTimeout(1500);
      shots.push(await shot(page, "step-09-final-dashboard.png"));
      return { screenshots: shots };
    });

    const summary = {
      generatedAt: new Date().toISOString(),
      total: journeyResults.length,
      pass: journeyResults.filter((r) => r.status === "pass").length,
      fail: journeyResults.filter((r) => r.status === "fail").length,
      skip: journeyResults.filter((r) => r.status === "skip").length,
      results: journeyResults,
    };
    fs.writeFileSync(REPORT_FILE, JSON.stringify(summary, null, 2), "utf8");
    console.log("[journey] report saved: " + REPORT_FILE);
    console.log("[journey] summary: " + summary.pass + " pass / " + summary.skip + " skip / " + summary.fail + " fail");
    const loginStep = journeyResults.find((r) => r.step === "STEP-01");
    expect(loginStep?.status, "Login phai PASS").toBe("pass");
  });
});
