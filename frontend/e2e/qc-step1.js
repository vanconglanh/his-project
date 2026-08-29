// BUOC 1 - Le tan: tao benh nhan moi qua UI that
const { shot, quickLogin, newSession, saveJson, BASE, EVID } = require("./qc-lib");
const fs = require("fs");
const path = require("path");

const STAMP = Date.now().toString().slice(-6);
const PATIENT = {
  full_name: "Nguyễn Thị Bích Trâm",
  phone: "09" + STAMP + "12".slice(0, 2),
  id_number: "0790" + STAMP + "99",
  dob: "1988-03-14",
  street: "Số 27 Ngõ 15 Đường Láng",
};

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  const result = { step: "1-reception-create-patient", patient: PATIENT, notes: [] };
  try {
    await quickLogin(page, "Lễ tân");
    await shot(page, "s1_01_after_login", []);

    await page.goto(BASE + "/patients/new", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(800);
    await shot(page, "s1_02_form_empty", [
      { sel: "#full_name", kind: "input", label: "Form tao benh nhan (trong)" },
      { sel: "btn:Tạo bệnh nhân", kind: "action", label: "Nut Tao benh nhan" },
    ]);

    // Dien form nhu nguoi dung that
    await page.fill("#full_name", PATIENT.full_name);
    await page.fill("#date_of_birth", PATIENT.dob);
    await page.fill("#phone", PATIENT.phone);
    await page.fill("#id_number", PATIENT.id_number);
    await page.fill("#street", PATIENT.street);

    // Combobox gioi tinh
    await page.click("#gender");
    await page.waitForTimeout(400);
    const optNu = page.getByRole("option", { name: "Nữ", exact: true });
    if (await optNu.count()) await optNu.first().click();
    else { result.notes.push("Khong tim thay option 'Nữ' trong combobox gioi tinh"); await page.keyboard.press("Escape"); }
    await page.waitForTimeout(400);

    await shot(page, "s1_03_form_filled", [
      { sel: "#full_name", kind: "input", label: PATIENT.full_name },
      { sel: "#phone", kind: "input", label: "SDT " + PATIENT.phone },
      { sel: "#gender", kind: "input", label: "Gioi tinh: Nu" },
    ]);

    const submit = page.getByRole("button", { name: "Tạo bệnh nhân", exact: true }).last();
    await submit.click();
    await page.waitForTimeout(3500);
    await page.waitForLoadState("networkidle").catch(() => {});

    result.urlAfterSubmit = page.url();
    result.toast = await page.locator("[data-sonner-toast], [role=status], [role=alert]").allInnerTexts().catch(() => []);
    await shot(page, "s1_04_after_submit", [
      { sel: "main", kind: "result", label: "Ket qua sau khi bam Tao benh nhan" },
    ]);

    // Xac minh lop 2: tim lai benh nhan trong danh sach
    await page.goto(BASE + "/patients", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    const search = page.locator("input[placeholder*='ìm'], input[type=search]").first();
    if (await search.count()) {
      await search.fill(PATIENT.full_name);
      await page.waitForTimeout(2500);
    }
    const bodyTxt = await page.locator("body").innerText();
    result.foundInList = bodyTxt.includes(PATIENT.full_name);
    await shot(page, "s1_05_verify_list", [
      { sel: "table", kind: "result", label: result.foundInList ? "Tim thay BN vua tao" : "KHONG thay BN" },
    ]);

    // Lay patient id tu link trong bang
    const link = page.locator("a[href^='/patients/']").filter({ hasText: PATIENT.full_name }).first();
    if (await link.count()) result.patientHref = await link.getAttribute("href");
    else {
      const rows = page.locator("tbody tr");
      if (await rows.count()) {
        await rows.first().click();
        await page.waitForTimeout(2000);
        result.patientHref = new URL(page.url()).pathname;
      }
    }
  } catch (e) {
    result.error = e.message;
    await shot(page, "s1_99_error", []).catch(() => {});
  }
  result.apiCalls = net.filter((n) => !n.url.includes("notification"));
  result.consoleErrs = consoleErrs;
  saveJson("s1_result", result);
  fs.writeFileSync(path.join(EVID, "_context.json"), JSON.stringify({ patient: PATIENT, patientHref: result.patientHref }, null, 1));
  console.log(JSON.stringify({ url: result.urlAfterSubmit, toast: result.toast, found: result.foundInList, href: result.patientHref, err: result.error, api: result.apiCalls.slice(-8) }, null, 1));
  await browser.close();
})();
