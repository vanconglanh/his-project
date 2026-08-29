// BUOC 4 - Ke don thuoc | BUOC 5 - Cap phat thuoc | BUOC 6 - Thu ngan
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

    // ===== BUOC 4: Ke don thuoc =====
    const P = (R.prescription = {});
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1800);
    await clickTab(page, "Đơn thuốc");
    await page.waitForTimeout(1800);
    await shot(page, "D1_01_rx_tab_empty", [
      { sel: "input[placeholder*='Tìm thuốc']", kind: "input", label: "O tim thuoc" },
      { sel: "main", kind: "result", label: "Tab Don thuoc (chua co don)" },
    ]);
    const ds = page.locator("input[placeholder*='Tìm thuốc']").first();
    P.hasSearch = (await ds.count()) > 0;
    if (P.hasSearch) {
      await ds.fill("metformin");
      await page.waitForTimeout(3000);
      P.searchArea = (await page.locator("main").innerText()).slice(-800);
      await shot(page, "D1_02_rx_search", [{ sel: "input[placeholder*='Tìm thuốc']", kind: "input", label: "Tim: metformin" }]);
      const item = page.locator("main [role=option],main [cmdk-item],main li,main button").filter({ hasText: /Metformin/i }).first();
      P.hasItem = (await item.count()) > 0;
      if (P.hasItem) {
        P.itemText = (await item.innerText()).replace(/\n/g, " | ").slice(0, 90);
        await item.click();
        await page.waitForTimeout(2000);
      }
      P.afterPick = (await page.locator("main").innerText()).slice(-900);
      await shot(page, "D1_03_rx_drug_added", [{ sel: "main", kind: "result", label: "Da them thuoc vao don" }]);
      // Tim nut luu don
      const saveRx = page.locator("main button").filter({ hasText: /Lưu đơn|Tạo đơn|Kê đơn|Lưu/ }).last();
      P.hasSaveBtn = (await saveRx.count()) > 0;
      if (P.hasSaveBtn) {
        P.saveBtnText = await saveRx.innerText();
        await saveRx.click();
        await page.waitForTimeout(4000);
        P.toast = await toasts(page);
        await shot(page, "D1_04_rx_after_save", [{ sel: "main", kind: "result", label: "Ket qua luu don: " + (P.toast[0] || "(khong toast)") }]);
        P.afterSave = (await page.locator("main").innerText()).slice(-800);
      } else {
        P.blocked = "Khong tim thay nut luu don thuoc trong tab Don thuoc";
        await shot(page, "D1_04_rx_no_save_btn", [{ sel: "main", kind: "result", label: "KHONG co nut luu don" }]);
      }
    } else P.blocked = "Khong co o tim thuoc";

    // ===== BUOC 5: Cap phat thuoc =====
    const D = (R.dispense = {});
    await page.goto(BASE + "/pharmacy/dispense", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(3000);
    D.pageText = (await page.locator("main").innerText()).slice(0, 1200);
    D.hasOurPatient = D.pageText.includes(ctx.patientName);
    await shot(page, "E1_01_dispense_queue", [{ sel: "main", kind: "result", label: "Hang cho phat thuoc" + (D.hasOurPatient ? " - CO BN cua ta" : " - KHONG co BN cua ta") }]);
    const dbtn = page.locator("main button").filter({ hasText: /Cấp phát|Phát thuốc|Chi tiết/ }).first();
    if (await dbtn.count()) {
      D.btnText = await dbtn.innerText();
      await dbtn.click();
      await page.waitForTimeout(2500);
      D.after = (await page.locator("body").innerText()).slice(0, 1000);
      await shot(page, "E1_02_dispense_detail", [{ sel: "body", kind: "result", label: "Man cap phat: " + D.btnText }]);
    } else D.blocked = "Khong co nut cap phat nao trong hang cho";

    // ===== BUOC 6: Thu ngan - thanh toan hoa don =====
    const C = (R.cashierPay = {});
    await page.goto(BASE + "/billings", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    const rows = page.locator("tbody tr");
    C.rowCount = await rows.count();
    C.patientColEmpty = C.rowCount > 0 && (await rows.first().locator("td").nth(1).innerText()).trim() === "";
    if (C.rowCount) {
      await rows.first().click();
      await page.waitForTimeout(3000);
      C.url = page.url();
      C.detailText = (await page.locator("main").innerText()).slice(0, 1200);
      await shot(page, "F1_01_invoice_detail", [{ sel: "main", kind: "result", label: "Chi tiet hoa don" }]);
      const btns = await page.locator("main button").allInnerTexts();
      C.buttons = btns.map((b) => b.trim().replace(/\s+/g, " ")).filter(Boolean).slice(0, 25);
      const payBtn = page.locator("main button").filter({ hasText: /Thu tiền|Thanh toán|Chốt hoá đơn|Xác nhận/ }).first();
      C.hasPayBtn = (await payBtn.count()) > 0;
      if (C.hasPayBtn) {
        C.payBtnText = await payBtn.innerText();
        await payBtn.click();
        await page.waitForTimeout(3000);
        C.dialogText = await page.locator("[role=dialog]").last().innerText().catch(() => "(khong co dialog)");
        await shot(page, "F1_02_pay_dialog", [{ sel: "[role=dialog]", kind: "action", label: "Dialog: " + C.payBtnText }]);
      }
    }
  } catch (e) {
    R.error = e.message.slice(0, 400);
    await shot(page, "DEF_error", []).catch(() => {});
  }
  R.api = net.filter((x) => x.status >= 400 || x.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step7_result", R);
  save();
  console.log(JSON.stringify(R, null, 1).slice(0, 8000));
  await browser.close();
})();
