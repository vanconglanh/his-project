/**
 * ute-evidence.spec.ts — THỰC THI UTC (UTE / 実施) + evidence chuẩn Nhật.
 * Mỗi step 1 ảnh, banner ghi [Mã case] 観点 · 期待結果, khoanh đỏ vùng cần confirm.
 * Chuỗi step / master: A01 Load → A02 Form rỗng → C01 Submit rỗng (lỗi bắt buộc)
 *   → B01 Điền đủ → E01 Submit OK → F01 List sau tạo.
 * BASE_URL=https://his.diab.com.vn npx playwright test --config=e2e/evidence-ui.config.ts e2e/ute-evidence.spec.ts
 */
import { test, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const EMAIL = process.env.EV_EMAIL || "admin@prodiab.local";
const PASSWORD = process.env.EV_PASSWORD || "admin123";
const DIR = process.env.SHOT_DIR
  ? path.resolve(process.env.SHOT_DIR)
  : path.resolve(__dirname, "..", "..", "docs", "test", "ute-shots");
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
  await page.waitForTimeout(1200);
}

// caption = { case, view, expect }  — banner: [MÃ] 観点 · 期待: ...
async function shot(page: Page, code: string, view: string, expect: string, focusSel?: string) {
  if (page.isClosed()) return;
  const idx = fs.readdirSync(DIR).filter((f) => f.endsWith(".png")).length + 1;
  const file = `${String(idx).padStart(2, "0")}-${code.toLowerCase().replace(/[^a-z0-9]+/g, "-")}.png`;
  await page.evaluate(({ code, view, expect, focusSel }) => {
    document.querySelectorAll(".__ev").forEach((e) => e.remove());
    const cap = document.createElement("div"); cap.className = "__ev";
    cap.innerHTML = `<span style="background:#F2C94C;color:#0b3b34;font-weight:800;padding:2px 8px;border-radius:5px;margin-right:8px">${code}</span><b>${view}</b>&nbsp;·&nbsp;期待: ${expect}`;
    Object.assign(cap.style, { position: "absolute", top: "0", left: "0", right: "0", zIndex: "2147483647", background: "#01645A", color: "#fff", font: "600 14px system-ui,Segoe UI,sans-serif", padding: "9px 14px" });
    document.body.appendChild(cap);
    let el: HTMLElement | null = null;
    if (focusSel) { try { el = document.querySelector(focusSel as string) as HTMLElement | null; } catch { el = null; } }
    if (el) {
      const r = el.getBoundingClientRect();
      const box = document.createElement("div"); box.className = "__ev";
      Object.assign(box.style, { position: "absolute", left: r.left + window.scrollX - 4 + "px", top: r.top + window.scrollY - 4 + "px", width: r.width + 8 + "px", height: r.height + 8 + "px", border: "3px solid #ef4444", borderRadius: "9px", zIndex: "2147483646", pointerEvents: "none", boxShadow: "0 0 0 2px rgba(239,68,68,.25)" });
      document.body.appendChild(box);
    }
  }, { code, view, expect, focusSel: focusSel ?? null });
  await page.waitForTimeout(250);
  try { await page.screenshot({ path: path.join(DIR, file), fullPage: true, timeout: 15_000 }); }
  catch { await page.screenshot({ path: path.join(DIR, file), fullPage: false, timeout: 10_000 }).catch(() => {}); }
  await page.evaluate(() => document.querySelectorAll(".__ev").forEach((e) => e.remove())).catch(() => {});
  fs.appendFileSync(MANIFEST, JSON.stringify({ file, code, view, expect }) + "\n");
  console.log(`[shot] ${file} — ${code}: ${view}`);
}

const uniq = (sel: string, v: string) =>
  /subdomain/i.test(sel) ? v + TS :
  /code/i.test(sel) ? v + TS :
  (sel.includes("email") || v.includes("@")) ? v.replace("@", TS + "@") : v;

async function fillSel(page: Page, sel: string, v: string) {
  const l = page.locator(sel).first();
  if (await l.isVisible({ timeout: 2500 }).catch(() => false)) await l.fill(v, { timeout: 4000 }).catch(() => {});
}
async function pickSelect(page: Page, trigger: string, idx1 = 1) {
  const t = page.locator(trigger).first();
  if (!(await t.isVisible({ timeout: 2000 }).catch(() => false))) return;
  await t.click().catch(() => {});
  await page.waitForTimeout(400);
  await page.locator('[role="option"]').nth(idx1).click({ timeout: 3000 }).catch(() => page.keyboard.press("Escape").catch(() => {}));
  await page.waitForTimeout(200);
}
async function checkFirst(page: Page, sel: string) {
  const el = page.locator(sel).first();
  if (await el.isVisible({ timeout: 2000 }).catch(() => false)) await el.click({ timeout: 4000, force: true }).catch(() => {});
}
async function clickText(page: Page, names: RegExp[], timeout = 6000): Promise<boolean> {
  for (const n of names) {
    for (const role of ["button", "link", "menuitem"] as const) {
      const e = page.getByRole(role, { name: n }).first();
      if (await e.isVisible({ timeout }).catch(() => false)) { await e.click({ timeout: 4000 }).catch(() => {}); return true; }
      timeout = 1200;
    }
  }
  return false;
}
async function goto(page: Page, url: string) {
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 40_000 }).catch(() => {});
  await page.waitForTimeout(1500);
}
// selector vùng lỗi bắt buộc (zod) — để khoanh đỏ
const ERR = 'p.text-destructive, .text-destructive, [role="alert"], p[id$="-error"], .text-red-500';

type Cfg = {
  prefix: string; name: string; route: string; search?: string | null; create: RegExp; type: "dialog" | "page";
  fields: [string, string][]; selects?: [string, number][]; checks?: string[]; submit: string; mandatory: string;
};
const MASTER: Cfg[] = [
  { prefix: "NCC", name: "Nhà cung cấp", route: "/admin/suppliers", search: 'input[placeholder*="Tìm" i]', create: /Tạo NCC|Thêm nhà cung cấp/i, type: "dialog",
    fields: [["#code", "NCC"], ["#name", "Công ty Dược ABC"], ["#tax_code", "0301234567"], ["#phone", "02838220000"], ["#email", "ncc@test.vn"], ["#contact_person", "Trần Văn Kho"], ["#address", "12 Lê Lợi, Q1"]], submit: "Tạo nhà cung cấp", mandatory: "Mã + Tên NCC bắt buộc" },
  { prefix: "THU", name: "Danh mục thuốc", route: "/drugs", search: 'input[placeholder*="Tìm" i]', create: /Tạo thuốc|Thêm thuốc/i, type: "dialog",
    fields: [["#code", "TH"], ["#name_vi", "Paracetamol 500mg"], ["#name_en", "Paracetamol"], ["#generic_name", "Paracetamol"], ["#atc_code", "N02BE01"], ["#strength", "500mg"], ["#unit", "viên"], ["#manufacturer", "DHG Pharma"], ["#price", "1200"]], submit: "Tạo thuốc", mandatory: "Mã + Tên + Đơn vị + Dạng bắt buộc" },
  { prefix: "DV", name: "Dịch vụ", route: "/services", search: 'input[placeholder*="Tìm" i]', create: /Tạo dịch vụ/i, type: "dialog",
    fields: [["#svc_code", "DV"], ["#svc_name", "Khám nội tổng quát"], ["#svc_price", "150000"]], submit: "Lưu dịch vụ", mandatory: "Mã + Tên dịch vụ bắt buộc" },
  { prefix: "VT", name: "Vai trò", route: "/admin/roles", search: null, create: /Tạo vai trò mới/i, type: "dialog",
    fields: [["#role-code", "ROLE"], ["#role-name", "Bác sĩ trưởng"], ["#role-desc", "Trưởng khoa nội"]], checks: ['[id^="perm-"]'], submit: "Tạo vai trò", mandatory: "Mã + Tên + ≥1 quyền bắt buộc" },
  { prefix: "API", name: "API Partner", route: "/admin/api-partners", search: 'input[placeholder*="Tìm" i]', create: /Tạo partner mới|Tạo đối tác/i, type: "dialog",
    fields: [["#name", "Website phòng khám A"], ["#contact_email", "partner@example.com"], ["#rate_limit_per_min", "60"], ["#daily_quota", "10000"]], checks: ['[id^="scope-"]'], submit: "Tạo đối tác", mandatory: "Tên + ≥1 scope bắt buộc" },
  { prefix: "PK", name: "Phòng khám (Tenant)", route: "/admin/tenants", search: 'input[placeholder*="Tìm" i]', create: /Tạo phòng khám mới/i, type: "dialog",
    fields: [["#code", "PK"], ["#subdomain", "pkabc"], ["#name", "Phòng khám Đa khoa An Bình"], ["#email", "lienhe@anbinh.vn"], ["#phone", "02838123456"], ["#cskcb_code", "79001"], ["#tax_code", "0312345678"], ["#address", "12 Lê Lợi, Q1"], ["#admin_email", "admin@anbinh.vn"], ["#admin_full_name", "Nguyễn Quản Trị"]], submit: "Tạo phòng khám", mandatory: "Mã + Subdomain + Tên + Email + Admin bắt buộc" },
  { prefix: "ND", name: "Người dùng", route: "/admin/users", search: 'input[placeholder*="Tìm" i]', create: /Mời người dùng/i, type: "dialog",
    fields: [["#inv-email", "letan.moi@phongkham.vn"], ["#inv-full-name", "Phạm Thị Lễ Tân"], ["#inv-phone", "0987654321"]], checks: ["#role-le_tan", '[id^="role-"]'], submit: "Gửi lời mời", mandatory: "Email + Tên + ≥1 vai trò bắt buộc" },
];

async function ute(page: Page, c: Cfg) {
  const dlg = c.type === "dialog" ? '[role="dialog"]' : "form, main";
  // A01 — Load list + filter
  await goto(page, c.route);
  await shot(page, `${c.prefix}-A01`, `初期表示 · ${c.name} — Danh sách (READ)`, `Hiển thị danh sách + ô tìm kiếm/lọc`, c.search ?? undefined);
  // A02 — Form rỗng
  if (!(await clickText(page, [c.create]))) { console.log(`  [skip] ${c.name}: không thấy nút tạo`); return; }
  await page.waitForTimeout(1400);
  await shot(page, `${c.prefix}-A02`, `初期表示 · Form tạo rỗng`, `Đủ control, field rỗng, giá trị mặc định`, dlg);
  // C01 — Submit rỗng → lỗi bắt buộc
  await clickText(page, [new RegExp("^" + esc(c.submit) + "$", "i"), new RegExp(esc(c.submit), "i")]);
  await page.waitForTimeout(1000);
  const errFocus = (await page.locator(ERR).first().isVisible({ timeout: 1500 }).catch(() => false)) ? ERR : dlg;
  await shot(page, `${c.prefix}-C01`, `入力チェック · Submit khi rỗng`, `Chặn submit + báo lỗi: ${c.mandatory}`, errFocus);
  // B01 — Điền đủ field
  for (const [sel, val] of c.fields) await fillSel(page, sel, uniq(sel, val));
  for (const [t, i] of c.selects ?? []) await pickSelect(page, t, i);
  for (const s of c.checks ?? []) { await checkFirst(page, s); break; }
  await shot(page, `${c.prefix}-B01`, `項目設定 · Điền đủ input`, `Nhận đủ dữ liệu (text + dropdown/checkbox)`, dlg);
  // E01 — Submit hợp lệ → message
  await clickText(page, [new RegExp("^" + esc(c.submit) + "$", "i"), new RegExp(esc(c.submit), "i")]);
  await page.waitForTimeout(1300);
  await shot(page, `${c.prefix}-E01`, `DB登録 · Submit hợp lệ`, `Thông báo thành công / dialog đóng`);
  await page.keyboard.press("Escape").catch(() => {});
  await page.waitForTimeout(400);
  // F01 — List sau tạo
  await goto(page, c.route);
  await shot(page, `${c.prefix}-F01`, `登録後再表示 · Danh sách sau tạo`, `Bản ghi mới xuất hiện trong danh sách`, c.type === "dialog" ? "table tbody tr, [role='row']" : undefined);
}

test("UTE — thực thi UTC master + evidence", async ({ page }) => {
  test.setTimeout(18 * 60_000);
  await login(page);
  await shot(page, "LOGIN", "初期表示 · Đăng nhập hệ thống", "Vào Dashboard thành công");

  // Bệnh nhân (full page)
  try {
    await goto(page, "/patients");
    await shot(page, "BN-A01", "初期表示 · Bệnh nhân — Danh sách", "Hiển thị danh sách + ô tìm kiếm", 'input[placeholder*="Tìm" i]');
    await clickText(page, [/Tạo bệnh nhân mới|Thêm bệnh nhân/i]);
    await page.waitForTimeout(1400);
    await shot(page, "BN-A02", "初期表示 · Form bệnh nhân rỗng", "Dropdown tiếng Việt + full-screen; default VN/SERVICE", "form, main");
    await clickText(page, [/^Tạo bệnh nhân$/i]);
    await page.waitForTimeout(1000);
    const ef = (await page.locator(ERR).first().isVisible({ timeout: 1500 }).catch(() => false)) ? ERR : "form, main";
    await shot(page, "BN-C01", "入力チェック · Submit rỗng", "Chặn + báo lỗi: Họ tên bắt buộc (min 2)", ef);
    await fillSel(page, "#full_name", "Nguyễn Văn Khỏe " + TS);
    await fillSel(page, "#date_of_birth", "1985-03-12");
    await fillSel(page, "#phone", "09" + TS + "11");
    await fillSel(page, "#id_number", "079085" + TS);
    await fillSel(page, "#email", "khoe" + TS + "@test.vn");
    await pickSelect(page, "#gender", 1);
    await shot(page, "BN-B01", "項目設定 · Điền đủ + chọn Giới tính", "Nhận input + dropdown tiếng Việt", "main");
    await clickText(page, [/^Tạo bệnh nhân$/i]);
    await page.waitForTimeout(1600);
    await shot(page, "BN-E01", "DB登録 · Submit hợp lệ", "Thông báo tạo bệnh nhân thành công");
    await goto(page, "/patients");
    await shot(page, "BN-F01", "登録後再表示 · Danh sách sau tạo", "Bệnh nhân mới xuất hiện", "table tbody tr, [role='row']");
  } catch (e) { console.log(`  [err] Bệnh nhân: ${String(e).slice(0, 120)}`); }

  for (const c of MASTER) {
    try { await ute(page, c); }
    catch (e) { console.log(`  [err] ${c.name}: ${String(e).slice(0, 120)}`); }
  }
});
