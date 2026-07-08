import { defineConfig, devices } from "@playwright/test";

// Config chup anh cho TAI LIEU VAN HANH (flow-guide). Chay thang vao deploy.
// BASE_URL=https://his.diab.com.vn npx playwright test --config=e2e/flow-guide.config.ts
export default defineConfig({
  testDir: "./",
  testMatch: ["flow-guide.spec.ts"],
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
      args: ["--disable-dev-shm-usage", "--disable-gpu", "--js-flags=--max-old-space-size=2048"],
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
