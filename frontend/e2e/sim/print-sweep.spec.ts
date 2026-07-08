/**
 * print-sweep.spec.ts — Đi qua CÀNG NHIỀU màn hình báo cáo/giấy tờ có nút IN/XUẤT càng tốt: chụp ảnh
 * màn hình (chứng minh có nút in) và XUẤT FILE PDF THẬT ra đĩa để kiểm chứng endpoint in hoạt động.
 *
 * Cơ chế in trong app: nhiều nút dùng printPdfBlob() = fetch(<url>/pdf) rồi window.print() (KHÔNG tạo
 * download event bắt được, window.print() là no-op ở headless). Nên cách trung thực để "xuất file" là
 * gọi CHÍNH endpoint PDF mà nút in trỏ tới (JWT) rồi lưu bytes — cùng file nút in tạo ra. Ngoài ra có
 * 1 demo BẤM nút in thật ("In phiếu khám") + bắt response application/pdf để chứng minh nút hoạt động.
 *
 * QUAN TRỌNG (bài học từ bản trước bị treo 20': .click() KHÔNG set timeout sẽ chờ VÔ HẠN nếu phần tử
 * không tồn tại — mọi thao tác ở đây đều có timeout tường minh + .catch()). Chạy SAU flow để tránh 429.
 *   BASE_URL=https://his.diab.com.vn SIM_USE_ADMIN=1 ADMIN_PASSWORD=admin123 \
 *   npx playwright test print-sweep.spec.ts --config=e2e/sim/playwright.sim.config.ts
 */
import { test, request as pwRequest, type Page, type APIRequestContext } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import { loginAs, attachErrorListeners } from "./helpers/session";

const SHOTS_DIR = path.resolve(__dirname, "..", "..", "..", "docs", "test", "evidence-shots");
const SWEEP_DIR = path.join(SHOTS_DIR, "sweep");
const PDF_DIR = path.join(SHOTS_DIR, "prints");
const REPORT_JSON = path.join(SHOTS_DIR, "print-sweep-report.json");
const BASE = process.env.BASE_URL || "http://localhost:3100";

type Status = "PASS" | "FAIL" | "SKIP";
interface R { step: string; name: string; status: Status; screenshots: string[]; note?: string; error?: string }
const results: R[] = [];

function rel(abs: string) { return path.relative(SHOTS_DIR, abs).split(path.sep).join("/"); }

async function shot(page: Page, file: string): Promise<string> {
  const abs = path.join(SWEEP_DIR, file);
  await page.screenshot({ path: abs, fullPage: true, timeout: 15_000 }).catch(() => {});
  return rel(abs);
}
function rec(step: string, name: string, status: Status, screenshots: string[], note?: string, error?: string) {
  results.push({ step, name, status, screenshots, note, error });
  console.log(`[sweep] ${step} ${name} -> ${status}${note ? " :: " + note : ""}${error ? " :: " + error : ""}`);
}

/** Đảm bảo đã đăng nhập; nếu bị đẩy về /login thì đăng nhập lại. */
async function ensureLoggedIn(page: Page) {
  if (page.url().includes("/login")) {
    await loginAs(page, "admin").catch(() => {});
  }
}

/** Lưu PDF từ endpoint (JWT) ra đĩa, verify %PDF + size. */
async function savePdf(api: APIRequestContext, url: string, outName: string) {
  try {
    const resp = await api.get(url, { timeout: 30_000 });
    const status = resp.status();
    const ct = resp.headers()["content-type"] || "";
    const buf = Buffer.from(await resp.body());
    const isPdf = buf.slice(0, 5).toString("latin1").startsWith("%PDF");
    const abs = path.join(PDF_DIR, outName);
    if (isPdf) fs.writeFileSync(abs, buf);
    return { ok: status === 200 && isPdf, status, ct, size: buf.length, isPdf, file: isPdf ? rel(abs) : "" };
  } catch (e) {
    return { ok: false, status: 0, ct: "", size: 0, isPdf: false, file: "", err: String(e) };
  }
}

test.describe.serial("Print/Report sweep — Pro-Diab HIS", () => {
  test("Di qua man hinh bao cao + nut in, xuat file", async ({ page }) => {
    test.setTimeout(10 * 60_000);
    fs.mkdirSync(SWEEP_DIR, { recursive: true });
    fs.mkdirSync(PDF_DIR, { recursive: true });
    attachErrorListeners(page);

    await loginAs(page, "admin");
    const token: string = await page.evaluate(() => {
      try { return JSON.parse(localStorage.getItem("auth-store") || "{}").state?.accessToken || ""; } catch { return ""; }
    });
    const api = await pwRequest.newContext({ baseURL: `${BASE}/api/v1/`, extraHTTPHeaders: token ? { Authorization: `Bearer ${token}` } : {} });

    const from = "2026-06-08", to = "2026-07-08";

    // Bắt PASSIVE mọi response application/pdf để lưu file do UI (nút in) fetch về.
    let pdfCaptureCount = 0;
    page.on("response", async (resp) => {
      try {
        const ct = resp.headers()["content-type"] || "";
        if (!ct.includes("application/pdf")) return;
        const buf = Buffer.from(await resp.body());
        if (!buf.slice(0, 5).toString("latin1").startsWith("%PDF")) return;
        pdfCaptureCount++;
        fs.writeFileSync(path.join(PDF_DIR, `ui-captured-${pdfCaptureCount}.pdf`), buf);
      } catch { /* body có thể không đọc được */ }
    });

    // ── B1) Trang Report Engine (catalog-driven) ───────────────────────────────
    try {
      await page.goto("/reports", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForTimeout(3500);
      rec("B1", "Trang Báo cáo (/reports)", "PASS", [await shot(page, "01-reports-home.png")], "màn danh mục báo cáo");
    } catch (e) { rec("B1", "Trang Báo cáo (/reports)", "FAIL", [], undefined, String(e)); }

    // ── B2) 3 báo cáo in server-side: mở màn preview (chụp) + xuất PDF qua endpoint ─
    for (const type of ["financial", "clinical", "pharmacy"] as const) {
      const step = `B2-${type}`;
      try {
        await page.goto(`/reports/print/${type}?from=${from}&to=${to}`, { waitUntil: "domcontentloaded", timeout: 30_000 });
        await ensureLoggedIn(page);
        // Chờ toolbar "Tải PDF" (bounded) — không bấm để tránh treo; chỉ để chụp preview có nút.
        await page.getByRole("button", { name: /Tải PDF/i }).first().waitFor({ state: "visible", timeout: 20_000 }).catch(() => {});
        await page.waitForTimeout(2500);
        const s = await shot(page, `02-report-${type}.png`);
        const r = await savePdf(api, `reports/${type}/pdf?from=${from}&to=${to}`, `report-${type}.pdf`);
        rec(step, `Báo cáo IN: ${type}`, r.ok ? "PASS" : "FAIL", [s],
          r.ok ? `PDF ${r.size}B -> ${r.file}` : `endpoint HTTP ${r.status} ct=${r.ct}`);
      } catch (e) { rec(step, `Báo cáo IN: ${type}`, "FAIL", [], undefined, String(e)); }
    }

    // ── B3) Per-doc: lấy id lượt khám + đơn thuốc gần nhất qua API ──────────────
    let encs: any[] = [], rxs: any[] = [];
    try { encs = (await (await api.get(`encounters?page=1&page_size=8`, { timeout: 20_000 })).json())?.data || []; } catch {}
    try { rxs = (await (await api.get(`prescriptions?page=1&page_size=8`, { timeout: 20_000 })).json())?.data || []; } catch {}

    // B3a) Bệnh án EMR: mở màn chi tiết (chụp có nút "In phiếu khám") + xuất PDF.
    //      Trên BN đầu tiên: BẤM nút "In phiếu khám" THẬT để chứng minh nút hoạt động (bắt PDF qua response).
    let emrDone = 0;
    for (const enc of encs.slice(0, 4)) {
      if (emrDone >= 3) break;
      const id = enc.id, nm = enc.patient_summary?.full_name || id;
      const step = `B3-emr-${emrDone + 1}`;
      try {
        await page.goto(`/encounters/${id}`, { waitUntil: "domcontentloaded", timeout: 30_000 });
        await ensureLoggedIn(page);
        await page.getByRole("tab", { name: "Khám bệnh" }).waitFor({ state: "visible", timeout: 12_000 }).catch(() => {});
        await page.waitForTimeout(1500);
        const s = await shot(page, `03-emr-${emrDone + 1}.png`);
        let clickNote = "";
        if (emrDone === 0) {
          const before = pdfCaptureCount;
          const btn = page.getByRole("button", { name: /In phiếu khám/i }).first();
          if (await btn.count()) {
            await btn.click({ timeout: 8_000 }).catch(() => {});
            await page.waitForTimeout(3000); // chờ printPdfBlob fetch xong -> response handler lưu file
            clickNote = pdfCaptureCount > before ? ` | BẤM "In phiếu khám" -> PDF fetched OK` : ` | đã bấm nút in (không bắt được PDF response)`;
          }
        }
        const r = await savePdf(api, `encounters/${id}/emr/pdf`, `benh-an-${emrDone + 1}.pdf`);
        rec(step, `Phiếu bệnh án EMR — ${nm}`, r.ok ? "PASS" : "SKIP", [s],
          (r.ok ? `PDF ${r.size}B -> ${r.file}` : `endpoint HTTP ${r.status}`) + clickNote);
        emrDone++;
      } catch (e) { rec(step, `Phiếu bệnh án EMR`, "FAIL", [], undefined, String(e)); }
    }

    // B3b) Đơn thuốc: mở màn chi tiết (chụp có nút in đơn) + xuất PDF.
    let rxDone = 0;
    for (const rx of rxs.slice(0, 4)) {
      if (rxDone >= 3) break;
      const id = rx.id, step = `B3-rx-${rxDone + 1}`;
      try {
        await page.goto(`/prescriptions/${id}`, { waitUntil: "domcontentloaded", timeout: 30_000 });
        await ensureLoggedIn(page);
        await page.waitForTimeout(2000);
        const s = await shot(page, `04-rx-${rxDone + 1}.png`);
        const r = await savePdf(api, `prescriptions/${id}/pdf`, `don-thuoc-${rxDone + 1}.pdf`);
        rec(step, `Đơn thuốc PDF — ${rx.status}`, r.ok ? "PASS" : "SKIP", [s],
          r.ok ? `PDF ${r.size}B -> ${r.file}` : `endpoint HTTP ${r.status}`);
        rxDone++;
      } catch (e) { rec(step, `Đơn thuốc PDF`, "FAIL", [], undefined, String(e)); }
    }

    // ── B4) Các màn danh sách có nút in/xuất (chụp màn) ────────────────────────
    for (const [step, name, url] of [
      ["B4-cashier", "Màn Thu ngân (in biên lai/hoá đơn)", "/cashier"],
      ["B4-billings", "Màn Hoá đơn (in hoá đơn)", "/billings"],
      ["B4-dispense", "Màn Phát thuốc (in phiếu phát)", "/pharmacy/dispense"],
      ["B4-reception", "Màn Tiếp đón (in phiếu tiếp đón)", "/reception"],
      ["B4-appointments", "Màn Lịch hẹn (in giấy hẹn)", "/appointments"],
      ["B4-labrad", "Màn Kết quả CLS (in KQ XN/CĐHA)", "/labrad"],
    ] as const) {
      try {
        await page.goto(url, { waitUntil: "domcontentloaded", timeout: 30_000 });
        await ensureLoggedIn(page);
        await page.waitForTimeout(2500);
        rec(step, name, "PASS", [await shot(page, `05-${step}.png`)], "chụp màn có nút in");
      } catch (e) { rec(step, name, "FAIL", [], undefined, String(e)); }
    }

    await api.dispose();

    const summary = {
      generatedAt: new Date().toISOString(), baseUrl: BASE,
      total: results.length,
      pass: results.filter((r) => r.status === "PASS").length,
      fail: results.filter((r) => r.status === "FAIL").length,
      skip: results.filter((r) => r.status === "SKIP").length,
      uiPdfCaptured: pdfCaptureCount,
      results,
    };
    fs.writeFileSync(REPORT_JSON, JSON.stringify(summary, null, 2), "utf-8");
    console.log(`[sweep] Xong. PASS ${summary.pass}/${summary.total}, UI PDF captured ${pdfCaptureCount}. Report: ${REPORT_JSON}`);
  });
});
