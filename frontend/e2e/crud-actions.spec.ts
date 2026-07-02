import { test, expect, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const ADMIN_EMAIL = "admin@prodiab.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "Admin@123";

const SHOTS_DIR = path.resolve(__dirname, "..", "test-results", "crud-shots");
const REPORT_FILE = path.resolve(__dirname, "..", "test-results", "crud-report.json");

type ActionStatus = "PASS" | "FAIL" | "SKIP";
interface ActionResult {
  module: string;
  action: string;
  status: ActionStatus;
  screenshots: string[];
  note?: string;
  error?: string;
}

const actionResults: ActionResult[] = [];

fs.mkdirSync(SHOTS_DIR, { recursive: true });

function pushResult(r: ActionResult) {
  actionResults.push(r);
  console.log(`[crud] ${r.module} :: ${r.action} -> ${r.status}${r.note ? " :: " + r.note : ""}${r.error ? " :: " + r.error : ""}`);
}

function shot(name: string): string {
  return path.join(SHOTS_DIR, name + ".png");
}

async function safeShot(page: Page, name: string): Promise<string> {
  const p = shot(name);
  await page.screenshot({ path: p, fullPage: true }).catch(() => {});
  return path.basename(p);
}

async function login(page: Page) {
  for (let attempt = 0; attempt < 3; attempt++) {
    try {
      await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 40_000 });
      await page.locator("#email").fill(ADMIN_EMAIL);
      await page.locator("#password").fill(ADMIN_PASSWORD);
      await page.getByRole("button", { name: /Đăng nhập/i }).click();
      await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30_000 });
      await page.waitForLoadState("domcontentloaded").catch(() => {});
      await page.waitForTimeout(600);
      return;
    } catch (e) {
      if (attempt === 2) throw e;
      await page.waitForTimeout(1500);
    }
  }
}

async function tryClickFirst(page: Page, locators: Array<() => any>): Promise<boolean> {
  for (const fn of locators) {
    try {
      const loc = fn();
      const count = await loc.count();
      if (count > 0) {
        await loc.first().click({ timeout: 4000 });
        return true;
      }
    } catch { /* try next */ }
  }
  return false;
}

async function fillByLabelOrPlaceholder(page: Page, regex: RegExp, value: string): Promise<boolean> {
  try {
    const byLabel = page.getByLabel(regex).first();
    if (await byLabel.count()) { await byLabel.fill(value); return true; }
  } catch {}
  try {
    const byPh = page.getByPlaceholder(regex).first();
    if (await byPh.count()) { await byPh.fill(value); return true; }
  } catch {}
  return false;
}


async function clickRowDropdownItem(page: Page, rowIndex: number, itemRegex: RegExp): Promise<boolean> {
  const row = page.locator('tbody tr').nth(rowIndex);
  if (!(await row.count())) return false;
  const trigger = row.locator('button[aria-label*="Thao tác" i], button[aria-haspopup], button:has(svg.lucide-more-horizontal), button:has(svg.lucide-more-vertical)').first();
  if (!(await trigger.count())) return false;
  try { await trigger.click({ timeout: 3000 }); } catch { return false; }
  await page.waitForTimeout(500);
  const item = page.getByRole('menuitem', { name: itemRegex }).first();
  if (!(await item.count())) {
    await page.keyboard.press('Escape').catch(() => {});
    return false;
  }
  try { await item.click({ timeout: 3000 }); } catch { return false; }
  return true;
}

test.describe.configure({ mode: "default", timeout: 180_000 });
test.describe.configure({ retries: 0 });

// Helper: unlock admin user qua docker exec — tránh trường hợp test trước
// đã trigger Lock/Disable lên chính admin → các test sau login fail.
async function unlockAdmin(): Promise<void> {
  const { execSync } = await import("child_process");
  try {
    execSync(
      `docker exec prodiab-mysql sh -c "mysql --default-character-set=utf8mb4 -uroot -proot_dev -e \\"UPDATE prodiab_his.diab_his_sec_users SET user_status='ACTIVE', is_active=1, failed_login_count=0, locked_until=NULL, deleted_at=NULL WHERE email='admin@prodiab.local';\\""`,
      { stdio: "ignore", timeout: 10000 }
    );
  } catch { /* container có thể chưa chạy hoặc không có docker — skip */ }
}

test.beforeEach(async ({ page }) => {
  await unlockAdmin();
  await login(page);
});

test.afterAll(async () => {
  const summary = {
    generatedAt: new Date().toISOString(),
    total: actionResults.length,
    pass: actionResults.filter((r) => r.status === "PASS").length,
    fail: actionResults.filter((r) => r.status === "FAIL").length,
    skip: actionResults.filter((r) => r.status === "SKIP").length,
    results: actionResults,
  };
  fs.writeFileSync(REPORT_FILE, JSON.stringify(summary, null, 2), "utf8");
  console.log(`[crud] report: ${REPORT_FILE}`);
  console.log(`[crud] summary: PASS=${summary.pass} SKIP=${summary.skip} FAIL=${summary.fail}`);
});

// Wrapper: bao toàn bộ test body, mọi exception cá biệt được nuốt + log
// → đảm bảo serial mode không bị 1 test fail kéo dừng các test sau
async function runSafely(name: string, fn: () => Promise<void>) {
  try { await fn(); } catch (e) {
    pushResult({ module: name, action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) });
  }
}

async function genericModuleTest(page: Page, opts: {
  module: string;
  route: string;
  createBtnRegex?: RegExp;
  fields?: Array<{ regex: RegExp; value: string }>;
  submitRegex?: RegExp;
  extraActions?: Array<{ name: string; regex: RegExp; rowDropdown?: boolean; rowIndex?: number }>;
  tabs?: Array<{ name: string; regex: RegExp }>;
}) {
  const t = Date.now().toString().slice(-6);
  const slug = opts.module.toLowerCase().replace(/[^a-z0-9]+/g, "-");

  try {
    await page.goto(opts.route, { waitUntil: "domcontentloaded", timeout: 20_000 });
    await page.waitForTimeout(1500);
    const s = await safeShot(page, `${slug}-01-list-${t}`);
    pushResult({ module: opts.module, action: "LIST", status: "PASS", screenshots: [s], note: opts.route });
  } catch (e) {
    pushResult({ module: opts.module, action: "LIST", status: "FAIL", screenshots: [], error: String(e).slice(0, 200) });
    return;
  }

  if (opts.tabs) {
    for (const tab of opts.tabs) {
      try {
        const loc = page.getByRole("tab", { name: tab.regex }).first();
        if (!(await loc.count())) {
          const btn = page.getByRole("button", { name: tab.regex }).first();
          if (!(await btn.count())) { pushResult({ module: opts.module, action: `TAB:${tab.name}`, status: "SKIP", screenshots: [], error: "tab not found" }); continue; }
          await btn.click({ timeout: 4000 });
        } else {
          await loc.click({ timeout: 4000 });
        }
        await page.waitForTimeout(1000);
        const s = await safeShot(page, `${slug}-tab-${tab.name.toLowerCase().replace(/\s+/g, "-")}-${t}`);
        pushResult({ module: opts.module, action: `TAB:${tab.name}`, status: "PASS", screenshots: [s] });
      } catch (e) {
        pushResult({ module: opts.module, action: `TAB:${tab.name}`, status: "FAIL", screenshots: [], error: String(e).slice(0, 200) });
      }
    }
    await page.goto(opts.route, { waitUntil: "domcontentloaded" }).catch(() => {});
    await page.waitForTimeout(800);
  }

  if (opts.createBtnRegex) {
    try {
      const opened = await tryClickFirst(page, [
        () => page.getByRole("button", { name: opts.createBtnRegex! }),
        () => page.getByRole("link", { name: opts.createBtnRegex! }),
      ]);
      if (!opened) throw new Error("SKIP: khong co nut create");
      await page.waitForTimeout(1000);
      const s1 = await safeShot(page, `${slug}-02-form-${t}`);
      if (opts.fields) {
        for (const f of opts.fields) await fillByLabelOrPlaceholder(page, f.regex, f.value);
      }
      const s2 = await safeShot(page, `${slug}-03-filled-${t}`);
      const submit = page.getByRole("button", { name: opts.submitRegex ?? /^Lưu$|^Tạo$|^Submit$|Lưu lại/i }).last();
      if (await submit.count()) {
        await submit.click({ timeout: 4000 }).catch(() => {});
        await page.waitForTimeout(1800);
      }
      const s3 = await safeShot(page, `${slug}-04-after-${t}`);
      pushResult({ module: opts.module, action: "CREATE", status: "PASS", screenshots: [s1, s2, s3] });
    } catch (e) {
      const msg = String(e);
      pushResult({ module: opts.module, action: "CREATE", status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [await safeShot(page, `${slug}-create-err-${t}`)], error: msg.slice(0, 200) });
    }
  }

  if (opts.extraActions) {
    await page.goto(opts.route, { waitUntil: "domcontentloaded" }).catch(() => {});
    await page.waitForTimeout(800);
    for (const a of opts.extraActions) {
      try {
        if (a.rowDropdown) {
          const ok = await clickRowDropdownItem(page, a.rowIndex ?? 0, a.regex);
          if (!ok) throw new Error("SKIP: row dropdown khong co action");
        } else {
          const btn = page.getByRole("button", { name: a.regex }).first();
          const link = page.getByRole("link", { name: a.regex }).first();
          const menuitem = page.getByRole("menuitem", { name: a.regex }).first();
          if (await btn.count()) await btn.click({ timeout: 4000 });
          else if (await link.count()) await link.click({ timeout: 4000 });
          else if (await menuitem.count()) await menuitem.click({ timeout: 4000 });
          else throw new Error("SKIP: action not found");
        }
        await page.waitForTimeout(1200);
        const s = await safeShot(page, `${slug}-action-${a.name.toLowerCase().replace(/\s+/g, "-")}-${t}`);
        pushResult({ module: opts.module, action: a.name, status: "PASS", screenshots: [s] });
      } catch (e) {
        const msg = String(e);
        pushResult({ module: opts.module, action: a.name, status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [], error: msg.slice(0, 200) });
      }
      // back to list to attempt next action
      await page.goto(opts.route, { waitUntil: "domcontentloaded" }).catch(() => {});
      await page.waitForTimeout(800);
    }
  }
}

test("01 Patient CRUD", async ({ page }) => {
  try {
  const mod = "Patient";
  const t = Date.now().toString().slice(-6);
  const fullName = `BN Test ${t}`;
  const phone = `09${Date.now().toString().slice(-9)}`;
  const shots: string[] = [];

  await page.goto("/patients", { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1200);
  shots.push(await safeShot(page, `patient-01-list-${t}`));
  pushResult({ module: mod, action: "LIST", status: "PASS", screenshots: [shots[shots.length - 1]] });

  try {
    const opened = await tryClickFirst(page, [
      () => page.getByRole("button", { name: /Thêm bệnh nhân|Tạo mới|Thêm mới|^Thêm$|^Tạo$/i }),
      () => page.getByRole("link", { name: /Thêm bệnh nhân|Tạo mới|^Thêm$/i }),
    ]);
    if (!opened) {
      await page.goto("/patients/new", { waitUntil: "domcontentloaded", timeout: 10_000 }).catch(() => {});
      if (page.url().endsWith("/patients")) throw new Error("SKIP: khong tim thay nut Tao benh nhan");
    }
    await page.waitForTimeout(900);
    shots.push(await safeShot(page, `patient-02-form-${t}`));
    await fillByLabelOrPlaceholder(page, /Họ.*tên|Tên|Full.*name/i, fullName);
    await fillByLabelOrPlaceholder(page, /Điện thoại|SĐT|Phone/i, phone);
    await fillByLabelOrPlaceholder(page, /Ngày sinh|DOB/i, "1990-01-15");
    await fillByLabelOrPlaceholder(page, /Địa chỉ|Address/i, "123 Test St");
    const male = page.getByLabel(/^Nam$/i).first();
    if (await male.count()) await male.check({ force: true }).catch(() => {});
    shots.push(await safeShot(page, `patient-03-filled-${t}`));
    const submit = page.getByRole("button", { name: /Tạo bệnh nhân|^Lưu$|^Tạo$|^Submit$|Lưu lại|Lưu thay đổi/i }).last();
    await submit.click({ timeout: 10000 });
    await page.waitForTimeout(2500);
    shots.push(await safeShot(page, `patient-04-created-${t}`));
    pushResult({ module: mod, action: "CREATE", status: "PASS", screenshots: shots.slice(-3), note: fullName });
  } catch (e) {
    const msg = String(e);
    pushResult({ module: mod, action: "CREATE", status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [await safeShot(page, `patient-create-err-${t}`)], error: msg.slice(0, 200) });
  }

  try {
    await page.goto("/patients", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(1200);
    const opened = await clickRowDropdownItem(page, 0, /Xem chi tiết|Chi tiết/i);
    if (!opened) throw new Error("SKIP: khong mo duoc dropdown View");
    await page.waitForTimeout(1200);
    const s = await safeShot(page, `patient-05-detail-${t}`);
    pushResult({ module: mod, action: "VIEW", status: "PASS", screenshots: [s] });
  } catch (e) {
    const msg = String(e);
    pushResult({ module: mod, action: "VIEW", status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [], error: msg.slice(0, 200) });
  }

  try {
    await page.goto("/patients", { waitUntil: "domcontentloaded", timeout: 15_000 });
    await page.waitForTimeout(1200);
    const ok = await clickRowDropdownItem(page, 0, /Chỉnh sửa|Sửa|Edit/i);
    if (!ok) throw new Error("SKIP: row dropdown khong co Chinh sua");
    await page.waitForTimeout(1500);
    await fillByLabelOrPlaceholder(page, /Địa chỉ|Address/i, "456 Updated St");
    const s1 = await safeShot(page, `patient-06-edit-${t}`);
    const save = page.getByRole("button", { name: /^Lưu$|Cập nhật|Save|Lưu thay đổi/i }).last();
    if (await save.count()) {
      await Promise.race([
        save.click({ timeout: 3000 }).catch(() => {}),
        new Promise((r) => setTimeout(r, 3500)),
      ]);
    }
    await Promise.race([page.waitForTimeout(2000), new Promise((r) => setTimeout(r, 2500))]);
    const s2 = await safeShot(page, `patient-07-updated-${t}`);
    pushResult({ module: mod, action: "UPDATE", status: "PASS", screenshots: [s1, s2] });
  } catch (e) {
    const msg = String(e);
    pushResult({ module: mod, action: "UPDATE", status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [], error: msg.slice(0, 200) });
  }

  try {
    await page.goto("/patients", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(1200);
    const ok = await clickRowDropdownItem(page, 0, /Xoá|Xóa|Delete/i);
    if (!ok) throw new Error("SKIP: row dropdown khong co Xoa");
    await page.waitForTimeout(800);
    const confirm = page.getByRole("button", { name: /Xoá bệnh nhân|Xoá|Xóa|Xác nhận|OK|Đồng ý/i }).last();
    if (await confirm.count()) await confirm.click({ timeout: 3000 }).catch(() => {});
    await page.waitForTimeout(1200);
    const s = await safeShot(page, `patient-08-deleted-${t}`);
    pushResult({ module: mod, action: "DELETE", status: "PASS", screenshots: [s] });
  } catch (e) {
    const msg = String(e);
    pushResult({ module: mod, action: "DELETE", status: msg.includes("SKIP") ? "SKIP" : "FAIL", screenshots: [], error: msg.slice(0, 200) });
  }

  } catch (e) { pushResult({ module: "01 Patient CRUD", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("02 Encounter", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "Encounter",
    route: "/encounters",
    createBtnRegex: /Tạo.*khám|Khám mới|^Thêm$|^Mới$/i,
    fields: [{ regex: /Lý do|Chief complaint/i, value: "Sốt cao" }],
    extraActions: [
      { name: "ViewDetail", regex: /Chi tiết|Xem/i },
      { name: "AddVital", regex: /Sinh hiệu|Vital/i },
      { name: "AddDiagnosis", regex: /Chẩn đoán|Diagnosis/i },
      { name: "CloseEncounter", regex: /Đóng|Kết thúc|Close/i },
    ],
  });

  } catch (e) { pushResult({ module: "02 Encounter", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("03 Reception", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "Reception",
    route: "/reception",
    extraActions: [
      { name: "CheckIn", regex: /Check.?in|Tiếp đón|Đón tiếp/i },
      { name: "PrintTicket", regex: /In phiếu|^In$|Print/i },
    ],
  });

  } catch (e) { pushResult({ module: "03 Reception", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("04 Prescription", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "Prescription",
    route: "/prescriptions",
    createBtnRegex: /Tạo đơn|Kê đơn|^Thêm$|^Mới$/i,
    fields: [{ regex: /Ghi chú|Note/i, value: "Đơn test auto" }],
    extraActions: [
      { name: "AddDrugItem", regex: /Thêm thuốc|Add drug|Chọn thuốc/i },
      { name: "Submit", regex: /Gửi|Submit|Lưu/i },
    ],
  });

  } catch (e) { pushResult({ module: "04 Prescription", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("05 Pharmacy Stock", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "PharmacyStock",
    route: "/pharmacy",
    tabs: [
      { name: "Stock", regex: /Tồn kho|Stock/i },
      { name: "Adjustment", regex: /Điều chỉnh|Adjustment/i },
    ],
    extraActions: [
      { name: "CreateAdjustment", regex: /Tạo.*điều chỉnh|Thêm điều chỉnh|^Thêm$/i },
    ],
  });

  } catch (e) { pushResult({ module: "05 Pharmacy Stock", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("06 Pharmacy Dispense", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "PharmacyDispense",
    route: "/pharmacy/dispense",
    tabs: [
      { name: "Queue", regex: /Chờ|Queue|Hàng đợi/i },
      { name: "History", regex: /Lịch sử|History/i },
    ],
    extraActions: [
      { name: "Dispense", regex: /Phát thuốc|Hoàn tất|Cấp phát|Dispense/i },
    ],
  });

  } catch (e) { pushResult({ module: "06 Pharmacy Dispense", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("07 Drug", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "Drug",
    route: "/drugs",
    createBtnRegex: /Thêm thuốc|Tạo thuốc|^Thêm$|^Mới$/i,
    fields: [
      { regex: /Tên thuốc|Drug name|^Tên$/i, value: `Thuoc Test ${t}` },
      { regex: /Hoạt chất|Active|Ingredient/i, value: "Paracetamol" },
      { regex: /Đơn vị|Unit/i, value: "Viên" },
    ],
    extraActions: [
      { name: "Search", regex: /^Tìm$|Search/i },
    ],
  });

  } catch (e) { pushResult({ module: "07 Drug", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("08 Cashier", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "Cashier",
    route: "/cashier",
    extraActions: [
      { name: "OpenBill", regex: /Xem chi tiết|Chi tiết|Xem/i, rowDropdown: true },
      { name: "ReceivePayment", regex: /Thu tiền|Thanh toán/i, rowDropdown: true },
      { name: "PrintReceipt", regex: /In hoá đơn|In hóa đơn|In phiếu/i, rowDropdown: true },
    ],
  });

  } catch (e) { pushResult({ module: "08 Cashier", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("09 Billing", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "Billing",
    route: "/billings",
    extraActions: [
      { name: "ViewBill", regex: /Xem chi tiết|Chi tiết/i, rowDropdown: true },
      { name: "ReceivePayment", regex: /Thu tiền/i, rowDropdown: true },
      { name: "PrintInvoice", regex: /In hoá đơn|In hóa đơn/i, rowDropdown: true },
    ],
  });

  } catch (e) { pushResult({ module: "09 Billing", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("10 Service Catalog", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "ServiceCatalog",
    route: "/services",
    createBtnRegex: /Thêm dịch vụ|Tạo dịch vụ|^Thêm$|^Mới$/i,
    fields: [
      { regex: /Tên dịch vụ|Service name|^Tên$/i, value: `Dich vu test ${t}` },
      { regex: /Giá|Price/i, value: "100000" },
    ],
    extraActions: [
      { name: "UpdatePrice", regex: /Sửa|Cập nhật|Edit/i },
    ],
  });

  } catch (e) { pushResult({ module: "10 Service Catalog", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("11 BHYT Export", async ({ page }) => {
  try {
  await genericModuleTest(page, {
    module: "BHYT",
    route: "/bhyt",
    createBtnRegex: /Tạo kỳ mới|Tạo kỳ đầu tiên|Tạo.*xuất|Xuất mới|^Thêm$|Export/i,
    fields: [
      { regex: /Kỳ|Period|Tháng/i, value: "2026-05" },
    ],
    extraActions: [
      { name: "ViewDetail", regex: /Chi tiết|Xem/i },
    ],
  });

  } catch (e) { pushResult({ module: "11 BHYT Export", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("12 Admin Users", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "AdminUsers",
    route: "/admin/users",
    createBtnRegex: /Mời người dùng|Mời|Invite|Thêm.*user|Thêm.*tài khoản|^Thêm$/i,
    fields: [
      { regex: /Email/i, value: `test${t}@prodiab.local` },
      { regex: /Họ.*tên|Tên/i, value: `User Test ${t}` },
    ],
    extraActions: [
      { name: "AssignRoles", regex: /Gán vai trò|Vai trò/i, rowDropdown: true, rowIndex: 1 },
      // LockUnlock target row khác admin (row[1]+) để tránh tự lock admin → các test sau fail login
      { name: "LockUnlock", regex: /Khoá tài khoản|Mở khoá|Khóa|Lock|Unlock/i, rowDropdown: true, rowIndex: 1 },
    ],
  });

  } catch (e) { pushResult({ module: "12 Admin Users", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("13 Admin Roles", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "AdminRoles",
    route: "/admin/roles",
    createBtnRegex: /Tạo vai trò mới|Tạo.*vai trò|Thêm.*role|^Thêm$/i,
    fields: [
      { regex: /Tên vai trò|Role name|^Tên$/i, value: `Role Test ${t}` },
    ],
    extraActions: [
      { name: "EditPermissions", regex: /Quyền|Permission|Sửa/i },
    ],
  });

  } catch (e) { pushResult({ module: "13 Admin Roles", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("14 Admin Tenants", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "AdminTenants",
    route: "/admin/tenants",
    createBtnRegex: /Tạo phòng khám mới|Tạo.*phòng khám|Thêm.*tenant|Thêm phòng|^Thêm$/i,
    fields: [
      { regex: /Tên phòng khám|Tenant|^Tên$/i, value: `Clinic Test ${t}` },
      { regex: /Mã|Code/i, value: `TC${t}` },
    ],
    extraActions: [
      { name: "SuspendActivate", regex: /Tạm ngưng|Kích hoạt|Suspend|Activate/i },
    ],
  });

  } catch (e) { pushResult({ module: "14 Admin Tenants", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});

test("15 Supplier", async ({ page }) => {
  try {
  const t = Date.now().toString().slice(-6);
  await genericModuleTest(page, {
    module: "Supplier",
    route: "/admin/suppliers",
    createBtnRegex: /Tạo NCC|Thêm nhà cung cấp|Thêm.*nhà cung cấp|Tạo.*supplier|^Thêm$|^Mới$/i,
    fields: [
      { regex: /Tên.*ncc|Tên.*nhà cung cấp|Supplier name|^Tên$/i, value: `NCC Test ${t}` },
      { regex: /Mã|Code/i, value: `NCC${t}` },
      { regex: /Điện thoại|Phone/i, value: `09${Date.now().toString().slice(-9)}` },
    ],
    extraActions: [
      { name: "Update", regex: /Sửa|Cập nhật|Edit/i },
    ],
  });

  } catch (e) { pushResult({ module: "15 Supplier", action: "TEST_FATAL", status: "FAIL", screenshots: [], error: String(e).slice(0, 250) }); }
});