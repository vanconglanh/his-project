// Dieu tra: trang chi tiet hoa don bao "Khong tim thay hoa don" du DB co row
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const ID = process.argv[2] || "2ffe93bd-ae07-42c2-ae29-21e1adea03c9";

(async () => {
  const { browser, page, consoleErrs } = await newSession();
  const calls = [];
  page.on("response", async (r) => {
    if (!r.url().includes("/api/")) return;
    let body = "";
    try { body = (await r.text()).slice(0, 500); } catch {}
    calls.push({ s: r.status(), m: r.request().method(), u: r.url().replace("http://localhost:5000", ""), body });
  });
  const R = {};
  try {
    await quickLogin(page, "Quản trị viên");
    calls.length = 0;
    await page.goto(BASE + "/billings/" + ID, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(3000);
    R.pageText = (await page.locator("main").innerText()).slice(0, 400);
    R.calls = calls.filter((c) => /billing/i.test(c.u));
    await shot(page, "F1_03_invoice_detail_404", [{ sel: "main", kind: "result", label: "Chi tiet HD " + ID.slice(0, 8) }]);
    // So sanh: API list tra ve gi
    R.listCall = calls.find((c) => /billings\?|billings$/.test(c.u));
  } catch (e) { R.error = e.message.slice(0, 300); }
  R.consoleErrs = consoleErrs.slice(0, 10);
  saveJson("step8_billing_detail", R);
  console.log(JSON.stringify(R, null, 1).slice(0, 6000));
  await browser.close();
})();
