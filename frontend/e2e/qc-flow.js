// FULL FLOW qua UI that - chay bang tai khoan Quan tri vien de tach bach loi UI
// khoi loi phan quyen RBAC (da chung minh o qc-step1.js).
// Chay: node e2e/qc-flow.js <fromStep>
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");

const CTX_FILE = path.join(EVID, "_flowctx.json");
const ctx = fs.existsSync(CTX_FILE) ? JSON.parse(fs.readFileSync(CTX_FILE, "utf8")) : {};
const save = () => fs.writeFileSync(CTX_FILE, JSON.stringify(ctx, null, 1));

const S = Date.now().toString().slice(-6);
const R = { steps: {} };

async function txt(page) { return await page.locator("body").innerText(); }
async function toasts(page) {
  return await page.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);
}
// Chon 1 combobox base-ui theo id, chon option chua text
async function pickCombo(page, id, optText) {
  await page.click("#" + id);
  await page.waitForTimeout(500);
  const opt = page.getByRole("option").filter({ hasText: optText }).first();
  if (await opt.count()) { await opt.click(); await page.waitForTimeout(300); return true; }
  await page.keyboard.press("Escape");
  return false;
}

(async () => {
  const only = process.argv[2];
  const { browser, page, net, consoleErrs } = await newSession();
  const log = (m) => console.log("  " + m);

  try {
    await quickLogin(page, "Quản trị viên");

    // ---------- A1: Tao benh nhan (Le tan flow, chay bang admin) ----------
    if (!only || only === "1") {
      const st = (R.steps.A1 = { name: "Tiep don - tao benh nhan moi", ok: false });
      ctx.patientName = "Trần Quốc Hưng " + S;
      ctx.patientPhone = "0912" + S;
      await page.goto(BASE + "/patients/new", { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(700);
      await shot(page, "A1_01_form_empty", [
        { sel: "#full_name", kind: "input", label: "Form trong" },
        { sel: "btn:Tạo bệnh nhân", kind: "action", label: "Nut submit" },
      ]);
      await page.fill("#full_name", ctx.patientName);
      await page.fill("#date_of_birth", "1975-06-20");
      await page.fill("#phone", ctx.patientPhone);
      await page.fill("#street", "45 Trần Duy Hưng, Cầu Giấy");
      await pickCombo(page, "gender", "Nam");
      // Workaround BUG-02: bo trong o "Ngay cap CMND/CCCD" (khong bat buoc) lam
      // FE gui chuoi rong -> BE 400. Dien du de di tiep cac buoc sau.
      if (process.env.QC_FILL_IDDATE === "1") {
        await page.fill("#id_number", "0010" + S + "77");
        await page.fill("#id_card_issued_date", "2021-08-10");
      }
      await shot(page, "A1_02_form_filled", [
        { sel: "#full_name", kind: "input", label: ctx.patientName },
        { sel: "#phone", kind: "input", label: ctx.patientPhone },
        { sel: "btn:Tạo bệnh nhân", kind: "action", label: "Bam Tao benh nhan" },
      ]);
      await page.getByRole("button", { name: "Tạo bệnh nhân", exact: true }).last().click();
      await page.waitForTimeout(4000);
      st.toast = await toasts(page);
      st.url = page.url();
      const m = st.url.match(/\/patients\/([0-9a-f-]{10,})/i);
      if (m) { ctx.patientId = m[1]; st.ok = true; }
      st.patientId = ctx.patientId;
      await shot(page, "A1_03_after_submit", [{ sel: "main", kind: "result", label: st.ok ? "Tao thanh cong -> " + ctx.patientId : "Chua vao duoc trang chi tiet" }]);
      log("A1 ok=" + st.ok + " id=" + ctx.patientId + " toast=" + JSON.stringify(st.toast));
      save();
    }

    // ---------- A2: Tao luot kham ----------
    if (!only || only === "2") {
      const st = (R.steps.A2 = { name: "Tao luot kham (encounter)", ok: false });
      await page.goto(BASE + "/encounters/new", { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(800);
      await shot(page, "A2_01_form_empty", [
        { sel: "input[placeholder*='SĐT']", kind: "input", label: "O tim benh nhan" },
        { sel: "btn:Tạo lượt khám", kind: "action", label: "Nut tao luot kham" },
      ]);
      const sb = page.locator("input[placeholder*='SĐT']").first();
      await sb.click();
      await sb.fill(ctx.patientName.split(" ").slice(-1)[0]);
      await page.waitForTimeout(2500);
      await shot(page, "A2_02_patient_search", [{ sel: "input[placeholder*='SĐT']", kind: "input", label: "Tim: " + ctx.patientName }]);
      // NOTE a11y: goi y benh nhan KHONG co role=option/listbox -> phai click theo text
      const opt = page.getByText(ctx.patientName, { exact: false }).last();
      if (await opt.count()) { await opt.click(); st.pickedExact = true; }
      else st.pickErr = "Khong co goi y benh nhan nao";
      await page.waitForTimeout(600);
      const reason = page.locator("textarea, input").filter({ hasNot: sb });
      const rt = page.locator("[placeholder*='Đái tháo đường tái khám']").first();
      if (await rt.count()) await rt.fill("Khát nhiều, tiểu nhiều 2 tuần - nghi ĐTĐ típ 2");
      const sym = page.locator("[placeholder*='triệu chứng bệnh nhân tự khai']").first();
      if (await sym.count()) await sym.fill("Mệt mỏi, sụt 3kg trong 1 tháng");
      await shot(page, "A2_03_filled", [
        { sel: "[placeholder*='Đái tháo đường tái khám']", kind: "input", label: "Ly do kham" },
        { sel: "btn:Tạo lượt khám", kind: "action", label: "Bam tao" },
      ]);
      await page.getByRole("button", { name: "Tạo lượt khám", exact: true }).last().click();
      await page.waitForTimeout(4500);
      st.toast = await toasts(page);
      st.url = page.url();
      const m = st.url.match(/\/encounters\/([0-9a-f-]{10,})/i);
      if (m) { ctx.encounterId = m[1]; st.ok = true; }
      await shot(page, "A2_04_after_submit", [{ sel: "main", kind: "result", label: st.ok ? "Luot kham " + ctx.encounterId : "Khong tao duoc" }]);
      log("A2 ok=" + st.ok + " enc=" + ctx.encounterId + " toast=" + JSON.stringify(st.toast) + " " + JSON.stringify(st.pickErr || st.picked || ""));
      save();
    }
  } catch (e) {
    R.error = e.message + "\n" + (e.stack || "").split("\n").slice(0, 3).join("\n");
    await shot(page, "AX_error", []).catch(() => {});
    console.log("ERROR: " + R.error);
  }
  R.api = net.filter((n) => n.status >= 400 || n.method !== "GET");
  R.consoleErrs = consoleErrs.slice(0, 20);
  saveJson("flow_result_" + (only || "all"), R);
  console.log(JSON.stringify(R.api.slice(-14), null, 1));
  await browser.close();
})();
