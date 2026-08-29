// BUOC 3 - Nhap ket qua XN cho dung benh nhan/chi dinh vua tao (qua UI that)
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const ctx = JSON.parse(fs.readFileSync(path.join(EVID, "_flowctx.json"), "utf8"));
const R = {};
const toasts = (p) => p.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);
const ROLE = process.env.QC_ROLE || "Quản trị viên";
const TAG = process.env.QC_TAG || "admin";

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, ROLE);
    await page.goto(BASE + "/labrad/results", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    R.listVisible = !(await page.locator("main").innerText()).includes("403");
    await shot(page, `B2_${TAG}_01_list`, [{ sel: "btn:+ Nhập kết quả", kind: "action", label: "Nut + Nhap ket qua (" + ROLE + ")" }]);

    const addBtn = page.getByRole("button", { name: /Nhập kết quả/ }).first();
    R.hasAddBtn = (await addBtn.count()) > 0;
    if (!R.hasAddBtn) { R.blocked = "Vai tro " + ROLE + " khong thay nut Nhap ket qua"; throw new Error(R.blocked); }
    await addBtn.click();
    await page.waitForTimeout(2500);
    const d = page.locator("[role=dialog]").last();

    await shot(page, `B2_${TAG}_02_form_empty`, [
      { sel: "#lr-order-search", kind: "input", label: "Bo chon chi dinh XN" },
      { sel: "#lr-value", kind: "input", label: "O gia tri ket qua" },
      { sel: "btn:Nhập kết quả", kind: "action", label: "Nut submit" },
    ]);

    // Chon dung chi dinh cua benh nhan vua tao
    await d.locator("#lr-order-search").fill(ctx.patientName);
    await page.waitForTimeout(2500);
    const opt = d.locator("button").filter({ hasText: ctx.patientName }).first();
    R.orderFound = (await opt.count()) > 0;
    if (!R.orderFound) { R.blocked = "Khong tim thay chi dinh XN cua BN " + ctx.patientName; }
    else {
      R.orderText = (await opt.innerText()).replace(/\n/g, " | ").slice(0, 120);
      await opt.click();
      await page.waitForTimeout(800);
    }
    await shot(page, `B2_${TAG}_03_order_picked`, [{ sel: "#lr-order-search", kind: "input", label: "Da chon: " + (R.orderText || "KHONG CHON DUOC") }]);

    await d.locator("#lr-value").fill("9.4");
    await d.locator("#lr-value-num").fill("9.4");
    await d.locator("#lr-unit").fill("mmol/L");
    await d.locator("#lr-method").fill("Enzymatic (hexokinase)");
    await d.locator("#lr-performed").fill("2026-08-29T10:15");
    await d.locator("#lr-note").fill("Duong huyet doi tang cao - phu hop chan doan DTD tip 2");
    await shot(page, `B2_${TAG}_04_form_filled`, [
      { sel: "#lr-value", kind: "input", label: "Gia tri 9.4 mmol/L" },
      { sel: "btn:Nhập kết quả", kind: "action", label: "Bam Nhap ket qua" },
    ]);

    await d.locator("button[type=submit]").click();
    await page.waitForTimeout(4000);
    R.toast = await toasts(page);
    R.dialogStillOpen = (await page.locator("[role=dialog]").count()) > 0;
    await page.waitForTimeout(1500);
    const t = await page.locator("main").innerText();
    R.foundInList = t.includes(ctx.patientName) && t.includes("9.4");
    await shot(page, `B2_${TAG}_05_after_submit`, [{ sel: "main", kind: "result", label: "Ket qua: " + (R.toast[0] || "(khong toast)") }]);
    R.listExcerpt = t.slice(0, 600);
  } catch (e) {
    R.error = e.message.slice(0, 300);
    await shot(page, `B2_${TAG}_99_error`, []).catch(() => {});
  }
  R.api = net.filter((x) => x.status >= 400 || x.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("step5_" + TAG, R);
  console.log(JSON.stringify(R, null, 1).slice(0, 5000));
  await browser.close();
})();
