// BUOC 2c - Sinh hieu (man Dieu duong) -> Chan doan ICD-10 -> Chi dinh CLS
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = JSON.parse(fs.readFileSync(CTX_FILE, "utf8"));
const save = () => fs.writeFileSync(CTX_FILE, JSON.stringify(ctx, null, 1));
const R = {};
const toasts = (p) => p.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);

async function dlg(page) {
  return await page.evaluate(() => {
    const d = [...document.querySelectorAll("[role=dialog]")].pop();
    if (!d) return null;
    return {
      text: d.innerText.replace(/\s+\n/g, "\n").slice(0, 1500),
      controls: [...d.querySelectorAll("input,textarea,select,[role=combobox]")]
        .filter((e) => e.getClientRects().length)
        .map((e) => ({ id: e.id, ph: e.placeholder || "", type: e.type || "", label: e.id ? (document.querySelector(`label[for="${CSS.escape(e.id)}"]`)?.innerText.trim() || "") : "" })),
      buttons: [...d.querySelectorAll("button")].filter((b) => b.getClientRects().length).map((b) => b.innerText.trim().slice(0, 40)),
    };
  });
}
async function clickTab(page, name) {
  const t = page.getByRole("tab").filter({ hasText: name }).first();
  if (await t.count()) { await t.click(); return true; }
  const b = page.locator("main button").filter({ hasText: name }).first();
  if (await b.count()) { await b.click(); return true; }
  return false;
}

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  const go = async () => {
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);
  };
  try {
    await quickLogin(page, "Quản trị viên");

    // ---- Sinh hieu qua man Dieu duong ----
    const S1 = (R.vitalsNurse = {});
    await page.goto(BASE + "/nurse", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2000);
    const row = page.locator("tr,li,div").filter({ hasText: ctx.patientName }).filter({ has: page.getByRole("button", { name: /Nhập sinh hiệu/ }) }).last();
    S1.rowFound = (await row.count()) > 0;
    await shot(page, "A4_10_nurse_queue", [{ sel: "txt:" + ctx.patientName, kind: "result", label: "BN trong hang cho dieu duong" }]);
    const btn = S1.rowFound ? row.getByRole("button", { name: /Nhập sinh hiệu/ }).first()
                            : page.getByRole("button", { name: /Nhập sinh hiệu/ }).first();
    await btn.click();
    await page.waitForTimeout(2000);
    S1.dialog = await dlg(page);
    await shot(page, "A4_11_vitals_form_empty", [{ sel: "[role=dialog]", kind: "input", label: "Form nhap sinh hieu (trong)" }]);
    if (S1.dialog) {
      const want = { temperature: "37.2", pulse: "88", systolic: "145", diastolic: "92", weight: "78", height: "168", spo2: "97", respiratory: "18", glucose: "9.4" };
      S1.filled = [];
      for (const c of S1.dialog.controls) {
        const key = Object.keys(want).find((k) => (c.id + " " + c.label + " " + c.ph).toLowerCase().includes(k.slice(0, 5)));
        if (key) { await page.locator("[role=dialog] #" + CSS_ESC(c.id)).fill(want[key]).catch(() => {}); S1.filled.push(c.id + "=" + want[key]); }
      }
      await shot(page, "A4_12_vitals_form_filled", [{ sel: "[role=dialog]", kind: "input", label: "Da nhap: " + S1.filled.join(" ") }]);
      const sb = page.locator("[role=dialog] button").filter({ hasText: /Lưu|Ghi nhận/ }).last();
      S1.hasSave = (await sb.count()) > 0;
      if (S1.hasSave) { await sb.click(); await page.waitForTimeout(3000); }
      S1.toast = await toasts(page);
      await shot(page, "A4_13_vitals_after_save", [{ sel: "main", kind: "result", label: "Sau khi luu sinh hieu: " + (S1.toast[0] || "") }]);
    }

    // ---- Chan doan ----
    await go();
    const S2 = (R.diagnosis = {});
    S2.tabClicked = await clickTab(page, "Chẩn đoán");
    await page.waitForTimeout(1500);
    await shot(page, "A5_01_diag_empty", [
      { sel: "input[placeholder*='ICD-10 (VD']", kind: "input", label: "O tim ICD-10" },
      { sel: "btn:Lưu chẩn đoán", kind: "action", label: "Nut Luu chan doan" },
    ]);
    const icd = page.locator("input[placeholder*='ICD-10 (VD']").first();
    await icd.fill("E11");
    await page.waitForTimeout(3000);
    await shot(page, "A5_02_diag_search", [{ sel: "input[placeholder*='ICD-10 (VD']", kind: "input", label: "Go: E11" }]);
    const sug = page.locator("main [role=option], main [cmdk-item], main li").filter({ hasText: /E11/ }).first();
    S2.hasSuggest = (await sug.count()) > 0;
    if (S2.hasSuggest) { S2.suggestText = (await sug.innerText()).slice(0, 60); await sug.click(); await page.waitForTimeout(1000); }
    else { await page.fill("#diag-code-0", "E11"); await page.fill("#diag-name-0", "Đái tháo đường típ 2"); S2.fallbackManual = true; }
    await shot(page, "A5_03_diag_filled", [{ sel: "main", kind: "input", label: "Chan doan E11 da nhap" }]);
    await page.getByRole("button", { name: "Lưu chẩn đoán" }).first().click();
    await page.waitForTimeout(3500);
    S2.toast = await toasts(page);
    S2.listCount = (await page.locator("main").innerText()).match(/Danh sách chẩn đoán \((\d+)\)/)?.[1];
    await shot(page, "A5_04_diag_after_save", [{ sel: "main", kind: "result", label: "Danh sach chan doan = " + S2.listCount }]);

    // ---- Chi dinh CLS ----
    await go();
    const S3 = (R.clsOrder = {});
    S3.tabClicked = await clickTab(page, "Cận lâm sàng");
    await page.waitForTimeout(1500);
    await shot(page, "A6_01_cls_tab", [{ sel: "btn:Tạo đợt chỉ định mới", kind: "action", label: "Nut Tao dot chi dinh moi" }]);
    await page.getByRole("button", { name: "Tạo đợt chỉ định mới" }).first().click();
    await page.waitForTimeout(3000);
    S3.dialog = await dlg(page);
    await shot(page, "A6_02_cls_dialog", [{ sel: "[role=dialog]", kind: "input", label: "Dialog tao dot chi dinh CLS" }]);
  } catch (e) {
    R.error = e.message.slice(0, 400);
    await shot(page, "A3c_error", []).catch(() => {});
  }
  R.api = net.filter((n) => n.status >= 400 || n.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step3c_result", R);
  save();
  console.log(JSON.stringify(R, null, 1).slice(0, 6500));
  await browser.close();
})();

function CSS_ESC(s) { return s.replace(/([:.\[\]#,])/g, "\\$1"); }
