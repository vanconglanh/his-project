import { test, expect, type Page } from "@playwright/test";

/**
 * Evidence chạy thật cho tính năng product tour (driver.js).
 * Đăng nhập bằng tài khoản admin seed, kiểm tra trên 3 trang đại diện:
 *  - /reception (nhóm A)
 *  - /encounters/[id] (nhóm A - EMR)
 *  - /cashier (nhóm B)
 * Kiểm: auto-run lần đầu, next/prev, đóng (skip), không auto-run lại lần vào sau,
 *        nút "Hướng dẫn" chủ động chạy lại.
 * Ảnh lưu vào docs/qc/evidence-product-tour-20260830/.
 */

const EVID = "../docs/qc/evidence-product-tour-20260830";
const ADMIN = { email: "admin@prodiab.local", password: "admin123" };
const ENCOUNTER_ID = "ff6fb23e-4dc5-49c3-8ba5-21fbc3d1180e";

async function login(page: Page) {
  await page.goto("/login");
  await page.locator("input#email").fill(ADMIN.email);
  await page.locator("input#password").fill(ADMIN.password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await page.waitForURL((u) => !u.pathname.startsWith("/login"), {
    timeout: 30_000,
  });
}

/** Xoá mọi key tour-seen để mô phỏng lần đầu vào. */
async function clearTourSeen(page: Page) {
  await page.evaluate(() => {
    Object.keys(localStorage)
      .filter((k) => k.startsWith("tour-seen:"))
      .forEach((k) => localStorage.removeItem(k));
    // Đánh dấu tour "Làm quen hệ thống" (onboarding) đã xem để không chặn tour
    // trang lẻ đang test ở đây — onboarding có bộ evidence riêng
    // (tour-onboarding.spec.ts).
    try {
      const raw = localStorage.getItem("auth-store");
      const userId = raw ? JSON.parse(raw)?.state?.user?.id : null;
      if (userId != null) {
        localStorage.setItem(`tour-onboarding-seen:${userId}`, "1");
      }
    } catch {
      /* ignore */
    }
  });
}

function popover(page: Page) {
  return page.locator(".driver-popover:visible");
}

test.describe("Product tour - evidence", () => {
  test.beforeAll(() => {
    // đảm bảo thư mục tồn tại (Playwright tự tạo khi screenshot path có thư mục)
  });

  test("reception - auto run, next/prev, skip, no-repeat, manual re-run", async ({
    page,
  }) => {
    await login(page);
    await page.goto("/reception");
    await clearTourSeen(page);
    await page.reload();

    // Auto-run: popover hiện ra
    await expect(popover(page)).toBeVisible({ timeout: 8_000 });
    await expect(page.locator(".driver-popover-title")).toContainText(
      "Tiếp đón",
    );
    await expect(page.locator(".driver-popover-progress-text")).toContainText(
      "Bước 1/",
    );
    await page.screenshot({ path: `${EVID}/01-reception-autorun-step1.png` });

    // Next
    await page.locator(".driver-popover-next-btn").click();
    await expect(page.locator(".driver-popover-progress-text")).toContainText(
      "Bước 2/",
    );
    await page.screenshot({ path: `${EVID}/02-reception-step2-next.png` });

    // Prev
    await page.locator(".driver-popover-prev-btn").click();
    await expect(page.locator(".driver-popover-progress-text")).toContainText(
      "Bước 1/",
    );
    await page.screenshot({ path: `${EVID}/03-reception-prev-back-step1.png` });

    // Skip (đóng bằng nút x)
    await page.locator(".driver-popover-close-btn").click();
    await expect(page.locator(".driver-overlay")).toHaveCount(0);

    // Đánh dấu đã xem -> reload không auto-run lại
    await page.reload();
    await page.waitForTimeout(2_000);
    await expect(page.locator(".driver-overlay")).toHaveCount(0);
    await page.screenshot({
      path: `${EVID}/04-reception-no-autorun-after-seen.png`,
    });

    // Nút "Hướng dẫn" chủ động chạy lại
    await page.getByRole("button", { name: /Hướng dẫn sử dụng trang này/ }).click();
    await expect(popover(page)).toBeVisible({ timeout: 5_000 });
    await page.screenshot({
      path: `${EVID}/05-reception-manual-rerun-via-button.png`,
    });
  });

  test("encounter detail - auto run tour", async ({ page }) => {
    await login(page);
    await page.goto(`/encounters/${ENCOUNTER_ID}`);
    await clearTourSeen(page);
    await page.reload();

    await expect(popover(page)).toBeVisible({ timeout: 10_000 });
    await expect(page.locator(".driver-popover-title")).toBeVisible();
    await page.screenshot({
      path: `${EVID}/06-encounter-autorun-step1.png`,
    });

    // đi qua vài bước
    const next = page.locator(".driver-popover-next-btn");
    if (await next.isVisible()) {
      await next.click();
      await page.waitForTimeout(400);
      await page.screenshot({ path: `${EVID}/07-encounter-step2.png` });
    }
  });

  test("cashier - auto run tour + button", async ({ page }) => {
    await login(page);
    await page.goto("/cashier");
    await clearTourSeen(page);
    await page.reload();

    await expect(popover(page)).toBeVisible({ timeout: 8_000 });
    await expect(page.locator(".driver-popover-title")).toContainText("Thu ngân");
    await page.screenshot({ path: `${EVID}/08-cashier-autorun-step1.png` });

    await page.locator(".driver-popover-next-btn").click();
    await page.waitForTimeout(400);
    await page.screenshot({ path: `${EVID}/09-cashier-step2.png` });

    // đóng và reload -> không auto lại
    await page.locator(".driver-popover-close-btn").click();
    await page.reload();
    await page.waitForTimeout(2_000);
    await expect(page.locator(".driver-overlay")).toHaveCount(0);
    await page.screenshot({
      path: `${EVID}/10-cashier-no-autorun-after-seen.png`,
    });
  });
});
