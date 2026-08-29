// BUOC 2e - xac minh gia thuyet validate sinh hieu; BUOC 3 - nhap ket qua XN (/labrad/results)
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = JSON.parse(fs.readFileSync(CTX_FILE, "utf8"));
const R = {};
const toasts = (p) => p.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, "Quản trị viên");

    // ===== 2e: dien DAY DU moi o so cua form sinh hieu =====
    if (process.env.SKIP_VITALS !== "1") {
      const V = (R.vitalsAllFilled = {});
      await page.goto(BASE + "/nurse", { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(2500);
      await page.getByRole("button", { name: /Nhập sinh hiệu/ }).first().click();
      await page.waitForTimeout(2000);
      const d = page.locator("[role=dialog]").last();
      const nums = d.locator("input[type=number]");
      const n = await nums.count();
      const vals = ["37.2", "88", "145", "92", "97", "18", "78", "168", "2", "168"];
      for (let i = 0; i < n; i++) await nums.nth(i).fill(vals[i] || "1");
      V.numInputs = n;
      await shot(page, "A4_30_vitals_all_filled", [{ sel: "[role=dialog]", kind: "input", label: "Dien DU " + n + " o so" }]);
      await d.getByRole("button", { name: "Lưu sinh hiệu" }).click();
      await page.waitForTimeout(3500);
      V.toast = await toasts(page);
      V.dialogStillOpen = (await page.locator("[role=dialog]").count()) > 0;
      await shot(page, "A4_31_vitals_all_saved", [{ sel: "main", kind: "result", label: "Ket qua: " + (V.toast[0] || "(khong toast)") }]);
      V.postCalls = net.filter((x) => x.method === "POST" && /vital/i.test(x.url));
    }

    // ===== BUOC 3: nhap ket qua xet nghiem =====
    const L = (R.labResults = {});
    await page.goto(BASE + "/labrad/results", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    L.pageText = (await page.locator("main").innerText()).slice(0, 900);
    await shot(page, "B1_01_labrad_results_list", [
      { sel: "btn:+ Nhập kết quả", kind: "action", label: "Nut + Nhap ket qua" },
      { sel: "main", kind: "result", label: "Danh sach ket qua CLS" },
    ]);
    const addBtn = page.getByRole("button", { name: /Nhập kết quả/ }).first();
    L.hasAddBtn = (await addBtn.count()) > 0;
    if (!L.hasAddBtn) { L.blocked = "Khong tim thay nut Nhap ket qua"; }
    else {
      await addBtn.click();
      await page.waitForTimeout(2500);
      const d2 = page.locator("[role=dialog]").last();
      L.dialogOpen = (await d2.count()) > 0;
      if (L.dialogOpen) {
        L.dialogText = (await d2.innerText()).slice(0, 1500);
        L.controls = await page.evaluate(() => {
          const d = [...document.querySelectorAll("[role=dialog]")].pop();
          return [...d.querySelectorAll("input,textarea,select,[role=combobox],button")]
            .filter((e) => e.getClientRects().length)
            .map((e) => ({ tag: e.tagName.toLowerCase(), id: e.id, ph: e.placeholder || "", type: e.type || "", txt: (e.innerText || "").trim().slice(0, 40) }));
        });
      }
      await shot(page, "B1_02_labrad_form_open", [{ sel: "[role=dialog]", kind: "input", label: "Form nhap ket qua XN" }]);
    }
  } catch (e) {
    R.error = e.message.slice(0, 400);
    await shot(page, "B1_error", []).catch(() => {});
  }
  R.api = net.filter((x) => x.status >= 400 || x.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step4_result", R);
  console.log(JSON.stringify(R, null, 1).slice(0, 7000));
  await browser.close();
})();
