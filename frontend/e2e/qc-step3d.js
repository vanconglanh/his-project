// BUOC 2d - Kiem tra hang cho dieu duong + hoan tat chi dinh CLS
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = JSON.parse(fs.readFileSync(CTX_FILE, "utf8"));
const save = () => fs.writeFileSync(CTX_FILE, JSON.stringify(ctx, null, 1));
const R = {};
const toasts = (p) => p.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);
async function clickTab(page, name) {
  const t = page.getByRole("tab").filter({ hasText: name }).first();
  if (await t.count()) { await t.click(); return true; }
  const b = page.locator("main button").filter({ hasText: name }).first();
  if (await b.count()) { await b.click(); return true; }
  return false;
}

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, "Quản trị viên");

    // ---- Hang cho dieu duong: BN dang kham co xuat hien khong? ----
    const N = (R.nurseQueue = {});
    await page.goto(BASE + "/nurse", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    const t = await page.locator("main").innerText();
    N.containsOurPatient = t.includes(ctx.patientName);
    N.queueExcerpt = t.slice(0, 900);

    // ---- Nhap sinh hieu cho DUNG benh nhan (neu co) ----
    const V = (R.vitals = {});
    if (N.containsOurPatient) {
      const card = page.locator("li,tr,div").filter({ hasText: ctx.patientName })
        .filter({ has: page.getByRole("button", { name: /Nhập sinh hiệu/ }) }).last();
      await card.getByRole("button", { name: /Nhập sinh hiệu/ }).first().click();
    } else {
      V.note = "BN dang kham KHONG co trong hang cho dieu duong -> khong the nhap sinh hieu dung BN";
      await page.getByRole("button", { name: /Nhập sinh hiệu/ }).first().click();
    }
    await page.waitForTimeout(2000);
    const d = page.locator("[role=dialog]").last();
    V.dialogTitle = (await d.innerText()).split("\n").slice(0, 2).join(" ");
    await shot(page, "A4_20_vitals_form_empty", [{ sel: "[role=dialog]", kind: "input", label: "Form sinh hieu: " + V.dialogTitle }]);
    const fills = [["36.5", "37.2"], ["80", "88"], ["120", "145"], ["80", "92"], ["98", "97"], ["16", "18"], ["60", "78"], ["165", "168"], ["100", "168"]];
    V.filled = [];
    for (const [ph, val] of fills) {
      const inp = d.locator(`input[placeholder="${ph}"]`).first();
      if (await inp.count()) { await inp.fill(val); V.filled.push(ph + "->" + val); }
    }
    await shot(page, "A4_21_vitals_form_filled", [{ sel: "[role=dialog]", kind: "input", label: "HA 145/92, mach 88, nhiet 37.2" }]);
    await d.getByRole("button", { name: "Lưu sinh hiệu" }).click();
    await page.waitForTimeout(3500);
    V.toast = await toasts(page);
    await shot(page, "A4_22_vitals_after_save", [{ sel: "main", kind: "result", label: "Ket qua luu: " + (V.toast[0] || "(khong co toast)") }]);

    // ---- Chi dinh CLS ----
    const C = (R.cls = {});
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);
    await clickTab(page, "Cận lâm sàng");
    await page.waitForTimeout(1200);
    await page.getByRole("button", { name: "Tạo đợt chỉ định mới" }).first().click();
    await page.waitForTimeout(2500);
    const dd = page.locator("[role=dialog]").last();
    await shot(page, "A6_10_cls_dialog_empty", [
      { sel: "#cls-search", kind: "input", label: "O tim dich vu CLS" },
      { sel: "btn:Lưu đợt chỉ định", kind: "action", label: "Nut luu dot chi dinh" },
    ]);
    await dd.locator("#cls-search").fill("glucose");
    await page.waitForTimeout(3000);
    C.afterSearch = (await dd.innerText()).slice(0, 700);
    await shot(page, "A6_11_cls_search", [{ sel: "#cls-search", kind: "input", label: "Tim: glucose" }]);
    // Neu khong ra ket qua, thu tu khoa khac
    if (/Không tìm thấy|Nhập từ khoá/.test(C.afterSearch)) {
      await dd.locator("#cls-search").fill("máu");
      await page.waitForTimeout(3000);
      C.afterSearch2 = (await dd.innerText()).slice(0, 700);
      await shot(page, "A6_12_cls_search2", [{ sel: "#cls-search", kind: "input", label: "Tim: mau" }]);
    }
    // Click ket qua dau tien trong danh sach goi y
    const item = dd.locator("[role=option],[cmdk-item],li,button").filter({ hasText: /₫|Glucose|máu/i }).first();
    C.hasItem = (await item.count()) > 0;
    if (C.hasItem) {
      C.itemText = (await item.innerText()).slice(0, 80).replace(/\n/g, " | ");
      await item.click();
      await page.waitForTimeout(1200);
    }
    C.beforeSave = (await dd.innerText()).slice(0, 700);
    await shot(page, "A6_13_cls_selected", [{ sel: "[role=dialog]", kind: "input", label: "Dich vu da chon" }]);
    await dd.getByRole("button", { name: "Lưu đợt chỉ định" }).click();
    await page.waitForTimeout(4000);
    C.toast = await toasts(page);
    await page.waitForTimeout(500);
    C.afterSave = (await page.locator("main").innerText()).slice(0, 900);
    await shot(page, "A6_14_cls_after_save", [{ sel: "main", kind: "result", label: "Ket qua tao dot CLS: " + (C.toast[0] || "") }]);
  } catch (e) {
    R.error = e.message.slice(0, 400);
    await shot(page, "A3d_error", []).catch(() => {});
  }
  R.api = net.filter((n) => n.status >= 400 || n.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step3d_result", R);
  save();
  console.log(JSON.stringify(R, null, 1).slice(0, 6500));
  await browser.close();
})();
