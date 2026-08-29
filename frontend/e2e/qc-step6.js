// BUOC 6 (nghiep vu chen truoc) - Chot dot CLS + Thu ngan thanh toan
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
async function dlgDump(page) {
  return await page.evaluate(() => {
    const d = [...document.querySelectorAll("[role=dialog]")].pop();
    if (!d) return null;
    return { text: d.innerText.replace(/\s+\n/g, "\n").slice(0, 1200),
      ctrls: [...d.querySelectorAll("input,textarea,select,[role=combobox]")].filter((e) => e.getClientRects().length)
        .map((e) => ({ id: e.id, ph: e.placeholder || "", type: e.type || "" })),
      btns: [...d.querySelectorAll("button")].filter((b) => b.getClientRects().length).map((b) => b.innerText.trim().slice(0, 40)) };
  });
}

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, "Quản trị viên");

    // ---- Chot dot CLS ----
    const C = (R.chotDot = {});
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1800);
    await clickTab(page, "Cận lâm sàng");
    await page.waitForTimeout(1500);
    const chot = page.getByRole("button", { name: "Chốt đợt" }).first();
    C.hasBtn = (await chot.count()) > 0;
    await shot(page, "C1_01_cls_round", [{ sel: "btn:Chốt đợt", kind: "action", label: "Nut Chot dot" }]);
    if (C.hasBtn) {
      await chot.click();
      await page.waitForTimeout(2500);
      C.dialog = await dlgDump(page);
      if (C.dialog) {
        await shot(page, "C1_02_chot_dialog", [{ sel: "[role=dialog]", kind: "action", label: "Xac nhan chot dot" }]);
        const ok = page.locator("[role=dialog] button").filter({ hasText: /Chốt|Xác nhận|Đồng ý/ }).last();
        if (await ok.count()) { await ok.click(); await page.waitForTimeout(3000); }
      }
      C.toast = await toasts(page);
      await page.waitForTimeout(1000);
      C.after = (await page.locator("main").innerText()).slice(-700);
      await shot(page, "C1_03_after_chot", [{ sel: "main", kind: "result", label: "Sau chot dot: " + (C.toast[0] || "") }]);
    }

    // ---- Thu ngan ----
    const T = (R.cashier = {});
    await page.goto(BASE + "/cashier", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    T.pageText = (await page.locator("main").innerText()).slice(0, 1200);
    T.hasOurPatient = T.pageText.includes(ctx.patientName);
    await shot(page, "C2_01_cashier_list", [{ sel: "main", kind: "result", label: "Man thu ngan" + (T.hasOurPatient ? " - co BN cua ta" : " - KHONG thay BN cua ta") }]);

    // ---- Hoa don ----
    const B = (R.billings = {});
    await page.goto(BASE + "/billings", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    B.pageText = (await page.locator("main").innerText()).slice(0, 1200);
    B.hasOurPatient = B.pageText.includes(ctx.patientName);
    await shot(page, "C2_02_billings_list", [{ sel: "main", kind: "result", label: "Danh sach hoa don" + (B.hasOurPatient ? " - co BN cua ta" : " - KHONG thay BN cua ta") }]);
  } catch (e) {
    R.error = e.message.slice(0, 400);
    await shot(page, "C_error", []).catch(() => {});
  }
  R.api = net.filter((x) => x.status >= 400 || x.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step6_result", R);
  save();
  console.log(JSON.stringify(R, null, 1).slice(0, 7000));
  await browser.close();
})();
