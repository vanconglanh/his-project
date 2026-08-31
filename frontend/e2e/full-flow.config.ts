import { defineConfig, devices } from "@playwright/test";

// Config cho UTE full-flow — chạy vào stack Docker local (KHÔNG tự dựng webServer).
// cd frontend && npx playwright test --config=e2e/full-flow.config.ts
export default defineConfig({
  testDir: "./",
  testMatch: ["full-flow-evidence.spec.ts", "full-flow-evidence-part2.spec.ts"],
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 25 * 60_000,
  expect: { timeout: 12_000 },
  reporter: [["list"]],
  use: {
    baseURL: process.env.BASE_URL || "http://localhost:3000",
    viewport: { width: 1600, height: 950 },
    trace: "off",
    screenshot: "off",
    ignoreHTTPSErrors: true,
    launchOptions: {
      args: ["--disable-dev-shm-usage", "--disable-gpu", "--js-flags=--max-old-space-size=2048"],
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
