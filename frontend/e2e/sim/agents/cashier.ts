/**
 * agents/cashier.ts — Mô phỏng thao tác thu ngân trên /cashier: mở ca, đóng ca, thu tiền hoá
 * đơn trong tab "Hoá đơn chờ thu" (dropdown "..." aria-label "Thao tác" -> "Thu tiền").
 */
import type { Page } from "@playwright/test";
import type { Persona } from "../personas";
import { escapeRegExp, actionWithResponse, assertOkResponse } from "../helpers/ui";

export class CashierAgent {
  async gotoCashier(page: Page): Promise<void> {
    if (!page.url().includes("/cashier")) {
      await page.goto("/cashier", { waitUntil: "domcontentloaded", timeout: 30_000 });
    }
    await page.getByRole("heading", { name: "Thu ngân" }).waitFor({ timeout: 15_000 });
  }

  /** Mở ca thu ngân nếu chưa mở (nút "Mở ca" chỉ hiển thị khi ca đang đóng). */
  async openShift(page: Page, openingBalance = 500_000): Promise<void> {
    await this.gotoCashier(page);
    const openBtn = page.getByRole("button", { name: "Mở ca", exact: true });
    if (!(await openBtn.count())) {
      return; // Ca đã mở sẵn — không cần thao tác thêm.
    }
    await openBtn.click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });
    await dialog.locator("#opening_balance").fill(String(openingBalance));
    const resp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: "Mở ca", exact: true }).click({ timeout: 10_000 }),
      { urlIncludes: "/cashier/closing/open", method: "POST" }
    );
    await assertOkResponse(resp, "Mở ca thu ngân");
    await page.waitForTimeout(800);
  }

  /** Đóng ca thu ngân nếu đang mở. Có thể truyền actualCash để mô phỏng lệch quỹ. */
  async closeShift(page: Page, actualCash?: number, acceptDifference = false): Promise<void> {
    await this.gotoCashier(page);
    const closeBtn = page.getByRole("button", { name: "Đóng ca", exact: true });
    if (!(await closeBtn.count())) {
      return; // Không có ca đang mở.
    }
    await closeBtn.click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });
    if (actualCash != null) {
      await dialog.locator("#actual_cash").fill(String(actualCash));
    }
    if (acceptDifference) {
      const checkbox = dialog.getByRole("checkbox", { name: /Chấp nhận chênh lệch/i });
      if (await checkbox.count()) {
        await checkbox.check().catch(() => {});
      }
    }
    const resp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: "Đóng ca", exact: true }).click({ timeout: 10_000 }),
      { urlIncludes: "/cashier/closing/close", method: "POST" }
    );
    await assertOkResponse(resp, "Đóng ca thu ngân");
    await page.waitForTimeout(800);
  }

  /** Thu tiền cho hoá đơn của bệnh nhân trong tab "Hoá đơn chờ thu". Trả về false nếu không thấy hoá đơn. */
  async collect(page: Page, persona: Persona): Promise<boolean> {
    await this.gotoCashier(page);
    await page.getByRole("tab", { name: "Hoá đơn chờ thu" }).click({ timeout: 10_000 });
    await page.waitForTimeout(500);

    const row = page.getByRole("row", { name: new RegExp(escapeRegExp(persona.fullName)) }).first();
    if (!(await row.count())) {
      return false;
    }

    await row.getByRole("button", { name: "Thao tác" }).click({ timeout: 10_000 });
    await page.getByRole("menuitem", { name: "Thu tiền" }).click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });
    const resp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: /Xác nhận thu tiền/i }).click({ timeout: 10_000 }),
      { urlIncludes: "/payments", method: "POST" }
    );
    await assertOkResponse(resp, `Thu tiền hoá đơn của "${persona.fullName}"`);
    await page.waitForTimeout(800);
    return true;
  }
}
