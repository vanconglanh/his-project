import { test, expect } from "@playwright/test";

test.describe("Authentication", () => {
  test("renders login page", async ({ page }) => {
    await page.goto("/login");
    await expect(
      page.getByRole("heading", { name: "Đăng nhập" })
    ).toBeVisible();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Mật khẩu")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Đăng nhập" })
    ).toBeVisible();
  });

  test("shows validation errors on empty submit", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("button", { name: "Đăng nhập" }).click();
    await expect(page.getByText("Email là bắt buộc")).toBeVisible();
    await expect(page.getByText("Mật khẩu là bắt buộc")).toBeVisible();
  });

  test("shows invalid email error", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Email").fill("not-an-email");
    await page.getByLabel("Mật khẩu").fill("password123");
    await page.getByRole("button", { name: "Đăng nhập" }).click();
    await expect(page.getByText("Email không hợp lệ")).toBeVisible();
  });

  test("successful login redirects to dashboard", async ({ page }) => {
    // Mock API response
    await page.route("**/api/v1/auth/login", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          data: {
            accessToken: "mock-access-token",
            refreshToken: "mock-refresh-token",
            expiresIn: 3600,
            user: {
              id: 1,
              email: "admin@phongkham.vn",
              fullName: "Nguyễn Văn A",
              role: "Admin",
              tenantId: 1,
              clinicId: 1,
              clinicName: "Phòng khám Demo",
            },
          },
        }),
      });
    });

    await page.goto("/login");
    await page.getByLabel("Email").fill("admin@phongkham.vn");
    await page.getByLabel("Mật khẩu").fill("password123");
    await page.getByRole("button", { name: "Đăng nhập" }).click();

    // Should redirect to dashboard
    await expect(page).toHaveURL("/");
  });

  test("shows error toast on invalid credentials (401)", async ({ page }) => {
    await page.route("**/api/v1/auth/login", async (route) => {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          error: {
            code: "INVALID_CREDENTIALS",
            message: "Email hoặc mật khẩu không đúng",
          },
        }),
      });
    });

    await page.goto("/login");
    await page.getByLabel("Email").fill("admin@phongkham.vn");
    await page.getByLabel("Mật khẩu").fill("wrongpassword");
    await page.getByRole("button", { name: "Đăng nhập" }).click();

    await expect(
      page.getByText("Email hoặc mật khẩu không đúng")
    ).toBeVisible();
  });
});
