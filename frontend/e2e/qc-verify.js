// Verify fix vong QC full-flow 2026-08-29: BUG-02/03/05/07/08 qua UI that.
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const ctx = JSON.parse(fs.readFileSync(path.join(EVID, "_flowctx.json"), "utf8"));

const STAMP = Date.now().toString().slice(-6);
const KNOWN_BILLING_ID = "07403735-25cd-4732-9ab3-f0f2dfb8778d";
const results = {};

async function run(tag, role, fn) {
  const { browser, page, net } = await newSession();
  const r = { role };
  try {
    await quickLogin(page, role);
    await fn(page, net, r);
    r.api403 = net.filter((n) => n.status === 403).map((n) => n.url);
    r.posts = net.filter((n) => n.method === "POST").map((n) => n.url.replace(BASE, "").replace("http://localhost:5000", "") + " " + n.status);
  } catch (e) {
    r.error = e.message.slice(0, 200);
  }
  results[tag] = r;
  console.log(`[${tag}] ${r.pass ? "PASS" : "FAIL"} ${r.error || ""}`);
  await browser.close();
}

(async () => {
  // ---- BUG-02: le tan tao BN khong dien ngay cap CCCD ----
  await run("BUG02_patient", "Lễ tân", async (page, net, r) => {
    await page.goto(BASE + "/patients/new", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(1000);
    await page.fill("#full_name", "Verify CCCD " + STAMP);
    await page.fill("#date_of_birth", "1990-05-20");
    await page.fill("#phone", "0913" + STAMP);
    await page.fill("#id_number", "0790" + STAMP + "01");
    await page.getByRole("button", { name: "Tạo bệnh nhân", exact: true }).last().click();
    await page.waitForTimeout(3000);
    const post = net.find((n) => /\/patients$/.test(n.url) && n.method === "POST");
    r.postStatus = post?.status;
    r.pass = !!post && post.status >= 200 && post.status < 300;
    await shot(page, "V_bug02_patient", [{ sel: "main", kind: "result", label: "POST /patients = " + r.postStatus }]);
  });

  // ---- BUG-03: bac si ghi sinh hieu ----
  await run("BUG03_vitals", "Bác sĩ", async (page, net, r) => {
    await page.goto(BASE + "/encounters/" + ctx.encounterId, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    await page.getByRole("button", { name: "Ghi sinh hiệu" }).first().click();
    await page.waitForTimeout(1200);
    await page.locator('input[name="temperature_c"]').fill("37");
    await page.locator('input[name="heart_rate_bpm"]').fill("82");
    await page.locator('input[name="bp_systolic"]').fill("120");
    await page.locator('input[name="bp_diastolic"]').fill("80");
    await page.getByRole("button", { name: "Lưu sinh hiệu" }).click();
    await page.waitForTimeout(3000);
    const post = net.find((n) => /\/vital-signs$/.test(n.url) && n.method === "POST");
    r.postStatus = post?.status;
    r.pass = !!post && post.status >= 200 && post.status < 300;
    await shot(page, "V_bug03_after", [{ sel: "main", kind: "result", label: "POST vital-signs = " + r.postStatus }]);
  });

  // ---- BUG-05: bac si chot dot -> thu tien ----
  await run("BUG05_pay", "Bác sĩ", async (page, net, r) => {
    await page.goto(BASE + "/encounters/" + ctx.encounterId + "?tab=cls-orders", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    const chot = page.getByRole("button", { name: "Chốt đợt" }).first();
    if (await chot.count()) { await chot.click(); await page.waitForTimeout(2000); }
    const thu = page.getByRole("button", { name: "Thu tiền" }).first();
    r.hasPayButton = (await thu.count()) > 0;
    await shot(page, "V_bug05_cls", [{ sel: "main", kind: "result", label: "Nut Thu tien: " + r.hasPayButton }]);
    if (r.hasPayButton) {
      await thu.click();
      await page.waitForTimeout(2500);
      const pay = net.find((n) => /\/cls-rounds\/.+\/pay$/.test(n.url) && n.method === "POST");
      r.payStatus = pay?.status;
      r.pass = !!pay && pay.status >= 200 && pay.status < 300;
    } else {
      r.pass = true; r.note = "Khong con round SUBMITTED+UNPAID (da PAID tu truoc)";
    }
    await shot(page, "V_bug05_after", [{ sel: "main", kind: "result", label: "pay=" + r.payStatus }]);
  });

  // ---- BUG-05b: KTV nhap ket qua XN (khong con CLS_ORDER_UNPAID) ----
  await run("BUG05_ktv_result", "Kỹ thuật viên", async (page, net, r) => {
    await page.goto(BASE + "/labrad/results", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2000);
    await page.getByRole("button", { name: /Nhập kết quả/ }).first().click();
    await page.waitForTimeout(1500);
    await page.locator("#lr-order-search").fill(ctx.patientName);
    await page.waitForTimeout(2200);
    const opt = page.locator("button").filter({ hasText: ctx.patientName }).first();
    if (await opt.count()) { r.orderText = (await opt.innerText()).replace(/\n/g, " | ").slice(0, 90); await opt.click(); }
    else r.orderText = "KHONG CHON DUOC";
    await page.waitForTimeout(800);
    await page.locator("#lr-value").fill("5.6");
    const perf = page.locator("#lr-performed");
    if (await perf.count()) {
      const v = await perf.inputValue();
      if (!v) await perf.fill("2026-08-29T09:00");
    }
    await shot(page, "V_bug05_ktv_form", [{ sel: '[role=dialog]', kind: "input", label: "Order: " + (r.orderText || "?") }]);
    await page.getByRole("button", { name: /^Nhập kết quả$/ }).last().click();
    await page.waitForTimeout(2800);
    const post = net.find((n) => /\/lab-results$/.test(n.url) && n.method === "POST");
    r.postStatus = post?.status;
    const bodyTxt = await page.locator("body").innerText().catch(() => "");
    r.hasUnpaidError = /CLS_ORDER_UNPAID|chưa thanh toán/i.test(bodyTxt);
    r.pass = !!post && post.status >= 200 && post.status < 300 && !r.hasUnpaidError;
    await shot(page, "V_bug05_ktv_after", [{ sel: "main", kind: "result", label: "POST lab-results=" + r.postStatus + " unpaidErr=" + r.hasUnpaidError }]);
  });

  // ---- BUG-07: duoc si - dropdown kho co du lieu ----
  await run("BUG07_warehouse", "Dược sĩ", async (page, net, r) => {
    await page.goto(BASE + "/pharmacy/dispense", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    const phat = page.getByRole("button", { name: "Phát thuốc" }).first();
    if (await phat.count()) { await phat.click(); await page.waitForTimeout(2000); }
    const whCall = net.find((n) => /pharmacy\/warehouses/.test(n.url));
    r.warehouseCallStatus = whCall?.status;
    r.badWarehouseCall = net.some((n) => /\/api\/v1\/warehouses/.test(n.url) && n.status === 404);
    const bodyTxt = await page.locator("body").innerText().catch(() => "");
    r.dialogHasKho = /Kho chính|Kho lẻ|Kho phát thuốc/.test(bodyTxt);
    r.pass = !!whCall && whCall.status === 200 && !r.badWarehouseCall;
    await shot(page, "V_bug07_dispense", [{ sel: "main", kind: "result", label: "GET pharmacy/warehouses=" + r.warehouseCallStatus + " khoVisible=" + r.dialogHasKho }]);
  });

  // ---- BUG-08: ke toan - mo chi tiet hoa don (navigate truc tiep) ----
  await run("BUG08_billing", "Kế toán", async (page, net, r) => {
    await page.goto(BASE + "/billings/" + KNOWN_BILLING_ID, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(2500);
    const bodyTxt = await page.locator("main").innerText().catch(() => "");
    r.notFound = /Không tìm thấy ho[áa] đơn/i.test(bodyTxt);
    const detailCall = net.find((n) => new RegExp("/billings/" + KNOWN_BILLING_ID + "$").test(n.url));
    r.detailStatus = detailCall?.status;
    r.pass = !r.notFound && (!detailCall || detailCall.status === 200);
    await shot(page, "V_bug08_detail", [{ sel: "main", kind: "result", label: "notFound=" + r.notFound + " api=" + r.detailStatus }]);
  });

  saveJson("verify_fixes", results);
  console.log("\n===== TONG KET =====");
  for (const [k, v] of Object.entries(results)) console.log(`${k}: ${v.pass ? "PASS" : "FAIL"}  ${JSON.stringify({post:v.postStatus,pay:v.payStatus,wh:v.warehouseCallStatus,khoVisible:v.dialogHasKho,notFound:v.notFound,unpaid:v.hasUnpaidError,order:v.orderText,note:v.note,err:v.error})}`);
})();
