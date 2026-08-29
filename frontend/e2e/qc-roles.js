// Kiem tra tung buoc nghiep vu bang DUNG vai tro duoc giao (theo yeu cau de bai)
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");
const ctx = JSON.parse(fs.readFileSync(path.join(EVID, "_flowctx.json"), "utf8"));

const CASES = [
  { tag: "letan_reception", role: "Lễ tân", url: "/reception", label: "Buoc 1 - Le tan: man Tiep don" },
  { tag: "bacsi_encounter", role: "Bác sĩ", url: "/encounters/" + ctx.encounterId, label: "Buoc 2 - Bac si: man Kham benh" },
  { tag: "ktv_labresults", role: "Kỹ thuật viên", url: "/labrad/results", label: "Buoc 3 - KTV: man Ket qua CLS" },
  { tag: "bacsi_rx", role: "Bác sĩ", url: "/prescriptions", label: "Buoc 4 - Bac si: man Ke don" },
  { tag: "duocsi_dispense", role: "Dược sĩ", url: "/pharmacy/dispense", label: "Buoc 5 - Duoc si: man Phat thuoc" },
  { tag: "ketoan_cashier", role: "Kế toán", url: "/cashier", label: "Buoc 6 - Ke toan: man Thu ngan" },
];

(async () => {
  const out = [];
  for (const c of CASES) {
    const { browser, page, net, consoleErrs } = await newSession();
    const r = { ...c };
    try {
      await quickLogin(page, c.role);
      await page.goto(BASE + c.url, { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(3000);
      const t = await page.locator("main").innerText().catch(() => "");
      r.text = t.slice(0, 450);
      r.deniedInUi = /không có quyền|403|Forbidden|Không đủ quyền/i.test(t);
      r.api403 = net.filter((n) => n.status === 403).map((n) => n.url);
      r.api404 = net.filter((n) => n.status === 404).map((n) => n.url);
      await shot(page, "G_" + c.tag, [{ sel: "main", kind: "result", label: c.label + (r.api403.length ? " | " + r.api403.length + " API 403" : "") }]);
    } catch (e) { r.error = e.message.slice(0, 200); }
    r.consoleErrs = consoleErrs.slice(0, 6);
    out.push(r);
    console.log(`${c.tag}: 403=${(r.api403 || []).length} 404=${(r.api404 || []).length} ${r.error || ""}`);
    await browser.close();
  }
  saveJson("roles_matrix", out);
  console.log(JSON.stringify(out, null, 1).slice(0, 9000));
})();
