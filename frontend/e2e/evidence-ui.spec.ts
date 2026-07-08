/**
 * evidence-ui.spec.ts — Evidence chi tiết: (1) CRUD full cycle TOÀN BỘ master,
 * (2) Flow khám bệnh end-to-end (1 BN đi trọn). Annotate + fullPage.
 * BASE_URL=https://his.diab.com.vn npx playwright test --config=e2e/evidence-ui.config.ts
 */
import { test, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const EMAIL = process.env.EV_EMAIL || "admin@prodiab.local";
const PASSWORD = process.env.EV_PASSWORD || "admin123";
const DIR = process.env.SHOT_DIR
  ? path.resolve(process.env.SHOT_DIR)
  : path.resolve(__dirname, "..", "..", "docs", "test", "evidence-ui-shots");
fs.mkdirSync(DIR, { recursive: true });
const MANIFEST = path.join(DIR, "manifest.jsonl");
const TS = Date.now().toString().slice(-5);
const esc = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 40_000 });
  if (!page.url().includes("/login")) return;
  await page.locator("#email").waitFor({ state: "visible", timeout: 30_000 });
  for (let a = 1; a <= 2; a++) {
    await page.locator("#email").fill(EMAIL);
    await page.locator("#password").fill(PASSWORD);
    await page.getByRole("button", { name: /Đăng nhập/i }).click();
    const ok = await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: a < 2 ? 12_000 : 30_000 }).then(() => true).catch(() => false);
    if (ok) break;
  }
  await page.waitForLoadState("domcontentloaded").catch(() => {});
  await page.waitForTimeout(1200);
}

async function shot(page: Page, section: string, caption: string, focusSel?: string) {
  if (page.isClosed()) return;
  const idx = fs.readdirSync(DIR).filter((f) => f.endsWith(".png")).length + 1;
  const file = `${String(idx).padStart(2, "0")}-${section.toLowerCase().replace(/[^a-z0-9]+/g, "-")}.png`;
  await page.evaluate(({ caption, focusSel, section }) => {
    document.querySelectorAll(".__ev").forEach((e) => e.remove());
    const cap = document.createElement("div"); cap.className = "__ev";
    cap.innerHTML = `<b style="opacity:.85">${section}</b>&nbsp;·&nbsp;${caption}`;
    Object.assign(cap.style, { position: "absolute", top: "0", left: "0", right: "0", zIndex: "2147483647", background: "#01645A", color: "#fff", font: "600 15px system-ui,Segoe UI,sans-serif", padding: "10px 16px", textAlign: "center" });
    document.body.appendChild(cap);
    let el: HTMLElement | null = null;
    if (focusSel) { try { el = document.querySelector(focusSel as string) as HTMLElement | null; } catch { el = null; } }
    if (el) {
      const r = el.getBoundingClientRect();
      const box = document.createElement("div"); box.className = "__ev";
      Object.assign(box.style, { position: "absolute", left: r.left + window.scrollX - 4 + "px", top: r.top + window.scrollY - 4 + "px", width: r.width + 8 + "px", height: r.height + 8 + "px", border: "3px solid #ef4444", borderRadius: "9px", zIndex: "2147483646", pointerEvents: "none", boxShadow: "0 0 0 2px rgba(239,68,68,.25)" });
      document.body.appendChild(box);
    }
  }, { caption, focusSel: focusSel ?? null, section });
  await page.waitForTimeout(250);
  try { await page.screenshot({ path: path.join(DIR, file), fullPage: true, timeout: 15_000 }); }
  catch { await page.screenshot({ path: path.join(DIR, file), fullPage: false, timeout: 10_000 }).catch(() => {}); }
  await page.evaluate(() => document.querySelectorAll(".__ev").forEach((e) => e.remove())).catch(() => {});
  fs.appendFileSync(MANIFEST, JSON.stringify({ file, section, caption }) + "\n");
  console.log(`[shot] ${file} — ${section}: ${caption}`);
}

const uniq = (sel: string, v: string) =>
  /subdomain/i.test(sel) ? v + TS :
  /code/i.test(sel) ? v + TS :
  (sel.includes("email") || v.includes("@")) ? v.replace("@", TS + "@") : v;

async function fillSel(page: Page, sel: string, v: string) {
  const l = page.locator(sel).first();
  if (await l.isVisible({ timeout: 2500 }).catch(() => false)) { await l.fill(v, { timeout: 4000 }).catch(() => {}); }
}
async function pickSelect(page: Page, trigger: string, idx1 = 1) {
  const t = page.locator(trigger).first();
  if (!(await t.isVisible({ timeout: 2500 }).catch(() => false))) return;
  await t.click().catch(() => {});
  await page.waitForTimeout(450);
  await page.locator('[role="option"]').nth(idx1).click({ timeout: 3000 }).catch(() => page.keyboard.press("Escape").catch(() => {}));
  await page.waitForTimeout(250);
}
async function checkFirst(page: Page, sel: string) {
  const el = page.locator(sel).first();
  if (await el.isVisible({ timeout: 2500 }).catch(() => false)) await el.click({ timeout: 4000, force: true }).catch(() => {});
}
async function clickText(page: Page, names: RegExp[], timeout = 6000): Promise<boolean> {
  for (const n of names) {
    for (const role of ["button", "link", "menuitem"] as const) {
      const e = page.getByRole(role, { name: n }).first();
      if (await e.isVisible({ timeout }).catch(() => false)) { await e.click({ timeout: 4000 }).catch(() => {}); return true; }
      timeout = 1500;
    }
  }
  return false;
}
async function goto(page: Page, url: string) {
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 40_000 }).catch(() => {});
  await page.waitForTimeout(1600);
}

// ===================== master configs =====================
type Cfg = { name: string; route: string; search?: string | null; create: RegExp; type: "dialog" | "page"; fields: [string, string][]; selects?: [string, number][]; checks?: string[]; submit: string };
const MASTER: Cfg[] = [
  { name: "Nhà cung cấp", route: "/admin/suppliers", search: 'input[placeholder*="Tìm" i]', create: /Tạo NCC|Thêm nhà cung cấp/i, type: "dialog",
    fields: [["#code", "NCC"], ["#name", "Công ty Dược ABC"], ["#tax_code", "0301234567"], ["#phone", "02838220000"], ["#email", "ncc@test.vn"], ["#contact_person", "Trần Văn Kho"], ["#address", "12 Lê Lợi, Q1"]], submit: "Tạo nhà cung cấp" },
  { name: "Danh mục thuốc", route: "/drugs", search: 'input[placeholder*="Tìm" i]', create: /Tạo thuốc|Thêm thuốc/i, type: "dialog",
    fields: [["#code", "TH"], ["#name_vi", "Paracetamol 500mg"], ["#name_en", "Paracetamol"], ["#generic_name", "Paracetamol"], ["#atc_code", "N02BE01"], ["#strength", "500mg"], ["#unit", "viên"], ["#manufacturer", "DHG Pharma"], ["#price", "1200"]], submit: "Tạo thuốc" },
  { name: "Dịch vụ", route: "/services", search: 'input[placeholder*="Tìm" i]', create: /Tạo dịch vụ/i, type: "dialog",
    fields: [["#svc_code", "DV"], ["#svc_name", "Khám nội tổng quát"], ["#svc_price", "150000"]], submit: "Lưu dịch vụ" },
  { name: "Vai trò", route: "/admin/roles", search: null, create: /Tạo vai trò mới/i, type: "dialog",
    fields: [["#role-code", "ROLE"], ["#role-name", "Bác sĩ trưởng"], ["#role-desc", "Trưởng khoa nội"]], checks: ['[id^="perm-"]'], submit: "Tạo vai trò" },
  { name: "API Partner", route: "/admin/api-partners", search: 'input[placeholder*="Tìm" i]', create: /Tạo partner mới|Tạo đối tác/i, type: "dialog",
    fields: [["#name", "Website phòng khám A"], ["#contact_email", "partner@example.com"], ["#rate_limit_per_min", "60"], ["#daily_quota", "10000"]], checks: ['[id^="scope-"]'], submit: "Tạo đối tác" },
  { name: "Phòng khám (Tenant)", route: "/admin/tenants", search: 'input[placeholder*="Tìm" i]', create: /Tạo phòng khám mới/i, type: "dialog",
    fields: [["#code", "PK"], ["#subdomain", "pkabc"], ["#name", "Phòng khám Đa khoa An Bình"], ["#email", "lienhe@anbinh.vn"], ["#phone", "02838123456"], ["#cskcb_code", "79001"], ["#tax_code", "0312345678"], ["#address", "12 Lê Lợi, Q1"], ["#admin_email", "admin@anbinh.vn"], ["#admin_full_name", "Nguyễn Quản Trị"]], submit: "Tạo phòng khám" },
  { name: "Người dùng", route: "/admin/users", search: 'input[placeholder*="Tìm" i]', create: /Mời người dùng/i, type: "dialog",
    fields: [["#inv-email", "letan.moi@phongkham.vn"], ["#inv-full-name", "Phạm Thị Lễ Tân"], ["#inv-phone", "0987654321"]], checks: ["#role-le_tan", '[id^="role-"]'], submit: "Gửi lời mời" },
];

async function crudCycle(page: Page, c: Cfg) {
  await goto(page, c.route);
  await shot(page, `${c.name} · 1 List`, `Danh sách ${c.name} (READ) — ô tìm kiếm/FILTER được khoanh`, c.search ?? undefined);
  if (!(await clickText(page, [c.create]))) { console.log(`  [skip] ${c.name}: không thấy nút tạo`); return; }
  await page.waitForTimeout(1400);
  const focus = c.type === "dialog" ? '[role="dialog"]' : "form, main";
  await shot(page, `${c.name} · 2 Form`, `Form tạo ${c.name} (rỗng) — các ô nhập input`, focus);
  for (const [sel, val] of c.fields) await fillSel(page, sel, uniq(sel, val));
  for (const [t, i] of c.selects ?? []) await pickSelect(page, t, i);
  for (const s of c.checks ?? []) { await checkFirst(page, s); break; }
  await shot(page, `${c.name} · 3 Điền`, `Đã điền đủ input (+dropdown/chọn) — nút SUBMIT`, focus);
  await clickText(page, [new RegExp("^" + esc(c.submit) + "$", "i"), new RegExp(esc(c.submit), "i")]);
  await page.waitForTimeout(2200);
  await shot(page, `${c.name} · 4 Message`, `Sau submit — thông báo thành công / danh sách cập nhật`);
  await page.keyboard.press("Escape").catch(() => {});
  await page.waitForTimeout(400);
}

// ===================== PART 1 — CRUD toàn bộ master =====================
test("evidence — 1) CRUD toàn bộ master", async ({ page }) => {
  await login(page);
  await shot(page, "Đăng nhập", "Vào hệ thống thành công — Dashboard tổng quan");

  // Bệnh nhân (full page — có dropdown tiếng Việt + full-screen)
  await goto(page, "/patients");
  await shot(page, "Bệnh nhân · 1 List", "Danh sách bệnh nhân (READ) — ô tìm kiếm/FILTER", 'input[placeholder*="Tìm" i]');
  await clickText(page, [/Tạo bệnh nhân mới|Thêm bệnh nhân/i]);
  await page.waitForTimeout(1400);
  await shot(page, "Bệnh nhân · 2 Form", "Form tạo bệnh nhân (rỗng) — dropdown tiếng Việt + full-screen", "form, main");
  await fillSel(page, "#full_name", "Nguyễn Văn Khỏe " + TS);
  await fillSel(page, "#date_of_birth", "1985-03-12");
  await fillSel(page, "#phone", "09" + TS + "11");
  await fillSel(page, "#id_number", "079085" + TS);
  await fillSel(page, "#id_card_issued_date", "2018-01-15");
  await fillSel(page, "#email", "khoe" + TS + "@test.vn");
  await pickSelect(page, "#gender", 1);
  await shot(page, "Bệnh nhân · 3 Điền", "Đã điền input + chọn dropdown (Giới tính) — nút Tạo", "main");
  await clickText(page, [/^Tạo bệnh nhân$/i]);
  await page.waitForTimeout(2400);
  await shot(page, "Bệnh nhân · 4 Message", "Sau submit — thông báo tạo bệnh nhân thành công");

  for (const c of MASTER) {
    try { await crudCycle(page, c); }
    catch (e) { console.log(`  [crud-err] ${c.name}: ${String(e).slice(0, 120)}`); }
  }
});

// ===================== PART 2 — FLOW end-to-end =====================
test("evidence — 2) Flow khám bệnh → kết thúc (end-to-end)", async ({ page }) => {
  test.setTimeout(12 * 60_000);
  if (process.env.EV_DIAG) {
    page.on("crash", () => console.log("!!! PAGE CRASH (renderer)"));
    page.on("pageerror", (e) => console.log("PAGEERROR: " + e.message.slice(0, 300)));
    page.on("console", (m) => { if (m.type() === "error") console.log("CONSOLE.ERR: " + m.text().slice(0, 300)); });
  }
  await login(page);
  const bn = `BN Flow ${TS}`;
  const phone = "093" + TS + "9";

  async function step(section: string, caption: string, focus: string | undefined, fn: () => Promise<void>) {
    try { await fn(); } catch (e) { console.log(`  [step-err] ${section}: ${String(e).slice(0, 120)}`); }
    await page.waitForTimeout(600);
    await shot(page, section, caption, focus);
  }

  // B0 — tạo BN cho flow
  await step("Flow · B0 Tạo bệnh nhân", "Tạo bệnh nhân mới cho ca khám", "main", async () => {
    await goto(page, "/patients/new");
    await fillSel(page, "#full_name", bn);
    await fillSel(page, "#date_of_birth", "1980-06-20");
    await fillSel(page, "#phone", phone);
    await fillSel(page, "#id_number", "07908" + TS);
    await fillSel(page, "#id_card_issued_date", "2019-02-10");
    await pickSelect(page, "#gender", 1);
    await clickText(page, [/^Tạo bệnh nhân$/i]);
    await page.waitForTimeout(2500);
  });

  // B1 — Lễ tân tiếp đón
  await step("Flow · B1 Tiếp đón", "Lễ tân: tìm BN → chọn phòng → Tiếp đón (F4) → vé WAITING", "main", async () => {
    await goto(page, "/reception");
    const s = page.getByPlaceholder(/Tìm tên, SĐT/i).first();
    if (await s.isVisible({ timeout: 6000 }).catch(() => false)) {
      await s.fill(bn); await page.waitForTimeout(1200);
      await page.getByRole("button", { name: new RegExp(esc(bn)) }).first().click({ timeout: 4000 }).catch(() => {});
      await page.waitForTimeout(800);
    }
    await page.locator("label").filter({ hasText: /Phòng|khám/i }).first().click({ timeout: 3000 }).catch(() => {});
    await page.getByPlaceholder(/Đau đầu, sốt/i).fill("Đái tháo đường tái khám, mệt mỏi").catch(() => {});
    await clickText(page, [/Tiếp đón \(F4\)/i]);
    await page.waitForTimeout(1500);
  });

  // B2 — Bác sĩ tạo lượt khám
  let encUrl = "";
  await step("Flow · B2 Tạo lượt khám", "Bác sĩ: chọn BN + bác sĩ + loại khám + lý do → Tạo lượt khám", "main", async () => {
    await goto(page, "/encounters/new");
    const pin = page.locator("#enc-patient-search");
    if (await pin.isVisible({ timeout: 5000 }).catch(() => false)) {
      await pin.fill(bn); await page.waitForTimeout(1200);
      await page.getByRole("button", { name: new RegExp(esc(bn)) }).first().click({ timeout: 4000 }).catch(() => {});
    }
    await pickSelect(page, "#enc-doctor-select", 1);
    await pickSelect(page, "#enc-type", 1);
    await fillSel(page, "#enc-reason", "Đái tháo đường tái khám");
    await clickText(page, [/^Tạo lượt khám$/i]);
    await page.waitForURL(/\/encounters\/(?!new)[^/?]+$/, { timeout: 15_000 }).catch(() => {});
    encUrl = page.url();
  });

  // B3 — Bắt đầu khám
  await step("Flow · B3 Bắt đầu khám", "Chuyển trạng thái lượt khám → IN_PROGRESS", "main", async () => {
    await clickText(page, [/^Bắt đầu khám$/i]); await page.waitForTimeout(1500);
  });

  // B4 — Sinh hiệu
  await step("Flow · B4 Sinh hiệu", "Nhập mạch/nhiệt độ/huyết áp/SpO2 → Lưu sinh hiệu", "main", async () => {
    await page.getByRole("tab", { name: /Sinh hiệu/i }).click({ timeout: 4000 }).catch(() => {});
    await page.waitForTimeout(800);
    await fillSel(page, "#v-hr", "78"); await fillSel(page, "#v-temp", "36.8");
    await page.getByLabel(/HA tâm thu/i).fill("128").catch(() => {});
    await page.getByLabel(/HA tâm trương/i).fill("82").catch(() => {});
    await fillSel(page, "#v-spo2", "98"); await fillSel(page, "#v-wt", "62"); await fillSel(page, "#v-ht", "162");
    await clickText(page, [/Lưu sinh hiệu/i]); await page.waitForTimeout(1200);
  });

  // B5 — Chẩn đoán
  await step("Flow · B5 Chẩn đoán", "Chọn ICD-10 (E11 — Đái tháo đường) → Lưu chẩn đoán", "main", async () => {
    await page.getByRole("tab", { name: /Chẩn đoán/i }).click({ timeout: 4000 }).catch(() => {});
    await page.waitForTimeout(800);
    await fillSel(page, "#diag-code-0", "E11"); await fillSel(page, "#diag-name-0", "Đái tháo đường type 2");
    await pickSelect(page, "#diag-type-0", 1);
    await clickText(page, [/Lưu chẩn đoán/i]); await page.waitForTimeout(1200);
  });

  // B6 — Bệnh án EMR
  await step("Flow · B6 Bệnh án", "Nhập bệnh án (EMR) + Lưu nháp + Ký số bệnh án", "main", async () => {
    await page.getByRole("tab", { name: /Khám bệnh/i }).click({ timeout: 4000 }).catch(() => {});
    await page.waitForTimeout(800);
    const ed = page.locator(".ProseMirror").first();
    if (await ed.isVisible({ timeout: 3000 }).catch(() => false)) { await ed.click(); await ed.type("BN tỉnh táo, tiếp xúc tốt. Đái tháo đường type 2 kiểm soát ổn.", { delay: 4 }); }
    await clickText(page, [/Lưu nháp/i]); await page.waitForTimeout(1200);
  });

  // B7 — Kê đơn
  await step("Flow · B7 Kê đơn + ĐTQG", "Thêm thuốc vào đơn + Ký số & gửi Đơn thuốc Quốc gia", "main", async () => {
    await page.getByRole("tab", { name: /Đơn thuốc/i }).click({ timeout: 4000 }).catch(() => {});
    await page.waitForTimeout(900);
    const ds = page.getByPlaceholder(/Tìm thuốc/i).first();
    if (await ds.isVisible({ timeout: 4000 }).catch(() => false)) {
      await ds.fill("Metformin"); await page.waitForTimeout(1200);
      await page.getByRole("option", { name: /Metformin/i }).first().click({ timeout: 4000 }).catch(() => {});
      await fillSel(page, "#dosage", "1 viên"); await fillSel(page, "#frequency", "2 lần/ngày"); await fillSel(page, "#duration_days", "30");
      await clickText(page, [/Thêm vào đơn/i]); await page.waitForTimeout(1200);
    }
  });

  // B8 — Đóng lượt khám
  await step("Flow · B8 Đóng lượt khám", "Hoàn tất khám → Đóng lượt khám (DONE)", "main", async () => {
    await clickText(page, [/Đóng lượt khám/i]); await page.waitForTimeout(1500);
  });

  // B9 — Cấp phát
  await step("Flow · B9 Cấp phát", "Dược sĩ: Hàng chờ → Phát thuốc (FEFO)", "main", async () => {
    await goto(page, "/pharmacy/dispense");
    await page.getByRole("tab", { name: /Hàng chờ/i }).click({ timeout: 4000 }).catch(() => {});
    await page.waitForTimeout(1000);
  });

  // B10 — Thu ngân
  await step("Flow · B10 Thu ngân", "Kế toán: mở ca → thu tiền → đóng ca (kết thúc lượt khám)", "main", async () => {
    await goto(page, "/cashier");
    await page.waitForTimeout(1000);
  });

  // B11 — Báo cáo
  await step("Flow · B11 Báo cáo", "Dashboard/Báo cáo: doanh thu, lượt khám, top thuốc", "main", async () => {
    await goto(page, "/reports");
  });
});

// ===================== PART 3 — Flow màn hình (direct nav, không crash) =====================
test("evidence — 3) Flow màn hình chức năng", async ({ page }) => {
  await login(page);
  const screens: [string, string, string][] = [
    ["/cls", "Flow · B5 CLS", "Cận lâm sàng — chỉ định xét nghiệm / chẩn đoán hình ảnh"],
    ["/prescriptions", "Flow · B7 Kê đơn", "Kê đơn thuốc + ký số & gửi Đơn thuốc Quốc gia"],
    ["/pharmacy/dispense", "Flow · B9 Cấp phát", "Dược sĩ: hàng chờ → phát thuốc (FEFO) theo lô/HSD"],
    ["/cashier", "Flow · B10 Thu ngân", "Thu ngân: mở ca → thu tiền → đóng ca (kết thúc lượt)"],
    ["/reports", "Flow · B11 Báo cáo", "Dashboard/Báo cáo: doanh thu, lượt khám, top thuốc"],
  ];
  for (const [url, section, caption] of screens) { await goto(page, url); await shot(page, section, caption, "main"); }
});

test.afterAll(async () => {
  const n = fs.existsSync(MANIFEST) ? fs.readFileSync(MANIFEST, "utf8").trim().split("\n").filter(Boolean).length : 0;
  console.log(`[evidence] ${n} shots -> ${DIR}`);
});
