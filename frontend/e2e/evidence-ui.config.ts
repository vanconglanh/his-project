import { defineConfig, devices } from "@playwright/test";

// Config riêng cho evidence capture — KHÔNG webServer (chạy thẳng vào deploy).
// BASE_URL=https://his.diab.com.vn npx playwright test --config=e2e/evidence-ui.config.ts
export default defineConfig({
  testDir: "./",
  testMatch: ["evidence-ui.spec.ts", "ute-evidence.spec.ts"],
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 20 * 60_000,
  expect: { timeout: 12_000 },
  reporter: [["list"]],
  use: {
    baseURL: process.env.BASE_URL || "https://his.diab.com.vn",
    viewport: { width: 1600, height: 900 },
    trace: "off",
    screenshot: "off",
    ignoreHTTPSErrors: true,
    launchOptions: {
      // Tránh Chromium headless crash ("Target closed") trên trang nặng (editor/chart)
      args: ["--disable-dev-shm-usage", "--disable-gpu", "--js-flags=--max-old-space-size=2048"],
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
