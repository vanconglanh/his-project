// Verify fix BUG-06: dropdown thuốc bị cắt + thiếu tên + thiếu nút Lưu đơn (màn kê đơn)
const { shot, quickLogin, newSession, saveJson, BASE } = require("./qc-lib");

const R = {};

(async () => {
  const { browser, page, net, consoleErrs } = await newSession();
  try {
    await quickLogin(page, "Bác sĩ");

    // Tìm 1 lượt khám đang khám (IN_PROGRESS) để vào tab kê đơn.
    // Nếu danh sách rỗng thì tạo mới 1 lượt khám.
    await page.goto(BASE + "/encounters", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);

    let encHref = await page
      .locator("table a[href^='/encounters/']")
      .first()
      .getAttribute("href")
      .catch(() => null);

    if (!encHref) {
      // Tạo lượt khám mới
      await page.goto(BASE + "/encounters/new", { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
      await page.waitForTimeout(1500);

      const patientInput = page.getByPlaceholder("Tìm theo tên, SĐT...").first();
      await patientInput.click();
      await patientInput.fill("an");
      await page.waitForTimeout(2000);
      // Chọn option đầu tiên trong dropdown bệnh nhân (theo cấu trúc absolute list giống ICD10)
      const patientOption = page.locator("div.absolute button").first();
      R.patientOptionCount = await patientOption.count();
      if (R.patientOptionCount) {
        await patientOption.click();
      }
      await page.waitForTimeout(500);
      await page.getByPlaceholder("Vd: Đái tháo đường tái khám, sốt 3 ngày...").fill("Kiểm tra QC BUG-06");
      await page.waitForTimeout(300);
      await page.getByRole("button", { name: "Tạo lượt khám" }).first().click();
      await page.waitForURL((u) => /\/encounters\/[^/]+$/.test(u.pathname) && !u.pathname.endsWith("/new"), { timeout: 20000 }).catch(() => {});
      await page.waitForTimeout(1500);
      encHref = new URL(page.url()).pathname;
      R.createdEncounter = encHref;
    }

    await page.goto(BASE + encHref, { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);

    // Nếu chưa IN_PROGRESS thì bấm bắt đầu khám (nếu có nút)
    const startBtn = page.getByRole("button", { name: /Bắt đầu khám/ });
    if (await startBtn.count()) {
      await startBtn.first().click().catch(() => {});
      await page.waitForTimeout(1500);
    }

    // Sang tab kê đơn
    await page.goto(BASE + encHref + "?tab=prescription", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1500);

    await shot(page, "BUG06_00_tab_prescription_initial");

    // Gõ tìm thuốc "para"
    const searchInput = page.getByPlaceholder("Tìm thuốc theo tên hoặc mã...").first();
    R.searchInputFound = (await searchInput.count()) > 0;
    if (R.searchInputFound) {
      await searchInput.click();
      await searchInput.fill("para");
      await page.waitForTimeout(1500);

      const dropdown = page.locator("[role=listbox]").first();
      R.dropdownVisible = (await dropdown.count()) > 0;
      if (R.dropdownVisible) {
        const box = await dropdown.boundingBox();
        R.dropdownBox = box;
        R.dropdownHeightOk = !!box && box.height > 30; // không còn bị cắt còn ~5px
        R.dropdownText = (await dropdown.innerText()).slice(0, 500);
      }
      await shot(page, "BUG06_01_dropdown_search_para", [
        { sel: "[role=listbox]", kind: "result", label: "Dropdown goi y thuoc" },
      ]);

      // Chọn thuốc đầu tiên
      const firstOption = page.locator("[role=option]").first();
      R.hasOption = (await firstOption.count()) > 0;
      if (R.hasOption) {
        R.firstOptionText = (await firstOption.innerText()).trim();
        await firstOption.click();
        await page.waitForTimeout(1000);
        await shot(page, "BUG06_02_after_select_drug");

        // Điền form item thuốc rồi bấm "Thêm vào đơn"
        const dosage = page.locator("#dosage");
        const frequency = page.locator("#frequency");
        if ((await dosage.count()) && (await frequency.count())) {
          await dosage.fill("1 viên");
          await frequency.fill("2 lần/ngày");
          await page.waitForTimeout(300);
          await page.getByRole("button", { name: "Thêm vào đơn" }).click();
          await page.waitForTimeout(2000);
        }
      }
    }

    await shot(page, "BUG06_03_after_add_item");

    // Kiểm tra nút Lưu đơn tồn tại
    const saveBtn = page.getByRole("button", { name: /^Lưu đơn$/ });
    R.saveButtonFound = (await saveBtn.count()) > 0;
    if (R.saveButtonFound) {
      await shot(page, "BUG06_04_save_button_visible", [
        { sel: "btn:Lưu đơn", kind: "action", label: "Nut Luu don" },
      ]);
      await saveBtn.first().click();
      await page.waitForTimeout(2000);
      await shot(page, "BUG06_05_after_click_save");
    }

    R.toasts = await page.locator("[data-sonner-toast]").allInnerTexts().catch(() => []);
  } catch (e) {
    R.error = e.message.slice(0, 500);
    await shot(page, "BUG06_error").catch(() => {});
  }

  R.apiErrors = net.filter((x) => x.status >= 400);
  R.apiAll = net.filter((x) => /prescriptions|drugs/i.test(x.url));
  R.consoleErrs = consoleErrs.slice(0, 15);
  saveJson("bug06_result", R);
  console.log(JSON.stringify(R, null, 1).slice(0, 6000));
  await browser.close();
})();
