// BUOC 2b - Bac si: ghi sinh hieu -> chan doan ICD-10 -> chi dinh CLS (qua UI that)
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = JSON.parse(fs.readFileSync(CTX_FILE, "utf8"));
const save = () => fs.writeFileSync(CTX_FILE, JSON.stringify(ctx, null, 1));
const R = {};

async function dlg(page) {
  return await page.evaluate(() => {
    const d = document.querySelector("[role=dialog]");
    if (!d) return null;
    return {
      text: d.innerText.replace(/\s+\n/g, "\n").slice(0, 1200),
      controls: [...d.querySelectorAll("input,textarea,select,[role=combobox]")]
        .filter((e) => e.getClientRects().length)
        .map((e) => ({ tag: e.tagName.toLowerCase(), id: e.id, ph: e.placeholder || "", type: e.type || "" })),
      buttons: [...d.querySelectorAll("button")].filter((b) => b.getClientRects().length).map((b) => b.innerText.trim().slice(0, 40)),
    };
  });
}
const toasts = (p) => p.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, "Quản trị viên");
    const go = async () => {
      await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(1500);
    };
    await go();

    // ---- 2b.1 Sinh hieu ----
    const S1 = (R.vitals = {});
    await page.getByRole("button", { name: "Ghi sinh hiệu" }).first().click();
    await page.waitForTimeout(1500);
    S1.dialog = await dlg(page);
    await shot(page, "A4_01_vitals_dialog_empty", [{ sel: "[role=dialog]", kind: "input", label: "Dialog ghi sinh hieu (trong)" }]);
    if (S1.dialog) {
      const fills = { "Mạch": "88", "Nhiệt": "37.2", "Huyết áp tâm thu": "145", "Huyết áp tâm trương": "92",
        "Cân nặng": "78", "Chiều cao": "168", "SpO2": "97", "Nhịp thở": "18" };
      S1.filled = [];
      for (const [lbl, val] of Object.entries(fills)) {
        const el = page.locator("[role=dialog] input").filter({ has: page.locator("xpath=.") });
        const byLabel = page.locator(`[role=dialog] label:has-text("${lbl}")`).first();
        if (await byLabel.count()) {
          const forId = await byLabel.getAttribute("for");
          if (forId) { await page.fill("#" + forId.replace(/([:.\[\]])/g, "\\$1"), val).catch(() => {}); S1.filled.push(lbl); }
        }
      }
      await shot(page, "A4_02_vitals_filled", [{ sel: "[role=dialog]", kind: "input", label: "Da nhap sinh hieu: " + S1.filled.join(", ") }]);
      const saveBtn = page.locator("[role=dialog] button").filter({ hasText: /^(Lưu|Ghi nhận|Lưu sinh hiệu)/ }).last();
      if (await saveBtn.count()) { await saveBtn.click(); await page.waitForTimeout(3000); }
      else S1.noSaveBtn = true;
      S1.toast = await toasts(page);
      await shot(page, "A4_03_vitals_after_save", [{ sel: "main", kind: "result", label: "Sau khi luu sinh hieu" }]);
      S1.bodyHasVital = (await page.locator("main").innerText()).includes("145");
    }

    // ---- 2b.2 Chan doan ----
    await go();
    const S2 = (R.diagnosis = {});
    await page.getByRole("button", { name: "Chẩn đoán", exact: true }).first().click();
    await page.waitForTimeout(1200);
    const icd = page.locator("input[placeholder*='ICD-10 (VD']").first();
    await shot(page, "A5_01_diag_empty", [
      { sel: "input[placeholder*='ICD-10 (VD']", kind: "input", label: "O tim ICD-10" },
      { sel: "btn:Lưu chẩn đoán", kind: "action", label: "Nut Luu chan doan" },
    ]);
    await icd.fill("E11");
    await page.waitForTimeout(2500);
    S2.suggest = await page.locator("main").innerText().then((t) => t.includes("E11"));
    await shot(page, "A5_02_diag_search", [{ sel: "input[placeholder*='ICD-10 (VD']", kind: "input", label: "Tim: E11" }]);
    const sug = page.locator("main [role=option], main li, main button").filter({ hasText: /^E11/ }).first();
    if (await sug.count()) { S2.suggestText = (await sug.innerText()).slice(0, 80); await sug.click(); await page.waitForTimeout(800); }
    else {
      S2.noSuggest = true;
      await page.fill("#diag-code-0", "E11");
      await page.fill("#diag-name-0", "Đái tháo đường típ 2");
    }
    await shot(page, "A5_03_diag_filled", [{ sel: "main", kind: "input", label: "Da chon chan doan E11" }]);
    const saveDiag = page.getByRole("button", { name: "Lưu chẩn đoán" }).first();
    await saveDiag.click();
    await page.waitForTimeout(3000);
    S2.toast = await toasts(page);
    S2.listText = (await page.locator("main").innerText()).match(/Danh sách chẩn đoán \(\d+\)/)?.[0];
    await shot(page, "A5_04_diag_after_save", [{ sel: "main", kind: "result", label: "Ket qua: " + (S2.listText || "?") }]);

    // ---- 2b.3 Chi dinh CLS ----
    await go();
    const S3 = (R.clsOrder = {});
    await page.getByRole("button", { name: "Cận lâm sàng", exact: true }).first().click();
    await page.waitForTimeout(1200);
    await shot(page, "A6_01_cls_tab", [{ sel: "btn:Tạo đợt chỉ định mới", kind: "action", label: "Nut tao dot chi dinh" }]);
    await page.getByRole("button", { name: "Tạo đợt chỉ định mới" }).first().click();
    await page.waitForTimeout(2500);
    S3.dialog = await dlg(page);
    await shot(page, "A6_02_cls_dialog", [{ sel: "[role=dialog]", kind: "input", label: "Dialog tao dot chi dinh CLS" }]);
  } catch (e) {
    R.error = e.message;
    await shot(page, "A3b_error", []).catch(() => {});
  }
  R.api = net.filter((n) => n.status >= 400 || n.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step3b_result", R);
  save();
  console.log(JSON.stringify(R, null, 1).slice(0, 6000));
  await browser.close();
})();
