/**
 * full-flow-evidence-part2.spec.ts — UTE full-flow phần 2 (2026-08-31).
 * Đi thẳng vào các màn CHI TIẾT theo ID thật đã tạo ở phần API (step1..step6),
 * để chụp các bước mà phần 1 không tới được (tab bệnh án/CLS/đơn thuốc/tái khám,
 * hộp thoại thu tiền, QR động, đổi chi nhánh).
 *
 * ID truyền qua biến môi trường:
 *   ENC_ID, PAT_ID, BILL_ID, RX_ID
 */
import { test, expect, type Page, type Locator } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const DIR = path.resolve(__dirname, "..", "..", "docs", "qc", "evidence-full-flow-20260831");
fs.mkdirSync(DIR, { recursive: true });
const MANIFEST = path.join(DIR, "manifest.jsonl");

const ENC = process.env.ENC_ID || "";
const PAT = process.env.PAT_ID || "";
const BILL = process.env.BILL_ID || "";
const RX = process.env.RX_ID || "";

type Box = { sel?: string; ref?: Locator; label: string };
type ShotOpts = { input?: Box; action?: Box; result?: Box };

// tiếp tục đánh số từ phần 1
let seq = 100;

async function shot(page: Page, code: string, view: string, expected: string, o: ShotOpts = {}) {
  if (page.isClosed()) return;
  seq += 1;
  const file = `${seq - 80}-${code.toLowerCase().replace(/[^a-z0-9]+/g, "-")}.png`;

  const boxes: { x: number; y: number; w: number; h: number; label: string; kind: string }[] = [];
  for (const [kind, b] of [["input", o.input], ["action", o.action], ["result", o.result]] as const) {
    if (!b) continue;
    try {
      const loc = b.ref ?? (b.sel ? page.locator(b.sel).first() : null);
      if (!loc) continue;
      if (!(await loc.isVisible({ timeout: 1500 }).catch(() => false))) continue;
      const bb = await loc.boundingBox();
      if (!bb) continue;
      const sc = await page.evaluate(() => ({ x: window.scrollX, y: window.scrollY }));
      boxes.push({ x: bb.x + sc.x, y: bb.y + sc.y, w: bb.width, h: bb.height, label: b.label, kind });
    } catch { /* bỏ qua vùng không có */ }
  }

  await page.evaluate(
    ({ code, view, expected, boxes }) => {
      document.querySelectorAll(".__ev").forEach((e) => e.remove());
      const COLORS: Record<string, [string, string]> = {
        input: ["#2563eb", "① NHẬP"], action: ["#d97706", "② THAO TÁC"], result: ["#059669", "③ KẾT QUẢ"],
      };
      const cap = document.createElement("div");
      cap.className = "__ev";
      cap.innerHTML =
        `<span style="background:#F2C94C;color:#0b3b34;font-weight:800;padding:2px 9px;border-radius:5px;margin-right:9px">${code}</span>` +
        `<b>${view}</b>&nbsp;·&nbsp;Kỳ vọng: ${expected}`;
      Object.assign(cap.style, {
        position: "absolute", top: "0", left: "0", right: "0", zIndex: "2147483647",
        background: "#01645A", color: "#fff", font: "600 14px system-ui,Segoe UI,sans-serif",
        padding: "10px 14px", boxSizing: "border-box",
      });
      document.body.appendChild(cap);
      for (const b of boxes) {
        const [color, prefix] = COLORS[b.kind] ?? ["#ef4444", ""];
        const box = document.createElement("div");
        box.className = "__ev";
        Object.assign(box.style, {
          position: "absolute", left: b.x - 4 + "px", top: b.y - 4 + "px",
          width: b.w + 8 + "px", height: b.h + 8 + "px", border: `3px solid ${color}`,
          borderRadius: "8px", zIndex: "2147483646", pointerEvents: "none", boxShadow: `0 0 0 3px ${color}33`,
        });
        const tag = document.createElement("div");
        tag.className = "__ev";
        tag.textContent = `${prefix} — ${b.label}`;
        Object.assign(tag.style, {
          position: "absolute", left: b.x - 4 + "px", top: Math.max(0, b.y - 26) + "px",
          background: color, color: "#fff", font: "700 11px system-ui,Segoe UI,sans-serif",
          padding: "3px 8px", borderRadius: "5px", zIndex: "2147483647", whiteSpace: "nowrap", pointerEvents: "none",
        });
        document.body.appendChild(box); document.body.appendChild(tag);
      }
    },
    { code, view, expected, boxes }
  );

  await page.waitForTimeout(220);
  try { await page.screenshot({ path: path.join(DIR, file), fullPage: true, timeout: 15_000 }); }
  catch { await page.screenshot({ path: path.join(DIR, file), fullPage: false, timeout: 8_000 }).catch(() => {}); }
  await page.evaluate(() => document.querySelectorAll(".__ev").forEach((e) => e.remove())).catch(() => {});
  fs.appendFileSync(MANIFEST, JSON.stringify({ file, code, view, expected }) + "\n");
  console.log(`[shot] ${file} — ${code}`);
}

async function suppressTour(page: Page) {
  await page.addInitScript(() => {
    try {
      const origGet = Storage.prototype.getItem;
      Storage.prototype.getItem = function (key: string) {
        if (key.startsWith("tour-onboarding-seen:") || key.startsWith("tour-seen:")) return "1";
        return origGet.call(this, key);
      };
    } catch { /* ignore */ }
  });
}

async function dismissOverlays(page: Page) {
  await page.evaluate(() => {
    document.querySelectorAll(".driver-overlay,.driver-popover,.driver-stage").forEach((e) => e.remove());
  }).catch(() => {});
}

async function loginAs(page: Page, roleLabel: string) {
  await suppressTour(page);
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  const quick = page.getByRole("button", { name: roleLabel, exact: true }).first();
  if (await quick.isVisible({ timeout: 4000 }).catch(() => false)) await quick.click();
  await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30_000 }).catch(() => {});
  await page.waitForTimeout(1400);
}

async function goto(page: Page, url: string) {
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 40_000 }).catch(() => {});
  await page.waitForTimeout(2000);
  await dismissOverlays(page);
}

async function safeClick(page: Page, loc: Locator, timeout = 4000): Promise<boolean> {
  if (!(await loc.isVisible({ timeout }).catch(() => false))) return false;
  const ok = await loc.click({ timeout: 6000 }).then(() => true).catch(() => false);
  if (!ok) await loc.click({ timeout: 4000, force: true }).catch(() => {});
  await page.waitForTimeout(700);
  return true;
}

test("UTE full-flow phần 2 — màn chi tiết theo ID thật", async ({ page }) => {
  test.setTimeout(15 * 60_000);

  // ══ BÁC SĨ — các tab trong lượt khám ══
  await loginAs(page, "Bác sĩ");

  await goto(page, `/encounters/${ENC}?tab=emr`);
  await shot(page, "UTC-EMR-01", "Tab Bệnh án — đã ký số", "Bệnh án đã ký: nội dung KHÓA, không sửa được", {
    action: { sel: "[data-tour='enc-emr-template'], button:has-text('Mẫu bệnh án')", label: "Chọn Mẫu bệnh án" },
    result: { sel: "main", label: "Nội dung bệnh án + trạng thái ký số" },
  });

  await goto(page, `/encounters/${ENC}?tab=cls-orders`);
  await shot(page, "UTC-CLS-01", "Tab Cận lâm sàng — đợt chỉ định", "Hiện đợt chỉ định + trạng thái thanh toán", {
    action: { sel: "button:has-text('Tạo đợt chỉ định')", label: "Tạo đợt chỉ định mới" },
    result: { sel: "main", label: "Danh sách đợt chỉ định XN/CĐHA" },
  });

  await goto(page, `/encounters/${ENC}?tab=cls-results`);
  await shot(page, "UTC-CLS-02", "Tab Kết quả CLS — cờ cảnh báo (Bug A)", "HbA1c 8.1 phải có cờ CRITICAL, KHÔNG phải NORMAL", {
    result: { sel: "main", label: "Kết quả XN + cờ H/HH/CRITICAL + link file gốc (GAP-8)" },
  });

  await goto(page, `/encounters/${ENC}?tab=prescription`);
  await shot(page, "UTC-RX-01", "Tab Đơn thuốc", "Hiện 2 thuốc đã kê, trạng thái đã ký số", {
    result: { sel: "main", label: "Đơn thuốc + cảnh báo tương tác (DDI)" },
  });

  await goto(page, `/encounters/${ENC}?tab=followup`);
  await shot(page, "UTC-APM-01", "Tab Tái khám", "Đặt lịch tái khám + danh sách lịch hẹn", {
    input: { sel: "#followup-at", label: "Thời gian tái khám" },
    action: { sel: "button:has-text('Đặt lịch tái khám')", label: "Đặt lịch tái khám" },
    result: { sel: "main", label: "Lịch hẹn của bệnh nhân" },
  });

  // Đổi chi nhánh
  const bs = page.locator("[data-tour='branch-switcher']").first();
  if (await bs.isVisible({ timeout: 4000 }).catch(() => false)) {
    await shot(page, "UTC-BRN-01", "Bộ chọn chi nhánh trên thanh trên", "Hiện chi nhánh đang làm việc", {
      action: { ref: bs, label: "Chọn chi nhánh đang làm việc" },
      result: { sel: "main", label: "Dữ liệu đang lọc theo chi nhánh này" },
    });
    await safeClick(page, bs, 3000);
    await shot(page, "UTC-BRN-02", "Danh sách chi nhánh khả dụng", "Liệt kê chi nhánh user được phép truy cập", {
      result: { sel: "[role='menu'], [role='listbox']", label: "Menu chọn chi nhánh" },
    });
    await page.keyboard.press("Escape").catch(() => {});
  }

  // ══ HỒ SƠ BỆNH NHÂN — tab Lịch sử InBody ══
  await goto(page, `/patients/${PAT}`);
  const tabInbody = page.locator("button:has-text('Lịch sử InBody'), [role='tab']:has-text('InBody')").first();
  if (await safeClick(page, tabInbody, 4000)) {
    await shot(page, "UTC-INB-01", "Tab Lịch sử InBody", "Danh sách lần đo; báo cáo đã huỷ (GAP-1) không còn hiển thị", {
      action: { sel: "button:has-text('Nhập kết quả InBody')", label: "Nhập kết quả InBody" },
      result: { sel: "main", label: "Lịch sử các lần đo InBody" },
    });
  }

  // ══ KẾ TOÁN — hoá đơn + thu tiền + QR động ══
  await loginAs(page, "Kế toán");
  await goto(page, `/billings/${BILL}`);
  await shot(page, "UTC-CSH-01", "Chi tiết hoá đơn", "Có mục hoá đơn, tổng tiền, nút Thu tiền / QR", {
    action: { sel: "[data-tour='bill-pay']", label: "Thu tiền" },
    result: { sel: "[data-tour='bill-summary'], main", label: "Tổng tiền / đã thu / còn lại" },
  });

  const pay = page.locator("[data-tour='bill-pay']").first();
  if (await safeClick(page, pay, 4000)) {
    await shot(page, "UTC-CSH-02", "Hộp thoại Thu tiền", "Chọn hình thức (phím tắt 1–7) + nhập số tiền", {
      input: { sel: "#amount", label: "Số tiền thu (VND)" },
      action: { sel: "button:has-text('Xác nhận thu tiền')", label: "Xác nhận thu tiền (F4)" },
      result: { sel: "[role='dialog']", label: "Form thu tiền" },
    });
    await page.keyboard.press("Escape").catch(() => {});
    await page.waitForTimeout(700);
  }

  const qr = page.locator("[data-tour='bill-qr'], button:has-text('Thanh toán QR')").first();
  if (await safeClick(page, qr, 4000)) {
    await page.waitForTimeout(2500);
    await shot(page, "UTC-CSH-03", "Thanh toán QR động (VietQR)", "Sinh mã QR đúng số tiền còn phải thu", {
      result: { sel: "[role='dialog']", label: "Mã QR + số tiền + nút xác nhận đã thanh toán" },
    });
    await page.keyboard.press("Escape").catch(() => {});
  }

  // ══ CÔNG NỢ ══
  await goto(page, "/cashier/debts");
  await shot(page, "UTC-CSH-04", "Màn Công nợ", "Danh sách hoá đơn còn nợ, có tên bệnh nhân", {
    result: { sel: "main", label: "Bảng công nợ" },
  });

  console.log(`\n[UTE-p2] Đã chụp ${seq - 100} ảnh`);
  expect(seq).toBeGreaterThan(100);
});
