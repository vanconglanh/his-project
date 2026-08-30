import { test, expect, type Page } from "@playwright/test";

/**
 * Evidence chạy thật cho tour "Làm quen hệ thống" (onboarding, chạy 1 lần khi user
 * lần đầu đăng nhập) và trang "Trung tâm trợ giúp" (/help).
 * Ảnh lưu vào docs/qc/evidence-product-tour-20260830/.
 */

const EVID = "../docs/qc/evidence-product-tour-20260830";
const ADMIN = { email: "admin@prodiab.local", password: "admin123" };

async function login(page: Page) {
  await page.goto("/login");
  await page.locator("input#email").fill(ADMIN.email);
  await page.locator("input#password").fill(ADMIN.password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await page.waitForURL((u) => !u.pathname.startsWith("/login"), {
    timeout: 30_000,
  });
}

/** Mô phỏng "lần đầu đăng nhập": xoá toàn bộ trạng thái tour đã xem (route lẻ + onboarding). */
async function clearAllTourState(page: Page) {
  await page.evaluate(() => {
    Object.keys(localStorage)
      .filter((k) => k.startsWith("tour-seen:") || k.startsWith("tour-onboarding-seen:"))
      .forEach((k) => localStorage.removeItem(k));
  });
}

function popover(page: Page) {
  return page.locator(".driver-popover:visible");
}

test.describe("Tour onboarding + Trung tam tro giup - evidence", () => {
  test("onboarding tu chay dung 1 lan khi dang nhap lan dau, khong lap lai", async ({
    page,
  }) => {
    await login(page);
    await page.goto("/reception");
    await clearAllTourState(page);
    await page.reload();

    // Onboarding tự chạy trước tiên (chặn tour trang lẻ) — bước 1 là intro chào mừng.
    await expect(popover(page)).toBeVisible({ timeout: 8_000 });
    await expect(page.locator(".driver-popover-title")).toContainText(
      "Chào mừng"
    );
    await page.screenshot({ path: `${EVID}/11-onboarding-autorun-step1.png` });

    // Bước 2: sidebar
    await page.locator(".driver-popover-next-btn").click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${EVID}/12-onboarding-step-sidebar.png` });

    // Đi hết các bước còn lại tới khi nút "Hoàn tất" xuất hiện rồi bấm.
    for (let i = 0; i < 10; i++) {
      if (!(await popover(page).isVisible().catch(() => false))) break;
      const doneBtn = page.getByRole("button", { name: "Hoàn tất" });
      if (await doneBtn.isVisible().catch(() => false)) {
        await doneBtn.click();
        break;
      }
      const nextBtn = page.locator(".driver-popover-next-btn");
      if (await nextBtn.isVisible().catch(() => false)) {
        await nextBtn.click();
        await page.waitForTimeout(300);
      } else {
        break;
      }
    }
    // Onboarding đóng lại -> tour trang lẻ (Tiếp đón) tự chạy nối tiếp ngay
    // (theo thiết kế: TourButton chờ sự kiện onboarding xong rồi mới auto-start),
    // nên overlay không biến mất hẳn giữa 2 tour — kiểm tra trực tiếp tour kế tiếp.
    await expect(popover(page)).toBeVisible({ timeout: 5_000 });
    await expect(page.locator(".driver-popover-title")).toContainText(
      "Tiếp đón"
    );
    await page.screenshot({
      path: `${EVID}/13-onboarding-done-then-page-tour.png`,
    });
    await page.locator(".driver-popover-close-btn").click();
    await expect(page.locator(".driver-overlay")).toHaveCount(0, {
      timeout: 5_000,
    });

    // Đợi localStorage thực sự ghi nhận cả 2 tour đã xem trước khi reload
    // (onDestroyed ghi state ngay khi đóng, nhưng đợi tường minh để tránh race
    // hiếm gặp giữa event loop của driver.js và React effect cleanup).
    await expect
      .poll(
        () =>
          page.evaluate(() =>
            Object.keys(localStorage).some((k) => k.startsWith("tour-seen:/reception"))
          ),
        { timeout: 5_000 }
      )
      .toBe(true);

    // Reload -> onboarding KHÔNG chạy lại (đã đánh dấu đã xem)
    await page.reload();
    await page.waitForTimeout(2_000);
    await expect(page.locator(".driver-overlay")).toHaveCount(0);
    await page.screenshot({
      path: `${EVID}/14-onboarding-no-repeat-after-seen.png`,
    });

    const seen = await page.evaluate(() => {
      const raw = localStorage.getItem("auth-store");
      const userId = raw ? JSON.parse(raw)?.state?.user?.id : null;
      return userId != null
        ? localStorage.getItem(`tour-onboarding-seen:${userId}`)
        : null;
    });
    expect(seen).toBe("1");
  });

  test("trang /help liet ke tour va Xem lai huong dan dieu huong + tu chay tour", async ({
    page,
  }) => {
    await login(page);
    await page.goto("/reception");
    await clearAllTourState(page);
    // Đánh dấu đã xem hết để /help hoạt động độc lập, không bị onboarding/tour tự
    // động chặn khi điều hướng qua lại giữa các bước kiểm tra.
    await page.evaluate(() => {
      const raw = localStorage.getItem("auth-store");
      const userId = raw ? JSON.parse(raw)?.state?.user?.id : null;
      if (userId != null) {
        localStorage.setItem(`tour-onboarding-seen:${userId}`, "1");
      }
    });

    await page.goto("/help");
    await expect(
      page.getByRole("heading", { name: "Trung tâm trợ giúp" })
    ).toBeVisible();
    await expect(page.getByText("Làm quen hệ thống")).toBeVisible();
    await expect(page.getByText("Hướng dẫn màn hình Tiếp đón")).toBeVisible();
    await page.screenshot({ path: `${EVID}/15-help-center-list.png` });

    // Bấm "Xem lại hướng dẫn" của mục Tiếp đón -> điều hướng sang /reception và tự chạy tour.
    await page.locator('[data-testid="help-tour-btn-reception"]').click();

    await page.waitForURL((u) => u.pathname === "/reception", {
      timeout: 10_000,
    });
    await expect(popover(page)).toBeVisible({ timeout: 8_000 });
    await expect(page.locator(".driver-popover-title")).toContainText(
      "Tiếp đón"
    );
    await page.screenshot({
      path: `${EVID}/16-help-navigate-and-autostart-tour.png`,
    });

    // Query param `?tour=1` phải được strip khỏi URL sau khi kích hoạt.
    await page.waitForTimeout(500);
    expect(new URL(page.url()).searchParams.get("tour")).toBeNull();
  });
});
