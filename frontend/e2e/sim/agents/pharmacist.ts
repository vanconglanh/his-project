/**
 * agents/pharmacist.ts — Mô phỏng thao tác dược sĩ trên /pharmacy/dispense: cấp phát thuốc theo
 * hàng chờ, từ chối đơn. Nhập kho (restock) chưa có selector ổn định đã xác minh trong UI hiện
 * tại nên luôn SKIP có ghi chú, không làm vỡ luồng chính.
 */
import type { Page } from "@playwright/test";
import type { Persona } from "../personas";
import { actionWithResponse, assertOkResponse } from "../helpers/ui";

export class PharmacistAgent {
  async gotoDispense(page: Page): Promise<void> {
    if (!page.url().includes("/pharmacy/dispense")) {
      await page.goto("/pharmacy/dispense", { waitUntil: "domcontentloaded", timeout: 30_000 });
    }
    await page.getByRole("heading", { name: "Phát thuốc" }).waitFor({ timeout: 15_000 });
    await page
      .getByRole("tab", { name: "Hàng chờ" })
      .click({ timeout: 10_000 })
      .catch(() => {});
  }

  /** Phát thuốc cho bệnh nhân theo tên hiển thị trên card hàng chờ. Trả về false nếu không có trong hàng chờ. */
  async dispense(page: Page, persona: Persona): Promise<boolean> {
    await this.gotoDispense(page);

    // Hàng chờ phát thuốc TÍCH TỤ nhiều đơn (đơn cũ chưa phát) -> lọc bằng ô "Tìm bệnh nhân..." theo
    // tên BN để chỉ còn 1 card, tránh getByText không thấy khi danh sách quá dài/virtualize.
    const search = page.getByPlaceholder(/Tìm bệnh nhân/i);
    if (await search.count()) {
      await search.fill(persona.fullName);
      await page.waitForTimeout(1200);
    }

    // Hàng chờ nạp bất đồng bộ (list + auto-refresh) -> chờ tên BN hiện ra trước khi kết luận "không
    // có đơn", tránh check quá sớm lúc list chưa render. Đồng thời KHÔNG khoá selector vào
    // data-slot="card" (bản deploy cũ trên prod chưa gắn attribute này) — fallback tìm tổ tiên gần
    // nhất của tên BN có chứa nút "Phát thuốc".
    const nameLoc = page.getByText(persona.fullName, { exact: false }).first();
    await nameLoc.waitFor({ state: "visible", timeout: 10_000 }).catch(() => {});

    let card = page.locator('[data-slot="card"]').filter({ hasText: persona.fullName }).first();
    if (!(await card.count())) {
      card = nameLoc.locator('xpath=ancestor::*[.//button[normalize-space()="Phát thuốc"]][1]');
    }
    if (!(await card.count())) {
      return false;
    }

    await card.getByRole("button", { name: "Phát thuốc", exact: true }).first().click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });

    // Select kho phát thuốc: SelectTrigger id="warehouse" (base-ui) — click theo id rồi chọn option
    // đầu tiên (KHO01). Dùng đúng pattern pickOptionById đã chạy ổn ở các Select khác.
    await dialog.locator("#warehouse").click({ timeout: 10_000 });
    const firstWarehouse = page.getByRole("option").first();
    await firstWarehouse.waitFor({ state: "visible", timeout: 10_000 });
    await firstWarehouse.click();

    const resp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: /Xác nhận phát thuốc/i }).click({ timeout: 10_000 }),
      { urlIncludes: "/pharmacy/dispense/", method: "POST" }
    );
    await assertOkResponse(resp, `Phát thuốc cho "${persona.fullName}"`);
    await page.waitForTimeout(800);

    const closeBtn = dialog.getByRole("button", { name: "Đóng", exact: true });
    if (await closeBtn.count()) {
      await closeBtn.click({ timeout: 8000 }).catch(() => {});
    }
    return true;
  }

  /** Từ chối phát thuốc kèm lý do. Trả về false nếu không có trong hàng chờ. */
  async reject(page: Page, persona: Persona, reason = "Thiếu thông tin đơn thuốc"): Promise<boolean> {
    await this.gotoDispense(page);

    const card = page.locator('[data-slot="card"]').filter({ hasText: persona.fullName }).first();
    if (!(await card.count())) {
      return false;
    }

    await card.getByRole("button", { name: "Từ chối", exact: true }).click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });
    await dialog.getByPlaceholder("Lý do...").fill(reason);
    const resp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: "Từ chối", exact: true }).click({ timeout: 10_000 }),
      { urlIncludes: "/reject", method: "POST" }
    );
    await assertOkResponse(resp, `Từ chối phát thuốc cho "${persona.fullName}"`);
    await page.waitForTimeout(500);
    return true;
  }

  /**
   * Nhập kho bổ sung — WarehouseTab/AdjustmentTab (/pharmacy) chưa được khảo sát đủ để có
   * selector ổn định trong phạm vi harness này, nên luôn SKIP có ghi chú rõ ràng.
   */
  async restock(page: Page, drugName: string): Promise<void> {
    // Điều hướng thật để ít nhất xác nhận trang còn truy cập được, phục vụ debug khi xem screenshot.
    await page.goto("/pharmacy", { waitUntil: "domcontentloaded", timeout: 30_000 }).catch(() => {});
    throw new Error(
      `SKIP: chức năng nhập kho cho "${drugName}" (WarehouseTab/AdjustmentTab) chưa có selector ổn định ` +
        `đã xác thực trong UI hiện tại — bỏ qua trong phạm vi mô phỏng này.`
    );
  }
}
