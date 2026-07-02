/**
 * playwright.sim.config.ts — Cấu hình Playwright riêng cho bộ mô phỏng phòng khám (e2e/sim/*).
 * Kế thừa tinh thần e2e/playwright.config.ts (workers:1, tuần tự, reuseExistingServer) nhưng
 * testDir/testMatch/outputDir tách riêng để không lẫn với các spec E2E khác trong e2e/.
 */
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./",
  testMatch: ["clinic-simulation.spec.ts", "exceptions.spec.ts", "evidence.spec.ts"],
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  // outputDir (trace/attachment) và html report KHÔNG được lồng vào nhau (Playwright sẽ dọn sạch
  // outputDir mỗi lần chạy) nên đặt html-report là thư mục anh em, tách biệt với test-results/sim.
  reporter: process.env.CI ? "line" : [["list"], ["html", { open: "never", outputFolder: "../../test-results/sim-html-report" }]],
  // Kịch bản mô phỏng nhiều bệnh nhân/nhiều bước UI thật nên cần timeout rộng hơn cấu hình mặc định.
  timeout: 30 * 60_000,
  expect: { timeout: 10_000 },
  outputDir: "../../test-results/sim",
  use: {
    baseURL: process.env.BASE_URL || "http://localhost:3100",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    command: "npm run dev -- -p 3100",
    url: "http://localhost:3100",
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
