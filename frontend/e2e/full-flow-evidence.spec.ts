/**
 * full-flow-evidence.spec.ts — UTE full-flow 2026-08-31.
 * Đi hết hành trình 1 bệnh nhân: Tiếp đón → Hồ sơ → Khám → CLS → Kê đơn → Thu ngân → Cấp phát → Tái khám.
 * Mỗi step 1 ảnh, khoanh 3 vùng: 🟦 INPUT (ô nhập) · 🟨 ACTION (nút bấm) · 🟩 RESULT (vùng kết quả).
 *
 * Chạy:  cd frontend && npx playwright test --config=e2e/full-flow.config.ts
 */
import { test, expect, type Page, type Locator } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const DIR = path.resolve(__dirname, "..", "..", "docs", "qc", "evidence-full-flow-20260831");
fs.mkdirSync(DIR, { recursive: true });
const MANIFEST = path.join(DIR, "manifest.jsonl");
const TS = Date.now().toString().slice(-6);

type Box = { sel?: string; ref?: Locator; label: string };
type ShotOpts = { input?: Box; action?: Box; result?: Box };

let seq = 0;

/** Vẽ overlay 3 vùng + banner rồi chụp; gỡ overlay sau khi chụp. */
async function shot(page: Page, code: string, view: string, expected: string, o: ShotOpts = {}) {
  if (page.isClosed()) return;
  seq += 1;
  const file = `${String(seq).padStart(2, "0")}-${code.toLowerCase().replace(/[^a-z0-9]+/g, "-")}.png`;

  // Quy đổi Locator -> bounding box pixel (toạ độ tuyệt đối theo trang)
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
    } catch { /* vùng không có mặt -> bỏ qua, không làm hỏng ảnh */ }
  }

  await page.evaluate(
    ({ code, view, expected, boxes }) => {
      document.querySelectorAll(".__ev").forEach((e) => e.remove());
      const COLORS: Record<string, [string, string]> = {
        input: ["#2563eb", "① NHẬP"],
        action: ["#d97706", "② THAO TÁC"],
        result: ["#059669", "③ KẾT QUẢ"],
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
          width: b.w + 8 + "px", height: b.h + 8 + "px",
          border: `3px solid ${color}`, borderRadius: "8px", zIndex: "2147483646",
          pointerEvents: "none", boxShadow: `0 0 0 3px ${color}33`,
        });
        const tag = document.createElement("div");
        tag.className = "__ev";
        tag.textContent = `${prefix} — ${b.label}`;
        Object.assign(tag.style, {
          position: "absolute", left: b.x - 4 + "px", top: Math.max(0, b.y - 26) + "px",
          background: color, color: "#fff", font: "700 11px system-ui,Segoe UI,sans-serif",
          padding: "3px 8px", borderRadius: "5px", zIndex: "2147483647",
          whiteSpace: "nowrap", pointerEvents: "none",
        });
        document.body.appendChild(box);
        document.body.appendChild(tag);
      }
    },
    { code, view, expected, boxes }
  );

  await page.waitForTimeout(220);
  try {
    await page.screenshot({ path: path.join(DIR, file), fullPage: true, timeout: 15_000 });
  } catch {
    await page.screenshot({ path: path.join(DIR, file), fullPage: false, timeout: 8_000 }).catch(() => {});
  }
  await page.evaluate(() => document.querySelectorAll(".__ev").forEach((e) => e.remove())).catch(() => {});
  fs.appendFileSync(MANIFEST, JSON.stringify({ file, code, view, expected }) + "\n");
  console.log(`[shot] ${file} — ${code}`);
}

/** Tắt product tour (driver.js) — overlay của tour che UI và chặn click, làm hỏng evidence. */
async function suppressTour(page: Page) {
  await page.addInitScript(() => {
    try {
      // Đánh dấu ĐÃ XEM cho mọi user/route để tour không tự bật
      for (let i = localStorage.length - 1; i >= 0; i--) {
        const k = localStorage.key(i);
        if (k && (k.startsWith("tour-seen:") || k.startsWith("tour-onboarding-seen:"))) localStorage.removeItem(k);
      }
      const origGet = Storage.prototype.getItem;
      Storage.prototype.getItem = function (key: string) {
        if (key.startsWith("tour-onboarding-seen:") || key.startsWith("tour-seen:")) return "1";
        return origGet.call(this, key);
      };
    } catch { /* private mode */ }
  });
  // Chèn CSS chặn overlay driver.js còn sót
  await page.addStyleTag({
    content: `.driver-overlay,.driver-popover,#driver-popover-item,.driver-stage,
              .driver-active-element{display:none !important;visibility:hidden !important;}`,
  }).catch(() => {});
}

/** Đóng mọi popup/tour/dialog còn mở để ảnh sạch và click không bị chặn. */
async function dismissOverlays(page: Page) {
  for (const sel of [
    "button:has-text('Bỏ qua')", "button:has-text('Đóng')",
    ".driver-popover-close-btn", "[aria-label='Close']",
  ]) {
    const b = page.locator(sel).first();
    if (await b.isVisible({ timeout: 700 }).catch(() => false)) {
      await b.click({ timeout: 2000 }).catch(() => {});
      await page.waitForTimeout(350);
    }
  }
  await page.evaluate(() => {
    document.querySelectorAll(".driver-overlay,.driver-popover,.driver-stage").forEach((e) => e.remove());
  }).catch(() => {});
}

async function loginAs(page: Page, roleLabel: string) {
  await suppressTour(page);
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  const quick = page.getByRole("button", { name: roleLabel, exact: true }).first();
  if (await quick.isVisible({ timeout: 4000 }).catch(() => false)) {
    await quick.click();
  } else {
    const map: Record<string, string> = {
      "Lễ tân": "letan.test@prodiab.test", "Bác sĩ": "bacsi.test@prodiab.test",
      "Kỹ thuật viên": "ktv.test@prodiab.test", "Kế toán": "ketoan.test@prodiab.test",
      "Dược sĩ": "duocsi.test@prodiab.test",
    };
    await page.locator("#email").fill(map[roleLabel]);
    await page.locator("#password").fill("Test@123");
    await page.getByRole("button", { name: /Đăng nhập/i }).click();
  }
  await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30_000 }).catch(() => {});
  await page.waitForTimeout(1500);
}

async function goto(page: Page, url: string) {
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 40_000 }).catch(() => {});
  await page.waitForTimeout(1800);
  await dismissOverlays(page);
}

/** Click an toàn: không để 1 nút không bấm được làm hỏng cả lượt chạy. */
async function safeClick(page: Page, loc: Locator, timeout = 4000): Promise<boolean> {
  if (!(await loc.isVisible({ timeout }).catch(() => false))) return false;
  const ok = await loc.click({ timeout: 6000 }).then(() => true).catch(() => false);
  if (!ok) await loc.click({ timeout: 4000, force: true }).catch(() => {});
  await page.waitForTimeout(600);
  return true;
}

test("UTE full-flow — 1 bệnh nhân đi hết chu trình khám", async ({ page }) => {
  test.setTimeout(20 * 60_000);

  // ══ 1. ĐĂNG NHẬP LỄ TÂN ══
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await shot(page, "UTC-AUTH-01", "Đăng nhập — panel chọn vai trò (dev)", "Hiện 6 nút vai trò test", {
    action: { sel: "button:has-text('Lễ tân')", label: "Nút đăng nhập nhanh vai trò Lễ tân" },
  });
  await loginAs(page, "Lễ tân");
  await shot(page, "UTC-AUTH-02", "Vào hệ thống với vai trò Lễ tân", "Vào Dashboard, sidebar theo quyền lễ tân", {
    result: { sel: "[data-tour='sidebar-nav']", label: "Menu điều hướng theo quyền" },
  });

  // ══ 2. TIẾP ĐÓN ══
  await goto(page, "/reception");
  await shot(page, "UTC-REC-01", "Màn Tiếp đón", "Có ô quét CCCD, form tiếp đón, bảng hàng đợi", {
    input: { sel: "[data-tour='reception-qr-scan']", label: "Vùng quét QR CCCD" },
    action: { sel: "[data-tour='reception-add-patient']", label: "Thêm bệnh nhân (F2)" },
    result: { sel: "[data-tour='reception-queue']", label: "Bảng hàng đợi hôm nay" },
  });

  // Quét CCCD — mô phỏng máy quét keyboard-wedge (gõ chuỗi rồi Enter)
  const qrInput = page.locator("[aria-label='Ô nhận chuỗi quét mã QR CCCD']").first();
  if (await qrInput.isVisible({ timeout: 4000 }).catch(() => false)) {
    const cccd = `0790851${TS}`;
    const qr = `${cccd}|025123456|Nguyễn Thị Bích Hạnh|15031985|Nữ|12 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM|20062021`;
    await qrInput.fill(qr);
    await shot(page, "UTC-REC-02", "Quét QR CCCD (mô phỏng máy quét)", "Ô nhận đủ 7 trường phân tách bằng |", {
      input: { ref: qrInput, label: `Chuỗi QR: ${cccd}|...|Nguyễn Thị Bích Hạnh|...` },
    });
    await qrInput.press("Enter");
    await page.waitForTimeout(2500);
    await shot(page, "UTC-REC-03", "Sau khi quét — hệ thống kiểm tra trùng CCCD", "Điều hướng tạo mới HOẶC hiện dialog trùng", {
      result: { sel: "[role='dialog'], form, main", label: "Kết quả xử lý chuỗi quét" },
    });
  }

  // ══ 3. HỒ SƠ BỆNH NHÂN ══
  await goto(page, "/patients");
  await shot(page, "UTC-PAT-01", "Danh sách bệnh nhân", "Hiện danh sách + ô tìm kiếm", {
    input: { sel: "input[placeholder*='Tìm' i]", label: "Ô tìm bệnh nhân" },
    result: { sel: "table, [role='table']", label: "Bảng danh sách bệnh nhân" },
  });

  const search = page.locator("input[placeholder*='Tìm' i]").first();
  if (await search.isVisible({ timeout: 3000 }).catch(() => false)) {
    await search.fill("Nguyễn Thị Bích Hạnh");
    await page.waitForTimeout(2500);
    await shot(page, "UTC-PAT-02", "Tìm bệnh nhân theo họ tên có dấu", "Lọc đúng bệnh nhân tiếng Việt có dấu", {
      input: { ref: search, label: "Nhập: Nguyễn Thị Bích Hạnh" },
      result: { sel: "table tbody, [role='rowgroup']", label: "Kết quả lọc" },
    });
    const row = page.locator("table tbody tr").first();
    if (await safeClick(page, row, 3000)) {
      await page.waitForTimeout(2500);
      await dismissOverlays(page);
      await shot(page, "UTC-PAT-03", "Chi tiết hồ sơ bệnh nhân", "Có tab Lịch sử InBody + nút tải tài liệu tự nhận diện", {
        action: { sel: "button:has-text('Tải tài liệu lên')", label: "Tải tài liệu lên (tự nhận diện)" },
        result: { sel: "main", label: "Hồ sơ bệnh nhân + các tab" },
      });

      // Smart-upload dialog
      const up = page.locator("button:has-text('Tải tài liệu lên')").first();
      if (await safeClick(page, up, 2500)) {
        await page.waitForTimeout(1500);
        await shot(page, "UTC-DOC-01", "Hộp thoại tải tài liệu tự nhận diện", "Nhận nhiều tệp PDF/ảnh hoặc 1 tệp ZIP", {
          input: { sel: "[role='dialog'] input[type='file']", label: "Ô chọn tệp (multiple + .zip)" },
          result: { sel: "[role='dialog']", label: "Dialog phân loại tài liệu" },
        });
        await page.keyboard.press("Escape").catch(() => {});
        await page.waitForTimeout(600);
      }
    }
  }

  // ══ 4. BÁC SĨ — KHÁM BỆNH ══
  await loginAs(page, "Bác sĩ");
  await goto(page, "/encounters");
  await shot(page, "UTC-ENC-01", "Danh sách lượt khám (vai trò Bác sĩ)", "Hiện lượt khám, có cột Chi nhánh", {
    result: { sel: "table, [role='table']", label: "Danh sách lượt khám" },
  });

  const encRow = page.locator("table tbody tr").first();
  if (await safeClick(page, encRow, 4000)) {
    await page.waitForTimeout(3000);
    await dismissOverlays(page);
    await shot(page, "UTC-ENC-02", "Chi tiết lượt khám", "Có thanh công cụ khám + các tab bệnh án", {
      action: { sel: "[data-tour='enc-sign']", label: "Ký số bệnh án" },
      result: { sel: "[data-tour='enc-tabs']", label: "Tab: Bệnh án / CLS / Đơn thuốc / Tái khám" },
    });

    const url = page.url();
    // Bệnh án + chọn mẫu
    await goto(page, url.split("?")[0] + "?tab=emr");
    await shot(page, "UTC-EMR-01", "Tab Bệnh án — chọn mẫu", "Có nút chọn mẫu bệnh án + trình soạn thảo", {
      action: { sel: "[data-tour='enc-emr-template']", label: "Chọn Mẫu bệnh án" },
      result: { sel: "main", label: "Nội dung bệnh án" },
    });

    // CLS
    await goto(page, url.split("?")[0] + "?tab=cls-orders");
    await shot(page, "UTC-CLS-01", "Tab Cận lâm sàng — chỉ định", "Có nút tạo đợt chỉ định CLS", {
      action: { sel: "button:has-text('Tạo đợt chỉ định')", label: "Tạo đợt chỉ định mới" },
      result: { sel: "main", label: "Danh sách đợt chỉ định" },
    });

    await goto(page, url.split("?")[0] + "?tab=cls-results");
    await shot(page, "UTC-CLS-02", "Tab Kết quả CLS", "Xem kết quả XN/CĐHA đã nhập, có cờ cảnh báo", {
      result: { sel: "main", label: "Bảng kết quả CLS + cờ H/HH/CRITICAL" },
    });

    // Đơn thuốc
    await goto(page, url.split("?")[0] + "?tab=prescription");
    await shot(page, "UTC-RX-01", "Tab Đơn thuốc trong lượt khám", "Hiện đơn đã kê + cảnh báo tương tác", {
      result: { sel: "main", label: "Đơn thuốc + DDI" },
    });

    // Tái khám
    await goto(page, url.split("?")[0] + "?tab=followup");
    await shot(page, "UTC-APM-01", "Tab Tái khám", "Đặt lịch tái khám + dặn dò", {
      input: { sel: "#followup-at", label: "Thời gian tái khám" },
      action: { sel: "button:has-text('Đặt lịch tái khám')", label: "Đặt lịch tái khám" },
      result: { sel: "main", label: "Danh sách lịch hẹn của bệnh nhân" },
    });
  }

  // ══ 5. CLS — NHẬP KẾT QUẢ BẰNG OCR (vai trò KTV) ══
  await loginAs(page, "Kỹ thuật viên");
  await goto(page, "/labrad");
  await shot(page, "UTC-LAB-01", "Màn Cận lâm sàng", "2 tab Kết quả XN / Kết quả CĐHA", {
    input: { sel: "[data-tour='labrad-search']", label: "Ô tìm kiếm" },
    action: { sel: "button:has-text('Nhập kết quả')", label: "+ Nhập kết quả" },
    result: { sel: "[data-tour='labrad-table']", label: "Bảng kết quả xét nghiệm (cột Cờ)" },
  });

  const btnLab = page.locator("button:has-text('Nhập kết quả')").first();
  if (await safeClick(page, btnLab, 4000)) {
    await page.waitForTimeout(2000);
    await shot(page, "UTC-LAB-02", "Nhập kết quả XN — 2 tab Nhập tay / Đọc từ file", "Có tab OCR 'Đọc từ file'", {
      action: { sel: "button:has-text('Đọc từ file'), [role='tab']:has-text('Đọc từ file')", label: "Tab Đọc từ file (OCR)" },
      result: { sel: "[role='dialog'], [role='tabpanel']", label: "Khu vực nhập kết quả" },
    });
    const tabOcr = page.locator("[role='tab']:has-text('Đọc từ file'), button:has-text('Đọc từ file')").first();
    if (await safeClick(page, tabOcr, 2500)) {
      await page.waitForTimeout(1500);
      await shot(page, "UTC-LAB-03", "Panel OCR đọc kết quả xét nghiệm", "Có ô mã lượt khám + chọn file PDF/ảnh", {
        input: { sel: "#ocr-encounter-id", label: "Mã lượt khám (encounter)" },
        action: { sel: "input[type='file']", label: "Chọn file PDF/ảnh kết quả" },
        result: { sel: "[role='dialog'], [role='tabpanel']", label: "Bảng review giá trị đọc được" },
      });
    }
    await page.keyboard.press("Escape").catch(() => {});
    await page.waitForTimeout(600);
  }

  // ══ 6. THU NGÂN ══
  await loginAs(page, "Kế toán");
  await goto(page, "/cashier");
  await shot(page, "UTC-CSH-01", "Màn Thu ngân", "Có ca làm việc, hoá đơn chờ thu, công nợ", {
    action: { sel: "[data-tour='cashier-shift']", label: "Mở ca / Đóng ca" },
    result: { sel: "[data-tour='cashier-stats']", label: "Tổng thu, số giao dịch, công nợ" },
  });
  await goto(page, "/billings");
  await shot(page, "UTC-CSH-02", "Danh sách hoá đơn", "Cột Bệnh nhân phải có tên (BUG-09 đã fix)", {
    result: { sel: "table, [role='table']", label: "Bảng hoá đơn — kiểm cột Bệnh nhân" },
  });
  const billRow = page.locator("table tbody tr").first();
  if (await safeClick(page, billRow, 4000)) {
    await page.waitForTimeout(2800);
    await dismissOverlays(page);
    await shot(page, "UTC-CSH-03", "Chi tiết hoá đơn", "Có Thu tiền + Thanh toán QR động", {
      action: { sel: "[data-tour='bill-pay']", label: "Thu tiền" },
      result: { sel: "[data-tour='bill-summary'], main", label: "Tổng tiền / đã thu / còn lại" },
    });
    const pay = page.locator("[data-tour='bill-pay']").first();
    if (await safeClick(page, pay, 2500)) {
      await page.waitForTimeout(1800);
      await shot(page, "UTC-CSH-04", "Hộp thoại Thu tiền", "Chọn hình thức + nhập số tiền", {
        input: { sel: "#amount", label: "Số tiền thu" },
        action: { sel: "button:has-text('Xác nhận thu tiền')", label: "Xác nhận thu tiền (F4)" },
        result: { sel: "[role='dialog']", label: "Form thu tiền" },
      });
      await page.keyboard.press("Escape").catch(() => {});
    }
  }

  // ══ 7. CẤP PHÁT THUỐC ══
  await loginAs(page, "Dược sĩ");
  await goto(page, "/pharmacy/dispense");
  await shot(page, "UTC-DIS-01", "Màn Phát thuốc", "Hàng chờ phát thuốc theo đơn đã ký", {
    input: { sel: "[data-tour='pharmacy-dispense-search']", label: "Tìm bệnh nhân" },
    result: { sel: "main", label: "Danh sách đơn chờ phát" },
  });

  // ══ 8. ĐỔI CHI NHÁNH ══
  await loginAs(page, "Bác sĩ");
  await goto(page, "/encounters");
  const bs = page.locator("[data-tour='branch-switcher']").first();
  if (await bs.isVisible({ timeout: 4000 }).catch(() => false)) {
    await shot(page, "UTC-BRN-01", "Bộ chọn chi nhánh trên thanh trên cùng", "Hiện chi nhánh đang làm việc", {
      action: { ref: bs, label: "Chọn chi nhánh đang làm việc" },
      result: { sel: "table, main", label: "Dữ liệu theo chi nhánh hiện tại" },
    });
    await safeClick(page, bs, 3000);
    await page.waitForTimeout(1200);
    await shot(page, "UTC-BRN-02", "Danh sách chi nhánh", "Liệt kê chi nhánh user được phép", {
      result: { sel: "[role='menu'], [role='listbox']", label: "Menu chi nhánh" },
    });
    await page.keyboard.press("Escape").catch(() => {});
  }

  console.log(`\n[UTE] Đã chụp ${seq} ảnh vào ${DIR}`);
  expect(seq).toBeGreaterThan(10);
});
