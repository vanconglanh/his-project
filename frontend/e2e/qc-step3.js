// BUOC 2 (nghiep vu) - Bac si kham: bat dau kham, sinh hieu, chan doan, chi dinh CLS
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = JSON.parse(fs.readFileSync(CTX_FILE, "utf8"));
const save = () => fs.writeFileSync(CTX_FILE, JSON.stringify(ctx, null, 1));

async function dumpTab(page) {
  return await page.evaluate(() => {
    const o = { controls: [], buttons: [], text: "" };
    document.querySelectorAll("main input,main select,main textarea,main [role=combobox]").forEach((el) => {
      if (!el.getClientRects().length) return;
      let l = "";
      if (el.id) l = document.querySelector(`label[for="${CSS.escape(el.id)}"]`)?.innerText.trim() || "";
      o.controls.push({ tag: el.tagName.toLowerCase(), id: el.id, name: el.name || "", ph: el.placeholder || "", label: l });
    });
    document.querySelectorAll("main button").forEach((b) => {
      const t = (b.innerText || "").trim().replace(/\s+/g, " ");
      if (t && b.getClientRects().length) o.buttons.push(t.slice(0, 50));
    });
    o.text = document.querySelector("main")?.innerText.replace(/\s+\n/g, "\n").slice(0, 1500) || "";
    return o;
  });
}

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  const R = { tabs: {} };
  const ROLE = process.env.QC_ROLE || "Quản trị viên";
  try {
    await quickLogin(page, ROLE);
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);
    await shot(page, "A3_01_encounter_detail", [{ sel: "btn:Bắt đầu khám", kind: "action", label: "Nut Bat dau kham" }]);

    const start = page.getByRole("button", { name: "Bắt đầu khám" }).first();
    if (await start.count()) {
      await start.click();
      await page.waitForTimeout(2500);
      R.afterStartToast = await page.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);
      await shot(page, "A3_02_after_start", [{ sel: "main", kind: "result", label: "Sau khi bam Bat dau kham" }]);
    } else R.startMissing = true;

    for (const tab of ["Bệnh án", "Cận lâm sàng", "Chẩn đoán", "Đơn thuốc", "Kết quả CLS"]) {
      const t = page.getByRole("tab", { name: tab }).first();
      const t2 = (await t.count()) ? t : page.getByRole("button", { name: tab }).first();
      if (!(await t2.count())) { R.tabs[tab] = { missing: true }; continue; }
      await t2.click();
      await page.waitForTimeout(2000);
      R.tabs[tab] = await dumpTab(page);
      await shot(page, "A3_tab_" + tab.replace(/\s/g, "_"), [{ sel: "main", kind: "result", label: "Tab " + tab }]);
    }
  } catch (e) {
    R.error = e.message;
    await shot(page, "A3_error", []).catch(() => {});
  }
  R.api = net.filter((n) => n.status >= 400 || n.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step3_explore", R);
  console.log(JSON.stringify(R, null, 1).slice(0, 7000));
  await browser.close();
})();
