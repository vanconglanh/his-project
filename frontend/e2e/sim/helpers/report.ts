/**
 * helpers/report.ts — Bọc từng bước mô phỏng bằng runStep() để KHÔNG BAO GIỜ hard-fail giữa
 * chừng: lỗi bắt đầu bằng "SKIP:" -> ghi nhận SKIP, lỗi khác -> FAIL (chỉ ghi log, không throw
 * ra ngoài). Chụp screenshot khi bước không PASS. saveReport() ghi báo cáo JSON tổng hợp.
 *
 * Lưu ý: `results` là state module-level dùng chung trong suốt tiến trình worker Playwright.
 * Với workers=1 (cấu hình playwright.sim.config.ts), nếu chạy nhiều spec file trong cùng 1 lần
 * (vd cả clinic-simulation.spec.ts lẫn exceptions.spec.ts), các lần gọi saveReport() sau sẽ chứa
 * TOÀN BỘ lịch sử tích luỹ tính từ đầu tiến trình, không chỉ riêng spec file đó.
 */
import * as fs from "fs";
import * as path from "path";
import type { Page } from "@playwright/test";

export type StepStatus = "PASS" | "FAIL" | "SKIP";

export interface StepMeta {
  day?: number;
  patientCode?: string;
  patientName?: string;
}

export interface StepRecord extends StepMeta {
  step: string;
  status: StepStatus;
  ms: number;
  error?: string;
  screenshot?: string;
}

const results: StepRecord[] = [];
let shotCounter = 0;

/** Trả về tham chiếu (copy nông) tới danh sách kết quả đã ghi nhận tới thời điểm gọi. */
export function getResults(): StepRecord[] {
  return [...results];
}

function slug(s: string): string {
  const cleaned = s.replace(/[^a-zA-Z0-9]+/g, "_").replace(/^_+|_+$/g, "");
  return (cleaned || "step").slice(0, 80);
}

const SHOTS_DIR = path.resolve(__dirname, "..", "..", "..", "test-results", "sim-shots");

async function captureScreenshot(page: Page, name: string): Promise<string | undefined> {
  try {
    fs.mkdirSync(SHOTS_DIR, { recursive: true });
    shotCounter += 1;
    const file = path.join(SHOTS_DIR, `${String(shotCounter).padStart(4, "0")}_${slug(name)}.png`);
    await page.screenshot({ path: file, timeout: 5000 });
    return file;
  } catch {
    return undefined;
  }
}

/**
 * Chạy 1 bước mô phỏng — KHÔNG BAO GIỜ throw ra ngoài (trừ khi bản thân runStep bị lỗi hạ tầng).
 * - fn() throw lỗi có message bắt đầu "SKIP:" -> ghi nhận SKIP (chủ động bỏ qua, có ghi chú).
 * - fn() throw lỗi khác -> ghi nhận FAIL, chụp screenshot minh chứng (nếu có page).
 * - fn() chạy xong không lỗi -> ghi nhận PASS.
 */
export async function runStep(
  name: string,
  page: Page | null,
  fn: () => Promise<void>,
  meta?: StepMeta
): Promise<StepStatus> {
  const start = Date.now();
  try {
    await fn();
    const ms = Date.now() - start;
    results.push({ step: name, status: "PASS", ms, ...meta });
    console.log(`[sim] PASS  ${name} (${ms}ms)`);
    return "PASS";
  } catch (e: unknown) {
    const ms = Date.now() - start;
    const message = e instanceof Error ? e.message : String(e);
    const status: StepStatus = message.startsWith("SKIP:") ? "SKIP" : "FAIL";
    const screenshot = page ? await captureScreenshot(page, `${status}_${name}`) : undefined;
    results.push({ step: name, status, ms, error: message, screenshot, ...meta });
    console.log(`[sim] ${status}  ${name} (${ms}ms) :: ${message}`);
    return status;
  }
}

interface GroupCount {
  pass: number;
  fail: number;
  skip: number;
}

function bumpGroup(map: Record<string, GroupCount>, key: string, status: StepStatus): void {
  if (!map[key]) map[key] = { pass: 0, fail: 0, skip: 0 };
  if (status === "PASS") map[key].pass += 1;
  else if (status === "FAIL") map[key].fail += 1;
  else map[key].skip += 1;
}

/** Ghi báo cáo JSON tổng hợp PASS/FAIL/SKIP + chi tiết theo ngày/bệnh nhân ra filePath. */
export function saveReport(filePath: string): void {
  const total = results.length;
  const pass = results.filter((r) => r.status === "PASS").length;
  const fail = results.filter((r) => r.status === "FAIL").length;
  const skip = results.filter((r) => r.status === "SKIP").length;

  const byDay: Record<string, GroupCount> = {};
  const byPatient: Record<string, GroupCount> = {};

  for (const r of results) {
    const dayKey = r.day != null ? `Ngày ${r.day}` : "Không xác định";
    const patKey = r.patientCode
      ? `${r.patientCode}${r.patientName ? " - " + r.patientName : ""}`
      : "Không xác định";
    bumpGroup(byDay, dayKey, r.status);
    bumpGroup(byPatient, patKey, r.status);
  }

  const summary = {
    generatedAt: new Date().toISOString(),
    total,
    pass,
    fail,
    skip,
    byDay,
    byPatient,
    results,
  };

  const dir = path.dirname(filePath);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(summary, null, 2), "utf-8");

  console.log("\n========= CLINIC SIM SUMMARY =========");
  console.log(`Total: ${total} | PASS: ${pass} | FAIL: ${fail} | SKIP: ${skip}`);
  console.log(`Report JSON: ${filePath}`);
  console.log("=======================================\n");
}
