const fs = require("fs");
const lib = require("D:/_Project/08.ATDS/02.Onetech/202501_CV10/_project/git/screen/_git/atds/his-project/frontend/e2e/qc-lib.js");
const EVID = "D:/_Project/08.ATDS/02.Onetech/202501_CV10/_project/git/screen/_git/atds/his-project/docs/qc/evidence-inbody-ocr-20260830";
const FULL = EVID + "/sample-inbody-full.pdf", PART = EVID + "/sample-inbody-partial.pdf", FAKE = "D:/tmp/inbodyqc/fake.pdf";
const PATIENT = "f0000000-0000-0000-0000-000000000008";
const log = []; const L = s => { console.log(s); log.push(s); };

async function openDialogAndUpload(page, file, shotPrefix) {
  await page.getByRole("button", { name: /Nhập kết quả InBody/ }).first().click();
  await page.waitForTimeout(1200);
  await page.locator('[role=dialog] input[type=file]').setInputFiles(file);
  await page.waitForTimeout(600);
  await page.screenshot({ path: EVID + `/${shotPrefix}-file-chosen.png` });
  await page.getByRole("button", { name: /Tải lên & đọc/ }).click();
  await page.waitForTimeout(4500);
  await page.screenshot({ path: EVID + `/${shotPrefix}-result.png`, fullPage: true });
}

(async () => {
  const { browser, page } = await lib.newSession();
  const errs = [];
  page.on("pageerror", e => errs.push("PAGEERROR: " + e.message));
  await lib.quickLogin(page, "Bác sĩ");
  await page.goto(lib.BASE + "/patients/" + PATIENT, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(3000);
  await page.getByRole("button", { name: /Lịch sử InBody/ }).first().click();
  await page.waitForTimeout(2500);
  const before = await page.locator("text=/Chờ xác nhận|Đã xác nhận/").count();
  L("So ban ghi lich su TRUOC = " + before);

  // === STEP 4/5: upload full
  await openDialogAndUpload(page, FULL, "br-10-full");
  const dlg = page.locator("[role=dialog]");
  const rowsTxt = await dlg.innerText();
  L("--- BANG XAC NHAN (full) ---\n" + rowsTxt);
  L("So field 'Doc duoc' = " + await dlg.locator("text=Đọc được").count() + " ; 'Chua doc duoc' = " + await dlg.locator("text=Chưa đọc được").count());
  const ticked = await dlg.locator('[role=checkbox][data-state=checked], [role=checkbox][aria-checked="true"]').count();
  L("So checkbox 'Dung' tich san = " + ticked);
  // sua tay
  await dlg.getByLabel("Giá trị Cân nặng").fill("71.3");
  await page.screenshot({ path: EVID + "/br-11-edited-weight.png", fullPage: true });
  await dlg.getByRole("button", { name: /Xác nhận & Lưu/ }).click();
  await page.waitForTimeout(3500);
  await page.screenshot({ path: EVID + "/br-12-after-confirm.png", fullPage: true });
  L("Toast sau confirm (patient dialog, KHONG co encounter) = " + JSON.stringify(await page.locator("[data-sonner-toast]").allTextContents()));
  L("Canh bao encounter hien thi = " + await page.locator("text=Chưa chọn lượt khám").count());
  await page.keyboard.press("Escape"); await page.waitForTimeout(1000);

  // === STEP 6: partial
  await openDialogAndUpload(page, PART, "br-13-partial");
  const d2 = page.locator("[role=dialog]");
  L("--- BANG XAC NHAN (partial) ---\n" + await d2.innerText());
  L("partial: 'Doc duoc'=" + await d2.locator("text=Đọc được").count() + " 'Chua doc duoc'=" + await d2.locator("text=Chưa đọc được").count());
  const btnSave = d2.getByRole("button", { name: /Xác nhận & Lưu/ });
  L("partial: nut Luu enabled = " + await btnSave.isEnabled());
  await page.keyboard.press("Escape"); await page.waitForTimeout(1000);

  // === STEP 9: file rac
  await openDialogAndUpload(page, FAKE, "br-14-invalid");
  L("Toast loi file rac = " + JSON.stringify(await page.locator("[data-sonner-toast]").allTextContents()));
  L("Dialog con hien thi (khong crash) = " + await page.locator("[role=dialog]").isVisible());
  L("PAGEERROR: " + JSON.stringify(errs));
  await page.keyboard.press("Escape"); await page.waitForTimeout(1500);
  await page.reload({ waitUntil: "domcontentloaded" }); await page.waitForTimeout(3000);
  await page.getByRole("button", { name: /Lịch sử InBody/ }).first().click();
  await page.waitForTimeout(2500);
  L("So ban ghi lich su SAU = " + await page.locator("text=/Chờ xác nhận|Đã xác nhận/").count());
  await page.screenshot({ path: EVID + "/br-15-history-after.png", fullPage: true });
  fs.writeFileSync("D:/tmp/inbodyqc/b2-log.txt", log.join("\n"));
  await browser.close();
})().catch(e => { console.error("FATAL", e.message); fs.writeFileSync("D:/tmp/inbodyqc/b2-log.txt", log.join("\n") + "\nFATAL " + e.message); });
